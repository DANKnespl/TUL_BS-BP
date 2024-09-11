using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BP
{
    static class GlobalVars
    {
        public static AppControls appControls;

        public static DB_Lists Databases;

        public static int databaseCounter;
        public static int recordCounter = 0;
        public static ElasticClient esClient;
        public static bool elEnable = false;
        public static string esIndex;


        public static string[][] transcriptS;
        public static int[][] inhaltOffsets;

        public static MainWindow mainWindow;
        public static Window1 window1;
        public static FoundWindow foundWindow;

        public static string pattern;
        public static string JsonFile;

        public static FontWeight fontWeightFound = FontWeights.Normal;
        public static FontStyle fontStyleFound = FontStyles.Normal;
        public static Brush fontBackground = Brushes.Yellow;
    }
}
