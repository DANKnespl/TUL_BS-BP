using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    /// <summary>
    /// Interakční logika pro PreprocessingWindow.xaml
    /// </summary>
    public partial class PreprocessingWindow : Window
    {
        public PreprocessingWindow()
        {
            InitializeComponent();
        }

        private DB_Lists getStructure(string path)
        {
            DB_Lists Databases = new DB_Lists();
            Databases.DB_List_Name = db_name.Text;
            Databases.ES_index = es_index.Text;
            string DB_type = db_type.Text;
            string Record_type = record_type.Text;
            var dirs = System.IO.Directory.GetDirectories(path);
            Databases.DB_List = new DBs[dirs.Length];
            int dirCounter = 0;
            foreach (var dir in dirs)
            {
                int fileCounter = 0;
                DBs newDB = new DBs();
                newDB.DB_Name = dir.Substring(dir.LastIndexOf("\\")+1, dir.Length- dir.LastIndexOf("\\")-1);
                newDB.DB_Type = DB_type;
                newDB.Record_Type = Record_type;
                var files = System.IO.Directory.GetFiles(dir, "*.jpg");
                newDB.Records = new string[files.Length][];
                foreach (var file in files)
                {
                    string[] record = new string[4];
                    record[0] = file.Substring(0, file.Length - 4) + ".txt"; //txt
                    record[1] = file; //img
                    record[2] = "0";
                    record[3] = "0";
                    if (file.Contains("inhalt"))
                    {
                        newDB.Lookup_File = file.Substring(0, file.Length - 4) + ".txt";
                    }
                    newDB.Records[fileCounter]=record;
                    fileCounter++;
                }
                Databases.DB_List[dirCounter]=newDB;
                dirCounter++;
            }
            return Databases;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DB_Lists structure = getStructure("C:\\Rocenky_JJHS");
            SaveFileDialog sfw = new SaveFileDialog();
            sfw.ShowDialog();
            if (sfw.SafeFileName != "")
            {
                System.IO.File.WriteAllText(sfw.FileName, JsonSerializer.Serialize(structure));
            }
        }
    }
}
