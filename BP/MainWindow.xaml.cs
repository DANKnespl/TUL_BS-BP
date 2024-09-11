using Elasticsearch.Net;
using Microsoft.Win32;
using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit;

namespace BP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class DBs
    {
        public string DB_Name { get; set; }
        public string DB_Type { get; set; }
        public string Record_Type { get; set; }
        public string Lookup_File { get; set; }
        public string[][] Records { get; set; }
    }
    public class DB_Lists 
    {
        public string DB_List_Name { get; set; }
        public string ES_index { get; set; }
        public DBs[] DB_List { get; set; }
    }


    public partial class MainWindow : Window
    {
        Window1 w1;
        SearchWindow sw = new();
        SettingsWindow settings = new();
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                GlobalVars.appControls = JsonSerializer.Deserialize<AppControls>(File.ReadAllText("./Settings.json"));
            }
            catch
            {
                File.WriteAllText("./Settings.json", "{\"keys\":{\"controls\":58,\"next\":25,\"last\":23,\"save\":62,\"undo\":69,\"redo\":68,\"image\":52,\"database\":47,\"search\":61,\"navigation\":57},\"special\":2}");
                GlobalVars.appControls = JsonSerializer.Deserialize<AppControls>(File.ReadAllText("./Settings.json"));
            }
            StartUP st = new();
            Nullable<bool> dialogResult = st.ShowDialog();
            if (dialogResult.Value && dialogResult.HasValue)
            {
                GlobalVars.pattern = "";
                GlobalVars.mainWindow = this;
                this.MISearch.IsEnabled = false;
                RefreshImg(true);
                w1 = new Window1();
                cacheFiles();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }


        /// <summary>
        /// Loading data from JSON file
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void loadJsonFile()
        {
            if (w1 != null && w1.ShowInTaskbar)
            {
                w1.Close();
            }
            if (sw.ShowInTaskbar)
            {
                sw.Close();
            }
            if (GlobalVars.foundWindow != null && GlobalVars.foundWindow.ShowInTaskbar)
            {
                GlobalVars.foundWindow.Close();
            }
            OpenFileDialog openFileDialog1 = new()
            {
                CheckFileExists = true,
                Filter = "json Files (*.json)|*.json",
                DefaultExt = "json"
            };
            openFileDialog1.ShowDialog();
            try
            {
                jsonDeser(openFileDialog1.FileName);
            }
            catch
            {
                GlobalVars.JsonFile = null;
                throw new Exception();
            }

            GlobalVars.pattern = "";
            GlobalVars.mainWindow = this;
            this.MISearch.IsEnabled = false;
            RefreshImg(true);
            w1 = new Window1();
            cacheFiles();
        }

        /// <summary>
        /// Loading text data to RAM
        /// </summary>
        private async void cacheFiles() 
        {
            StatusBarI3.Content = "Loading Files";
            await loadFilesAsync2();
            MISearch.IsEnabled = true;
            StatusBarI3.Content = "Loading Done, Search enabled";
            inhaltOffsets();
        }

        /// <summary>
        /// Creating connection to Elasticsearch DB
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="uname"></param>
        /// <param name="pword"></param>
        public static void ElasticConn(string uri,string uname,string pword)
        {
            if(uri == "")
            {
                uri = "https://localhost:9200";
            }
            var node = new Uri(uri);
            var settings = new ConnectionSettings(node)
                .ServerCertificateValidationCallback((o, certificate, chain, errors) => true) // Disable certificate verification
                .BasicAuthentication(uname, pword)
                .DefaultFieldNameInferrer(p => p);
            GlobalVars.esClient = new ElasticClient(settings);
        }

        /// <summary>
        /// JSON parser
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="Exception"></exception>
        private static void jsonDeser(string path)
        {
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch
            {
                throw new Exception();
            }
            var tmpDB = JsonSerializer.Deserialize<DB_Lists>(json);
            if (tmpDB.DB_List_Name != null && tmpDB.DB_List != null)
            {
                if (tmpDB.ES_index != null && tmpDB.ES_index != "")
                {
                    GlobalVars.esIndex = tmpDB.ES_index;
                }
                else
                {
                    GlobalVars.esIndex = "index";
                }
                GlobalVars.JsonFile = path;
                GlobalVars.Databases = tmpDB;
            }
        }

        /// <summary>
        /// On window load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //SettingsWindow settingsWindow = new();
            //settingsWindow.Show();
        }

        /// <summary>
        /// On window close -> kill everything
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Finding chapters offsets for records
        /// </summary>
        private static void inhaltOffsets()
        {
            int[][] offsets = new int[GlobalVars.Databases.DB_List.Length][];
            int iter = 0;
            for(int i = 0; i < GlobalVars.Databases.DB_List.Length; i++)
            {
                string[] text;
                try 
                {
                    text = File.ReadAllText(GlobalVars.Databases.DB_List[i].Lookup_File).Split("\n");
                }
                catch 
                {
                    text = "".Split("\n");
                }
                string[] transcript = new string[GlobalVars.Databases.DB_List[i].Records.Length];
                for (int j = 0; j < GlobalVars.Databases.DB_List[i].Records.Length; j++)
                {
                    transcript[j] = GlobalVars.Databases.DB_List[i].Records[j][0];
                }
                int recordOffset = 0;
                int[] databaseOffsets = new int[text.Length];
                int lineCounter = 0;
                try
                {
                foreach (string line in text)
                {
                    Match m = Regex.Match(line, "#(\\d{1,3}) (.+)$");
                    if (m.Success)
                    {
                        while (!transcript[recordOffset + int.Parse(m.Groups[1].Value)].Substring(transcript[recordOffset + int.Parse(m.Groups[1].Value)].IndexOf("s") + 1, 3).Contains(m.Groups[1].Value))
                        {
                            recordOffset++;
                        }
                        databaseOffsets[lineCounter] = recordOffset;
                        lineCounter++;
                        recordOffset--;
                    }
                }
                }catch { }
                offsets[iter] = databaseOffsets;
                iter++;
            }
            GlobalVars.inhaltOffsets = offsets;
        }

        /// <summary>
        /// Loading all files
        /// </summary>
        /// <returns></returns>
        private static async ValueTask loadFilesAsync2()
        {
            DBs[] databases = GlobalVars.Databases.DB_List;
            string[][] transcriptS = new string[databases.Length][];
            await Task.Run(() =>
            {
                Parallel.ForEach(databases, (database, state, i) =>
                {
                    string[] files = new string[database.Records.Length];
                    for (int j = 0; j < database.Records.Length; j++) {
                        //files[j] = database.Records[j][0];
                        files[j] = getChangedFile(i, j);
                    }
                    var strings = new string[files.Length];
                    for (int j = 0; j < files.Length; j++)
                    {
                        if ( File.Exists(files[j])) {
                            strings[j] = File.ReadAllText(files[j]);
                        }else{
                            strings[j] = "";
                        }
                    }
                    transcriptS[i] = strings;
                });
            });
            GlobalVars.transcriptS = transcriptS;
            /*
            //No idea why this was here
            int[] indexOffset = new int[databases.Length];
            indexOffset[0] = 0;
            for (int i = 1; i < indexOffset.Length; i++)
            {
                indexOffset[i] = indexOffset[i - 1] + transcriptS[i - 1].Length;
            }
            GlobalVars.pageOffsets = indexOffset;
            */
        }

        /// <summary>
        /// Load new record
        /// </summary>
        /// <param name="way"></param>
        public void RefreshImg(bool way)
        {
            if ((GlobalVars.recordCounter) < GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records.Length && (GlobalVars.recordCounter) >= 0)
            {
                try
                {
                    ImgBox.Source = new BitmapImage(new Uri(GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[GlobalVars.recordCounter][1]));
                }
                catch
                {
                    ImgBox.Source = null;
                }
                TextBox1.Document.Blocks.Clear();
                var doc = new FlowDocument();
                string input;
                if (!MISearch.IsEnabled) {
                    try 
                    {
                        input = File.ReadAllText(getChangedFile(GlobalVars.databaseCounter,GlobalVars.recordCounter));
                    }
                    catch 
                    {
                        input = "";
                    }
                }
                else
                {
                    input = GlobalVars.transcriptS[GlobalVars.databaseCounter][GlobalVars.recordCounter];
                }
                Paragraph p = new(new Run(input));
                doc.Blocks.Add(p);
                TextBox1.Document = doc;
                TextBox1.ScrollToHome();
                sw.ReplaceMethod(TextBox1, GlobalVars.pattern, 0);
                StatusBarI1.Content = GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Record_Type + ": " + (GlobalVars.recordCounter + 1) + "/" + GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records.Length;
                StatusBarI1.Content = StatusBarI1.Content+ ", " + GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[GlobalVars.recordCounter][2] + "/" + GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[GlobalVars.recordCounter][3];
                StatusBarI2.Content = GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].DB_Type + ": " + GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].DB_Name;
            }
            else { fixFiles(way); }
        }

        /// <summary>
        /// No record with given index in DB fix
        /// </summary>
        /// <param name="way"></param>
        public void fixFiles(bool way)
        {
            int tmpDatabaseCounter = GlobalVars.databaseCounter;
            int tmpRecordCounter = GlobalVars.recordCounter;
            try
            {
                if (way == true)
                {
                    GlobalVars.databaseCounter++;
                    GlobalVars.recordCounter = 0;
                    RefreshImg(true);
                }
                else
                {
                    GlobalVars.databaseCounter--;
                    GlobalVars.recordCounter = (GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records.Length) - 1;
                    RefreshImg(true);
                }
            }
            catch
            {
                GlobalVars.databaseCounter = tmpDatabaseCounter;
                GlobalVars.recordCounter = (tmpRecordCounter > 0) ? (tmpRecordCounter - 1) : (0);
            }
        }

        /// <summary>
        /// rewrite old edits
        /// </summary>
        /// <param name="path0"></param>
        /// <param name="length"></param>
        private void fixChangedFiles(string path0,int length)
        {
            string pathSrc, pathDst;
            string text;
            for(int i = 2; i <=length; i++)
            {
                pathSrc = path0.Substring(0, path0.Length - 4) + "_" + i + ".txt";
                pathDst = path0.Substring(0, path0.Length - 4) + "_" + (i-1) + ".txt";
                text = File.ReadAllText(pathSrc);
                File.WriteAllText(pathDst, text);
            }
        }

        /// <summary>
        /// Load edit into Elasticsearch DB
        /// </summary>
        /// <param name="newText"></param>
        private void UpdateES(string newText)
        {
            if (GlobalVars.elEnable)
            {
                var searchResponse = GlobalVars.esClient.Search<dynamic>(s => s
                    .Index(GlobalVars.esIndex)
                    .Source(false)
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m => m.Match(t => t.Field("RecordCounter").Query(GlobalVars.recordCounter.ToString())),
                                m => m.Match(t => t.Field("DBCounter").Query(GlobalVars.databaseCounter.ToString()))
                            )
                        )
                    )
                );
                if (searchResponse.IsValid)
                {
                    foreach (var hit in searchResponse.Hits)
                    {
                        var id = hit.Id.ToString();
                        var existingDoc = GlobalVars.esClient.Get<dynamic>(id, g => g.Index(GlobalVars.esIndex));

                        if (existingDoc.Found)
                        {
                            var script = $"ctx._source.{"Text"} = params.updatedValue";
                            var updateResponse = GlobalVars.esClient.Update<dynamic, dynamic>(id, u => u
                                .Index(GlobalVars.esIndex)
                                .Script(s => s.Source(script).Params(p => p.Add("updatedValue", newText)))
                            );

                            if (updateResponse.IsValid)
                            {
                                Debug.WriteLine("Succ");
                            }
                            else
                            {
                                Debug.WriteLine("Fail");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get text data from current edit
        /// </summary>
        /// <param name="dbCounter"></param>
        /// <param name="recordCounter"></param>
        /// <returns></returns>
        public static string getChangedFile(long dbCounter,int recordCounter)
        {
            var currentRecord = GlobalVars.Databases.DB_List[dbCounter].Records[recordCounter];
            string textFile = currentRecord[0];
            if (currentRecord[2] != "0")
            {
                textFile = textFile.Substring(0, textFile.Length - 4) + "_" + currentRecord[2] + ".txt";
            }
            return textFile;
        }
        
        /// <summary>
        /// Change image in current record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgChange(object sender, RoutedEventArgs e) 
        {
            OpenFileDialog openFileDialog1 = new()
            {
                CheckFileExists = true,
                Filter = "Image Files(*.PNG;*.BMP;*.JPG;*.GIF)|*.PNG;*.BMP;*.JPG;*.GIF|All files (*.*)|*.*",
                DefaultExt = "png"
            };
            openFileDialog1.ShowDialog();
            if(openFileDialog1.FileName != "")
            {
                GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[GlobalVars.recordCounter][1] = openFileDialog1.FileName;
                File.WriteAllText(GlobalVars.JsonFile, JsonSerializer.Serialize(GlobalVars.Databases));
            }
            RefreshImg(true);
        }
        
        /// <summary>
        /// Save edit
        /// </summary>
        public void addChange()
        {
            TextRange textRange = new TextRange(
                TextBox1.Document.ContentStart,
                TextBox1.Document.ContentEnd
            );
            var currentRecord = GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[GlobalVars.recordCounter];
            string textFile = currentRecord[0];
            if (currentRecord[2] != "4")
            {
                currentRecord[2] = "" + (int.Parse(currentRecord[2]) + 1);
                currentRecord[3] = currentRecord[2];
            }
            else
            {
                fixChangedFiles(currentRecord[0],int.Parse(currentRecord[2]));
            }
            textFile = textFile.Substring(0, textFile.Length - 4) + "_" + int.Parse(currentRecord[2]) + ".txt";
            UpdateES(textRange.Text);
            File.WriteAllText(textFile, textRange.Text);

            GlobalVars.transcriptS[GlobalVars.databaseCounter][GlobalVars.recordCounter] = File.ReadAllText(getChangedFile(GlobalVars.databaseCounter, GlobalVars.recordCounter));
            File.WriteAllText(GlobalVars.JsonFile, JsonSerializer.Serialize(GlobalVars.Databases));
            RefreshImg(true);
        }

        /// <summary>
        /// Returning to previous edit
        /// </summary>
        private void undoChange() 
        {
            var currentRecord = GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[GlobalVars.recordCounter];
            if (currentRecord[2] != "0")
            {
                currentRecord[2] = "" + (int.Parse(currentRecord[2]) - 1);
            }
            GlobalVars.transcriptS[GlobalVars.databaseCounter][GlobalVars.recordCounter] = File.ReadAllText(getChangedFile(GlobalVars.databaseCounter, GlobalVars.recordCounter));
            UpdateES(File.ReadAllText(getChangedFile(GlobalVars.databaseCounter, GlobalVars.recordCounter)));
            File.WriteAllText(GlobalVars.JsonFile, JsonSerializer.Serialize<DB_Lists>(GlobalVars.Databases));
            RefreshImg(true);
        }

        /// <summary>
        /// Returning next edit
        /// </summary>
        private void redoChange()
        {
            var currentRecord = GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[GlobalVars.recordCounter];
            if (currentRecord[2] != currentRecord[3]){
                currentRecord[2] = "" + (int.Parse(currentRecord[2]) + 1);
            }
            GlobalVars.transcriptS[GlobalVars.databaseCounter][GlobalVars.recordCounter] = File.ReadAllText(getChangedFile(GlobalVars.databaseCounter, GlobalVars.recordCounter));
            UpdateES(File.ReadAllText(getChangedFile(GlobalVars.databaseCounter, GlobalVars.recordCounter)));
            File.WriteAllText(GlobalVars.JsonFile, JsonSerializer.Serialize<DB_Lists>(GlobalVars.Databases));
            RefreshImg(true);
        }

        /// <summary>
        /// Keypress controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (!TextBox1.IsFocused)
            {
                PreviewKeyDown(sender, e);
            }
        }

        /// <summary>
        /// Keyboard shortcuts controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == GlobalVars.appControls.special)
            {
                switch (GetKeyFromValue(e.Key))
                {
                    case "save":
                        if (MISearch.IsEnabled)
                        {
                            addChange();
                        }
                        break;
                    case "undo":
                        if (MISearch.IsEnabled)
                        {
                            undoChange();
                        }
                        break;
                    case "redo":
                        if (MISearch.IsEnabled)
                        {
                            redoChange();
                        }
                        break;
                    case "next":
                        ButtonNext_Click(null, null);
                        break;
                    case "last":
                        ButtonLast_Click(null, null);
                        break;
                    case "navigation":
                        Contents_Click(null, null);
                        break;
                    case "search":
                        if (MISearch.IsEnabled)
                        {
                            Search_Click(null, null);
                        }
                        break;
                    case "image":
                        ImgChange(null, null);
                        break;
                    case "database":
                        OpenDatabase_Click(null, null);
                        break;
                    case "controls":
                        Settings_Click(null, null);
                        break;
                    default:
                        break;
                }
            }
            
        }

        /// <summary>
        /// Get shorcut action
        /// </summary>
        /// <param name="valueVar"></param>
        /// <returns></returns>
        public static string GetKeyFromValue(Key valueVar)
        {
            foreach (string keyVar in GlobalVars.appControls.keys.Keys)
            {
                if (GlobalVars.appControls.keys[keyVar] == valueVar)
                {
                    return keyVar;
                }
            }
            return null;
        }

        /// <summary>
        /// Load new JSON file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenDatabase_Click(object sender, EventArgs e) 
        {
            GlobalVars.recordCounter=0;
            GlobalVars.databaseCounter = 0;
            try
            {
                loadJsonFile();
            }
            catch
            {
                Debug.Write("Did not load");
            }
        }
        
        /// <summary>
        /// Button click -> Next record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.recordCounter++;
            RefreshImg(true);
        }

        /// <summary>
        /// Button click -> Previous record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLast_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.recordCounter--;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Open search window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (sw.IsVisible == false)
            {
                sw = new SearchWindow();
            }
            sw.Show();
            sw.Focus();
        }

        /// <summary>
        /// Button click -> Open settings window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settings.IsVisible == false)
            {
                settings = new SettingsWindow();
            }
            settings.Show();
            settings.Focus();
        }

        /// <summary>
        /// Button click -> Open List of contents window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contents_Click(object sender, RoutedEventArgs e)
        {
            if (w1.IsVisible == false)
            {
                w1 = new Window1();
            }
            w1.Show();
            w1.Focus();
        }

        /// <summary>
        /// Button click -> Allow zoom on image
        /// </summary>
        /// <param name="enable"></param>
        private void EnableDisableZoom(bool enable)
        {
            if (enable && ZoomBool.IsChecked)
            {
                MyMagnifier.ZoomFactor = 0.3;
            }
            else
            {
                MyMagnifier.ZoomFactor = 0;
            }
        }

        /// <summary>
        /// Button click -> Standard text bold
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bold_Checked(object sender, RoutedEventArgs e)
        {
            TextBox1.FontWeight = FontWeights.Bold;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Standard text not bold
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bold_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBox1.FontWeight = FontWeights.Normal;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Standard text italic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Italic_Checked(object sender, RoutedEventArgs e)
        {
            TextBox1.FontStyle = FontStyles.Italic;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Standard text not italic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Italic_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBox1.FontStyle = FontStyles.Normal;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Standard text larger
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IncreaseFont_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox1.FontSize < 20)
            {
                TextBox1.FontSize += 1;
            }
        }

        /// <summary>
        /// Button click -> Standard text smaller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DecreaseFont_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox1.FontSize > 10)
            {
                TextBox1.FontSize -= 1;
            }
        }

        /// <summary>
        /// Button click -> Text found by search bold
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bold_Checked_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontWeightFound = FontWeights.Bold;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Text found by search not bold
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bold_Unchecked_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontWeightFound = FontWeights.Normal;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Text found by search italic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Italic_Checked_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontStyleFound = FontStyles.Italic;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> text found by search not italic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Italic_Unchecked_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontStyleFound = FontStyles.Normal;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Background of text found by search none 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoneBackground_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontBackground = TextBox1.Background;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Background of text found by search yellow 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void YellowBackground_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontBackground = Brushes.Yellow;
            RefreshImg(false);
        }

        /// <summary>
        /// Button click -> Background of text found by search red 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RedBackground_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontBackground = Brushes.Red;
            RefreshImg(false);
        }
        
        /// <summary>
        /// Button click -> Background of text found by search magenta
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MagentaBackground_Found(object sender, RoutedEventArgs e)
        {
            GlobalVars.fontBackground = Brushes.Magenta;
            RefreshImg(false);
        }

        /// <summary>
        /// Mouse enters image area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgBox_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            EnableDisableZoom(true);
        }

        /// <summary>
        /// Mouse leaves image area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgBox_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            EnableDisableZoom(false);
        }
    }
}