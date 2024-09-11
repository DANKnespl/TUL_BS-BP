using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BP
{
    public class MyListBoxItem : ListBoxItem
    {
        public string DBname { get; set; }
        public int DBrecord { get; set; }
    }

    /// <summary>
    /// Interakční logika pro FoundWindow.xaml
    /// </summary>
    public partial class FoundWindow : Window
    {
        public FoundWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creating records to show in window
        /// </summary>
        /// <param name="result"></param>
        /// <param name="context"></param>
        public async void addItem(ElasticObj result, TextBlock context)
        {
            MyListBoxItem listBoxItem = new()
            {
                DBname = GlobalVars.Databases.DB_List[result.DBCounter].DB_Name,
                Tag = result.DBCounter,
                DBrecord = result.RecordCounter,
                Content = context
            };
            this.ListBox1.Items.Add(listBoxItem);
        }

        /// <summary>
        /// Button press -> Show picked record in main window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ShowClick(object sender, RoutedEventArgs e)
        {
            if (ListBox1.SelectedItem is MyListBoxItem lbi)
            {
                GlobalVars.databaseCounter = int.Parse(lbi.Tag.ToString());
                GlobalVars.recordCounter = lbi.DBrecord;
                GlobalVars.mainWindow.RefreshImg(true);
            }
        }

        /// <summary>
        /// Transform data for next work
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static ElasticObj[] SearchWrapper(int[] input)
        {
            int tmpPointer = 0;
            List<ElasticObj> tmpRes = new();
            for (int i = 0; i < GlobalVars.transcriptS.Length; i++)
            {
                for (int j = 0; j < GlobalVars.transcriptS[i].Length; j++)
                {
                    if (input[tmpPointer] != -1)
                    {
                        tmpRes.Add(new ElasticObj(i, j));
                    }
                    tmpPointer++;
                }
            }
            return tmpRes.ToArray();
        }

        /// <summary>
        /// Adding records with text to window to show
        /// </summary>
        /// <param name="result"></param>
        /// <param name="pattern"></param>
        public static void generateItems2(ElasticObj[] result, string pattern)
        {
            GlobalVars.foundWindow.ListBox1.Items.Clear();
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i].DBCounter<GlobalVars.Databases.DB_List.Length && result[i].RecordCounter < GlobalVars.Databases.DB_List[result[i].DBCounter].Records.Length)
                {
                    GlobalVars.foundWindow.addItem(result[i], getContext(result[i], pattern));
                }
                else
                {
                    break;
                }

            }
        }

        /// <summary>
        /// Find text around first instance of found text
        /// </summary>
        /// <param name="result"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static TextBlock getContext(ElasticObj result, string pattern)
        {
            const string longDivider = "==================================";
            const string shortDivider = "----------------------------------------------------------";
            string beginString = "...";
            string endString = "...";

            int databaseCount = result.DBCounter;
            int record = result.RecordCounter;
            string txt = GlobalVars.transcriptS[databaseCount][record];
            int contextLength = 50;
            int minIndex;
            int maxIndex;
            
            List<int> indexes = SearchWindow.BoyerMooreAll(txt, pattern);
            int recordOffset;
            try
            {
                recordOffset = indexes[0];
            }catch
            {
                return null;
            }

            if (recordOffset > contextLength / 2)
            {
                minIndex = recordOffset - contextLength / 2;
                maxIndex = recordOffset + contextLength / 2;
            }
            else
            {
                beginString = "";
                minIndex = 0;
                maxIndex = contextLength;
            }

            if (maxIndex >= txt.Length)
            {
                endString = "";
                maxIndex = txt.Length - 1;
            }

            TextBlock outputTextBlock = new();
            
            outputTextBlock.Inlines.Add(getRun(longDivider + "\n", 0));
            if (pattern.Length >= 5)
            {
                outputTextBlock.Inlines.Add(getRun(getHeadline(databaseCount, record) + "\n" + shortDivider + "\n", 0));
            }
            outputTextBlock.Inlines.Add(getRun(beginString + txt[minIndex..indexes[0]], 0));

            int touchingOffset = 0;
            for (int i = 0; i < indexes.Count; i++)
            {
                outputTextBlock.Inlines.Add(getRun(txt.Substring(indexes[i] + touchingOffset, pattern.Length - touchingOffset), 1));
                if (pattern.Length < contextLength)
                {
                    if ((i + 1 == indexes.Count) || (indexes[i + 1] >= maxIndex - pattern.Length))
                    {
                        outputTextBlock.Inlines.Add(getRun(txt[(indexes[i] + pattern.Length)..maxIndex].Trim(), 0));
                        break;
                    }
                    try //oeo -> oeoeo
                    {
                        outputTextBlock.Inlines.Add(getRun(txt[(indexes[i] + pattern.Length)..indexes[i + 1]].Trim(), 0));
                        touchingOffset = 0;
                    }
                    catch
                    {
                        touchingOffset = -(indexes[i + 1] - (indexes[i] + pattern.Length));
                    }
                }
            }
            outputTextBlock.Inlines.Add(getRun(endString + "\n" + shortDivider + "\n" + GlobalVars.Databases.DB_List[databaseCount].DB_Type + ": " + GlobalVars.Databases.DB_List[databaseCount].DB_Name + ", " + GlobalVars.Databases.DB_List[databaseCount].Record_Type + ": " + (record + 1), 0));
            outputTextBlock.Inlines.Add(getRun("\n" + longDivider, 0));
            return outputTextBlock;
        }

        /// <summary>
        /// Find chapter of the record
        /// </summary>
        /// <param name="databaseCount"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public static string getHeadline(int databaseCount, int record)
        {
            string[] text;
            try
            {
                text = File.ReadAllText(GlobalVars.Databases.DB_List[databaseCount].Lookup_File).Split("\n");
            }
            catch
            {
                text = "".Split("\n");
            }
            string headlineText = "";
            int matchesCounter = 0;
            Match[] matches = new Match[text.Length];
            foreach (string line in text)
            {
                Match m = Regex.Match(line, "#(\\d{1,3}) (.+)$");
                if (m.Success)
                {
                    matches[matchesCounter] = m;
                    matchesCounter++;
                }
            }

            //WTF is this, what in the god forsaken hell have I been smoking
            /*
             * if recordPointer is smaller than actualRecordPointer, it returns the name of chapter
             * if it is larger, it follows, that it is part of one of the next chapters
             * therefore it rewrites the current chapter name with the next one and goes check that one
            */
            for (int i = 0; i < matchesCounter; i++)
            {

                if (record < GlobalVars.inhaltOffsets[databaseCount][i] + int.Parse(matches[i].Groups[1].Value))
                {
                    return headlineText;
                }
                else
                {
                    headlineText = matches[i].Groups[2].Value.Trim();
                }
            }
            return headlineText;
        }

        /// <summary>
        /// Function to colour text in context
        /// </summary>
        /// <param name="text"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static Run getRun(string text, int status)
        {
            Run outputRun = new(text);
            switch (status)
            {
                case 1:
                    outputRun.Foreground = Brushes.Red;
                    outputRun.FontWeight = FontWeights.Bold;
                    break;
                default:
                    outputRun.Foreground = Brushes.Black;
                    outputRun.FontWeight = FontWeights.Normal;
                    break;
            }
            return outputRun;
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
                ShowClick(sender, e);
            }
        }
    }
}
