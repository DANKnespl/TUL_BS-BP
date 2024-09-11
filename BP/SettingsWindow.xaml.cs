using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections;
using System.Xml;

namespace BP
{
    public class AppControls
    {
        public Dictionary<string, Key> keys { get; set; }
        public ModifierKeys special { get; set; }

        public AppControls()
        {
            keys = new Dictionary<string, Key>();
        }
        public AppControls Copy()
        {
            AppControls copiedObject = new();
            foreach (var kvp in keys)
            {
                copiedObject.keys.Add(kvp.Key, kvp.Value);
            }
            copiedObject.special = special;
            return copiedObject;
        }
    }


    /// <summary>
    /// Interakční logika pro Window1.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        AppControls appControls;        
        
        public SettingsWindow()
        {
            InitializeComponent();
            elCheck.IsChecked = GlobalVars.elEnable;
            if (GlobalVars.appControls == null)
            {
                appControls = new();
            }
            else
            {
                appControls = GlobalVars.appControls.Copy();
            }
            if (appControls is not null)
            {
                special.Text = appControls.special.ToString();
                Key tmpKey;
                appControls.keys.TryGetValue("next", out tmpKey);
                next.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("last", out tmpKey);
                last.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("save", out tmpKey);
                save.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("undo", out tmpKey);
                undo.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("redo", out tmpKey);
                redo.Text = tmpKey.ToString();

                appControls.keys.TryGetValue("image", out tmpKey);
                image.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("database", out tmpKey);
                database.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("search", out tmpKey);
                search.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("navigation", out tmpKey);
                navigation.Text = tmpKey.ToString();
                appControls.keys.TryGetValue("controls", out tmpKey);
                controls.Text = tmpKey.ToString();
            }
        }

        /// <summary>
        /// Key pressed -> save as shortcut enabler
        /// not TAB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void setActionKey(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab)
            {
                TextBox tbox = (TextBox)sender;
                appControls.special = Keyboard.Modifiers;
                tbox.Text = appControls.special.ToString();
            }
        }

        /// <summary>
        /// Key pressed -> save as shortcut
        /// not TAB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void setKey(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab)
            {
                TextBox tbox = (TextBox)sender;
                appControls.keys[tbox.Name] = e.Key;
                tbox.Text = e.Key.ToString();
            }
        }

        /// <summary>
        /// Button click -> save shortcuts to JSON file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void saveKeys(object sender, RoutedEventArgs e)
        {
            string[] contents = new string[10];
            contents[0]=next.Text;
            contents[1] = last.Text;
            contents[2] = save.Text;
            contents[3] = undo.Text;
            contents[4] = redo.Text;
            contents[5] = image.Text;
            contents[6] = database.Text;
            contents[7] = search.Text;
            contents[8] = navigation.Text;
            contents[9] = controls.Text;
            if (contents.Distinct().Count() == contents.Length)
            {
                GlobalVars.appControls = appControls.Copy();
                File.WriteAllText("./Settings.json", JsonSerializer.Serialize(GlobalVars.appControls));
                Debug.WriteLine(JsonSerializer.Serialize(GlobalVars.appControls));
                MessageBox.Show("Uložení nastavení proběhlo úspěšně!", "Settings status");
            }
            else
            {
                MessageBox.Show("Uložení nastavení neproběhlo z důvodu konfliktních klávesových zkratek!", "Settings status");
            }
        }

        /// <summary>
        /// Button click -> Create Elasticsearch connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ElasticConn(uri.Text, user.Text, pass.Password);
        }

        /// <summary>
        /// Checked -> Allow saving text to Elasticsearch DB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void elCheck_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            GlobalVars.elEnable = cb.IsChecked.Value;
        }
    }
}
