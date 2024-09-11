using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Nest;
using Elasticsearch.Net;
using System.Runtime;
using System.Text;
using System.Collections;

namespace BP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public class ElasticObj
    {
        public int DBCounter { get; set; }
        public int RecordCounter { get; set; }
        public string Text { get; set; }
        public ElasticObj(int db,int record) {
            this.DBCounter = db;
            this.RecordCounter = record;
            this.Text = "";
        }
    }


    public partial class SearchWindow : Window
    {



        public SearchWindow()
        {
            InitializeComponent();
            FindBox.Text = GlobalVars.pattern;
            currentRB.IsChecked = true;
        }
        
        /// <summary>
        /// Searching in Elasticsearch DB
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private ElasticObj[] SearchES(string pattern) {
            var searchResults = GlobalVars.esClient.Search<ElasticObj>(s => s
                .Index(GlobalVars.esIndex)
                .Query(q => q
                    .Wildcard(c => c
                        .Field(f => f.Text.Suffix("wildcard"))
                        .Value(WildcardedString(pattern))
                    )
                )
                .Sort(sort => sort
                    .Ascending(p => p.DBCounter)
                    .Ascending(p => p.RecordCounter)
                )
                .Source(src => src
                    .Excludes(f => f.Field(p => p.Text)) // Exclude the "Text" field from the source
                )
                .Size(10000)
                .Scroll("1m")
            );
            return searchResults.Documents.ToArray();
        }

        /// <summary>
        /// Preprocessing of pattern for Elasticsearch DB
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string WildcardedString(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            return "*" + Regex.Escape(input) + "*";
        }

        /// <summary>
        /// Button click -> Searching
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.pattern = FindBox.Text;
            if (GlobalVars.foundWindow == null || GlobalVars.foundWindow.IsVisible == false)
            {
                GlobalVars.foundWindow = new FoundWindow();
            }
            if (currentRB.IsChecked.Value)
            {
                GlobalVars.foundWindow.Visibility = Visibility.Hidden;
                ReplaceMethod(GlobalVars.mainWindow.TextBox1, GlobalVars.pattern, 0);
            }
            if (allRB.IsChecked.Value)
            {
                GlobalVars.foundWindow.Show();
                GlobalVars.foundWindow.Focus();
                ReplaceMethod(GlobalVars.mainWindow.TextBox1, GlobalVars.pattern, 1);
            }
            if (inhaltRB.IsChecked.Value)
            {
                GlobalVars.foundWindow.Show();
                GlobalVars.foundWindow.Focus();
                ReplaceMethod(GlobalVars.mainWindow.TextBox1, GlobalVars.pattern, 2);
            }
        }

        /// <summary>
        /// Change font of found text
        /// </summary>
        /// <param name="rtBox"></param>
        /// <param name="pattern"></param>
        /// <param name="type"></param>
        public void ReplaceMethod(RichTextBox rtBox, string pattern, int type)
        {
            if (GlobalVars.mainWindow.MISearch.IsEnabled)
            {
                GlobalVars.mainWindow.StatusBarI3.Content = "";
            }
            if (pattern.Length > 0)
            {
                if (type > 0 && ElasticBool.IsChecked is not null)
                {
                    Check(pattern, type, ElasticBool.IsChecked.Value);
                }
                var paragraph = new TextRange(rtBox.Document.ContentStart, rtBox.Document.ContentEnd);
                paragraph.ApplyPropertyValue(TextElement.BackgroundProperty, rtBox.Background);
                paragraph.ApplyPropertyValue(TextElement.FontWeightProperty, rtBox.FontWeight);
                paragraph.ApplyPropertyValue(TextElement.FontStyleProperty, rtBox.FontStyle);
                TextRange textRange;
                TextPointer documentStart = paragraph.Start;
                TextRange documentRange = new(documentStart, paragraph.End);

                List<int> ints = BoyerMooreAll(documentRange.Text, pattern);
                int NumberOfMatches = 0;
                for (int i = ints.Count - 1; i >= 0; i--)
                {
                    int startIndex = ints[i];
                    int length = pattern.Length;

                    if (NumberOfMatches > 0)
                    {
                        startIndex++;
                    }
                    TextPointer startPointer = documentStart.GetPositionAtOffset(startIndex);
                    TextPointer endPointer = documentStart.GetPositionAtOffset(startIndex + length);
                    textRange = new TextRange(startPointer, endPointer);
                    textRange.ApplyPropertyValue(TextElement.BackgroundProperty, GlobalVars.fontBackground);
                    textRange.ApplyPropertyValue(TextElement.FontWeightProperty, GlobalVars.fontWeightFound);
                    textRange.ApplyPropertyValue(TextElement.FontStyleProperty, GlobalVars.fontStyleFound);
                    NumberOfMatches++;
                    GlobalVars.mainWindow.StatusBarI3.Content = "Matches found: " + NumberOfMatches + ", Pattern: " + pattern + "  ";
                }
            }
            else
            {
                var paragraph = new TextRange(rtBox.Document.ContentStart, rtBox.Document.ContentEnd);
                paragraph.ApplyPropertyValue(TextElement.BackgroundProperty, rtBox.Background);
                paragraph.ApplyPropertyValue(TextElement.FontWeightProperty, rtBox.FontWeight);
                paragraph.ApplyPropertyValue(TextElement.FontStyleProperty, rtBox.FontStyle);

            }
        }

        /// <summary>
        /// Boyer-Moore returning all instances of pattern
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<int> BoyerMooreAll(string text, string pattern)
        {
            List<int> occurrences = new();
            int m = pattern.Length;
            int n = text.Length;
            Dictionary<char, int> badChar = new();
            // Initialize all occurrences as -1
            foreach (char c in pattern)
            {
                if (!badChar.ContainsKey(c))
                {
                    badChar[c] = -1;
                }
            }
            
            // Fill the actual value of last occurrence
            for (int i = 0; i < m; i++)
            {
                badChar[pattern[i]] = i;
            }

            int s = 0;  // s is shift of the pattern with respect to text
            while (s <= n - m)
            {
                int j = m - 1;
                // Keep reducing index j of pattern while characters of pattern and text are matching
                while (j >= 0 && pattern[j] == text[s + j])
                {
                    j--;
                }
                // If the pattern is present at the current shift, then index j will become -1 after the above loop
                if (j < 0)
                {
                    occurrences.Add(s);
                    // shift the pattern to the right so that the next occurrence can be found
                    if (s + m < n)
                    {
                        char nextChar = text[s + m];
                        s += Math.Max(1, m - (badChar.ContainsKey(nextChar) ? badChar[nextChar] : -1));
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    // Shift the pattern so that the bad character in text aligns with the last occurrence of it in the pattern.
                    char badCharInText = text[s + j];
                    s += Math.Max(1, j - (badChar.ContainsKey(badCharInText) ? badChar[badCharInText] : -1));
                }
            }
            return occurrences;
        }

        /// <summary>
        /// KMP returning index of first instance of pattern/-1
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static int KMP(string text, string pattern)
        {
            int textLength = text.Length;
            int patternLength = pattern.Length;
            int[] prefixFunction = ComputePrefixFunction(pattern);
            int textIndex = 0;
            int patternIndex = 0;
            while (textIndex < textLength)
            {
                if (text[textIndex] == pattern[patternIndex])
                {
                    textIndex++;
                    patternIndex++;
                    if (patternIndex == patternLength)
                    {
                        return textIndex - patternLength;
                    }
                }
                else if (patternIndex > 0)
                {
                    patternIndex = prefixFunction[patternIndex - 1];
                }
                else
                {
                    textIndex++;
                }
            }
            return -1;
        }

        /// <summary>
        /// KMP prefix array
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static int[] ComputePrefixFunction(string pattern)
        {
            int m = pattern.Length;
            int[] prefix = new int[m];
            int prefixLength = 0;  // length of the longest proper prefix of the substring ending at i
            for (int i = 1; i < m; i++)
            {
                while (prefixLength > 0 && pattern[prefixLength] != pattern[i])
                {
                    prefixLength = prefix[prefixLength - 1];
                }
                if (pattern[prefixLength] == pattern[i])
                {
                    prefixLength++;
                }
                prefix[i] = prefixLength;
            }
            return prefix;
        }

        /// <summary>
        /// KMP/Elasticsearch filtering
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="type"></param>
        /// <param name="elastic"></param>
        public async void Check(string pattern, int type, bool elastic)
        {
            ElasticObj[] result;
            decimal start = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;

            if (elastic && type == 1) {
                result = SearchES(pattern);
            }
            else
            {
                var tasks = new List<Task<int>>();
                for (int i = 0; i < GlobalVars.transcriptS.Length; i++)
                {
                    for (int j = 0; j < GlobalVars.transcriptS[i].Length; j++)
                        tasks.Add(Contains(i, j, pattern, type));
                }
                int[] tmpResult = await Task.WhenAll(tasks.ToArray());
                result = FoundWindow.SearchWrapper(tmpResult);
            }
            decimal end = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
            decimal millis = end - start;
            try
            {
                using (StreamWriter outputFile = File.AppendText("./Logs/SearchLog.txt"))
                {
                    outputFile.WriteLine("Pattern: " + pattern + ", TTS: " + millis.ToString("G") + " ms, Type: " + type + ", Contains: " + result.Length);
                }
            } catch (Exception ex)
            {
                bool folderExists = Directory.Exists("./Logs/");
                if (!folderExists)
                    Directory.CreateDirectory("./Logs/");
                using (FileStream fs = File.Create("./Logs/SearchLog.txt")) ;
                using (StreamWriter outputFile = File.AppendText("./Logs/SearchLog.txt"))
                {
                    outputFile.WriteLine("Pattern: " + pattern + ", TTS: " + millis.ToString("G") + " ms, Type: " + type + ", Contains: " + result.Length);
                }
            }
            FoundWindow.generateItems2(result, pattern);
        }

        /// <summary>
        /// KMP filtering
        /// </summary>
        /// <param name="databaseCounter"></param>
        /// <param name="recordCounter"></param>
        /// <param name="pattern"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static async Task<int> Contains(int databaseCounter, int recordCounter, string pattern, int type)
        {
            string transcript;
            if (type == 2)
            {
                if (GlobalVars.Databases.DB_List[databaseCounter].Lookup_File == "" || GlobalVars.Databases.DB_List[databaseCounter].Lookup_File != GlobalVars.Databases.DB_List[databaseCounter].Records[recordCounter][0])
                {
                    return -1;
                }
                try
                {
                    transcript = File.ReadAllText(GlobalVars.Databases.DB_List[databaseCounter].Lookup_File);
                }
                catch
                {
                    transcript = "";
                }
            }
            else
            {
                transcript = GlobalVars.transcriptS[databaseCounter][recordCounter];
            }
            return KMP(transcript, pattern);
        }

        /// <summary>
        /// Return press -> Show picked record in main window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Search_Click(sender, e);
            }
        }
    }
}