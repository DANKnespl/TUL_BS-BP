using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
    /// Interakční logika pro StartUP.xaml
    /// </summary>
    public partial class StartUP : Window
    {
        bool status = false;
        
        public StartUP()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loading data from JSON file
        /// </summary>
        private void loadJsonFile()
        {
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
                GlobalVars.JsonFile = openFileDialog1.FileName;
                if (GlobalVars.Databases != null)
                {

                    string text = "DBList name: " + GlobalVars.Databases.DB_List_Name + "\nNumber of databases: " + GlobalVars.Databases.DB_List.Length + "\nElastic index: "+GlobalVars.esIndex;
                    database_data.Text = text;
                    status = true;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                status = false;
                database_data.Text = "Database Error";
                GlobalVars.JsonFile = null;
            }
        }

        /// <summary>
        /// JSON parser
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="Exception"></exception>
        private void jsonDeser(string path)
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
            GlobalVars.Databases = JsonSerializer.Deserialize<DB_Lists>(json);
            if (GlobalVars.Databases.DB_List_Name == null || GlobalVars.Databases.DB_List == null)
            {
                GlobalVars.Databases = null;
                GlobalVars.esIndex = null;
            }
            else if (GlobalVars.Databases.ES_index != null && GlobalVars.Databases.ES_index != "")
            {
                GlobalVars.esIndex = GlobalVars.Databases.ES_index;
            }
            else
            {
                GlobalVars.esIndex = "index";
            }
        }

        /// <summary>
        /// Button click -> Validate JSON file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            loadJsonFile();
        }

        /// <summary>
        /// Button click -> Load JSON file, connect to Elasticsearch DB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (GlobalVars.JsonFile != null){
                MainWindow.ElasticConn(uri.Text, user.Text, pass.Password);
                GlobalVars.elEnable = elCheck.IsChecked.Value;
                this.DialogResult = status;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            PreprocessingWindow pr = new PreprocessingWindow();
            pr.Show();
        }
    }
}
