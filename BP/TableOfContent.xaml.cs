using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace BP
{
    /// <summary>
    /// Interakční logika pro Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private int chosenDBCounter;
        public Window1()
        {
            InitializeComponent();
            GlobalVars.window1 = this;
            chosenDBCounter = GlobalVars.databaseCounter;
            for (int i = 0;i < GlobalVars.Databases.DB_List.Length;i++) 
            {
                RadioButton rb = new RadioButton()
                {
                    Content = GlobalVars.Databases.DB_List[i].DB_Name,
                    Tag = i
                };
                if (rb.Content == GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].DB_Name)
                {
                    rb.IsChecked = true;
                    generateContent();
                }
                rb.Click += new RoutedEventHandler(radial_click);
                rb.VerticalAlignment = VerticalAlignment.Center;
                rb.HorizontalAlignment = HorizontalAlignment.Center;
                this.GB_databases.Children.Add(rb);
            }
        }

        /// <summary>
        /// Database select -> create chapters radials
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radial_click(object sender, RoutedEventArgs e)
        {
            chosenDBCounter = int.Parse((sender as RadioButton).Tag.ToString());
            generateContent();
        }

        /// <summary>
        /// Create database radials
        /// </summary>
        private void generateContent()
        {
            string DBName = GlobalVars.Databases.DB_List[chosenDBCounter].DB_Name;
            string[] text;
            try
            {
                text = File.ReadAllText(GlobalVars.Databases.DB_List[chosenDBCounter].Lookup_File).Split("\n");
            }
            catch
            {
                text = "".Split("\n");
            }
            this.GB_records.Children.Clear();
            char[] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            foreach (string line in text)
            {
                if (Regex.Match(line, "#\\d{1,3}").Success) {
                    RadioButton rb = new RadioButton();
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = line;
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    rb.Content = textBlock; 
                    rb.Tag = Regex.Match(line, "#\\d{1,3}").ToString().Substring(1);
                    rb.VerticalAlignment = VerticalAlignment.Center;
                    //rb.HorizontalAlignment = HorizontalAlignment.Center;
                    rb.Click += new RoutedEventHandler(Button_Click);
                    this.GB_records.Children.Add(rb);
                }
            }
        }
        
        /// <summary>
        /// Chapter select -> Load new data in main window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int recordOffset = 0;
            GlobalVars.databaseCounter = chosenDBCounter;
            GlobalVars.recordCounter = int.Parse((sender as RadioButton).Tag.ToString());
            string[] transcript = new string[GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records.Length];
            for (int i = 0; i < GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records.Length; i++)
            {
                transcript[i] = GlobalVars.Databases.DB_List[GlobalVars.databaseCounter].Records[i][0];
            }
            while (!transcript[recordOffset + GlobalVars.recordCounter].Substring(transcript[recordOffset + GlobalVars.recordCounter].IndexOf("s") + 1, 3).Contains(GlobalVars.recordCounter.ToString()))
            { 
                recordOffset++; 
            }
            GlobalVars.recordCounter += recordOffset;
            GlobalVars.mainWindow.RefreshImg(true);
        }
    }
}