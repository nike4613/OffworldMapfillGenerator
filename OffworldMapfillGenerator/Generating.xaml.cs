using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace OffworldMapfillGenerator
{
    /// <summary>
    /// Interaction logic for Generating.xaml
    /// </summary>
    public partial class Generating : Window
    {
        public Generating()
        {
            InitializeComponent();
        }
        
        private static List<string> DIRECT_COMPONENTS = new List<string>() {
            "MapName", "UsesTypes", "MapWidth", "MapLength", "MapEdgeTilePadding", "MapClass", "MapSizeType", "RequiredMod", "LocationType", "HasResourceInfo", "MapLatitude"
        };

        private class GenerationPropertyStruct
        {
            public string MapName = "";
            public bool UsesTypes = false;
            public int MapWidth = 0;
            public int MapLength = 0;
            public int MapEdgeTilePadding = 0;
            public string MapClass = "";
            public string MapSizeType = "";
            public string RequiredMod = "";
            public string LocationType = "";
            public bool HasResourceInfo = false;
            public int MapLatitude = 0;

            public string tileToGenerate = "";
            public bool randomizeTiles = false;
            public HashSet<string> tilesToGenerate = new HashSet<string>();

            public string iceToGenerate = "";
            public bool randomizeIce = false;
            public HashSet<string> icesToGenerate = new HashSet<string>();
        }

        private Thread Thread;

        public event Action OnGenerateComplete = new Action(()=> { });
        public event Action OnGenerateClose = new Action(() => { });

        private string targetFilename = "map.map";

        private int TileGenerationCount = 0;

        private GenerationPropertyStruct props;

        private void LoadProps(MainWindow window)
        {
            props = new GenerationPropertyStruct();

            Type owntype = typeof(MainWindow);
            Type tgttype = typeof(GenerationPropertyStruct);

            foreach (var compn in DIRECT_COMPONENTS)
            {
                var field = owntype.GetField(compn, BindingFlags.NonPublic | BindingFlags.Instance);
                var field2 = tgttype.GetField(compn, BindingFlags.Public | BindingFlags.Instance);
                var membr = owntype.InvokeMember(field.Name, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, window, new object[] { });

                if (membr.GetType() == typeof(TextBox))
                {
                    TextBox box = (TextBox)membr;

                    string boxval = box.Text;
                    object value = boxval;

                    if (field2.FieldType == typeof(int)) value = int.Parse(boxval); 

                    field2.SetValue(props, value);
                }
                if (membr.GetType() == typeof(ComboBox))
                {
                    ComboBox box = (ComboBox)membr;
                    ComboBoxItem item = (ComboBoxItem)box.SelectedItem;
                    string value = item.Name;

                    field2.SetValue(props, value);
                }
                if (membr.GetType() == typeof(CheckBox))
                {
                    CheckBox box = (CheckBox)membr;
                    bool? isChecked = box.IsChecked;

                    field2.SetValue(props, isChecked.GetValueOrDefault());
                }
            }

            props.randomizeTiles = window.selector.randomizeTiles.IsChecked.GetValueOrDefault();
            props.randomizeIce = window.selector.randomizeIce.IsChecked.GetValueOrDefault();

            props.tileToGenerate = ((ComboBoxItem)window.selector.tileToGenerate.SelectedItem).Name.Replace("cmbx_","");
            props.iceToGenerate = ((ComboBoxItem)window.selector.iceToGenerate.SelectedItem).Name.Replace("cmbx_", "");

            Type selector = typeof(TerrainSelector);

            var fields = selector.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (!field.Name.Contains("use_")) continue;

                if (!((CheckBox)field.GetValue(window.selector)).IsChecked.GetValueOrDefault()) continue;

                string name = field.Name.Replace("use_", "");

                if (name.Contains("ICE"))
                { // Ice
                    props.icesToGenerate.Add(name);
                } else
                { // Terrain
                    props.tilesToGenerate.Add(name);
                }
            }
        }

        public Thread Generate(MainWindow window)
        {
            LoadProps(window);

            int mapw = int.Parse(window.MapWidth.Text);
            int maph = int.Parse(window.MapLength.Text);
            int border = int.Parse(window.MapEdgeTilePadding.Text);

            TileGenerationCount = (mapw + border * 2) * (maph + border * 2);

            // 1: Create XML doc, DIRECT_COMPONENTS.Count: Apply attributes, TileGenerationCount*5: Number of tiles*5 things, 1: Write File
            Progress.Maximum = 1 + DIRECT_COMPONENTS.Count + TileGenerationCount*5 + 1;
            Progress.Minimum = 0;
            Progress.IsIndeterminate = false;
            Progress.Value = 0;

            var thread = new Thread(() => {
                GenerateThread();
                Dispatcher.Invoke(new Action(() => {
                    Progress.IsIndeterminate = true;
                    OnGenerateComplete();
                    Close();
                }));
            });

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = String.Concat(props.MapName.Split(System.IO.Path.GetInvalidFileNameChars())), // Default file name
                DefaultExt = ".map", // Default file extension
                Filter = "Offworld Custom Map Files|*.map" // Filter files by extension
            };

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                targetFilename = dlg.FileName;
            }
            else return null;
            
            thread.Start();

            Thread = thread;

            return thread;
        }

        private void IncrementProgress(int amount=1)
        {
            Dispatcher.InvokeAsync(new Action(() => { Progress.Value += amount; }));
        }

        private void GenerateThread()
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", ""));

            XmlElement root = (XmlElement)doc.AppendChild(doc.CreateElement("Root"));
            IncrementProgress();

            Type owntype = typeof(GenerationPropertyStruct);

            foreach (var compn in DIRECT_COMPONENTS)
            {
                var field = owntype.GetField(compn, BindingFlags.Public | BindingFlags.Instance);

                var membr = owntype.InvokeMember(compn, BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance, null, props, new object[] { });

                if (membr.GetType() == typeof(string))
                {
                    string box = (string)membr;

                    root.SetAttribute(compn, box);
                }
                if (membr.GetType() == typeof(int))
                {
                    int box = (int)membr;
                    string value = box.ToString();

                    root.SetAttribute(compn, value);
                }
                if (membr.GetType() == typeof(bool))
                {
                    string value = ((bool)membr) ? "True" : "False";
                    root.SetAttribute(compn, value);
                }

                IncrementProgress();
            }

            Type terrsel = typeof(TerrainSelector);

            Random rand = new Random();

            for (int i = 0; i < TileGenerationCount; i++)
            {
                XmlElement tinfo = (XmlElement)root.AppendChild(doc.CreateElement("tInfo"));
                IncrementProgress();

                tinfo.SetAttribute("ID", i.ToString());
                IncrementProgress();

                XmlElement terrain = (XmlElement)tinfo.AppendChild(doc.CreateElement("Terrain"));

                if (!props.randomizeTiles)
                    terrain.InnerText = props.tileToGenerate;
                else
                    terrain.InnerText = props.tilesToGenerate.ElementAt(rand.Next(0, props.tilesToGenerate.Count));
                IncrementProgress();

                XmlElement height = (XmlElement)tinfo.AppendChild(doc.CreateElement("Height"));

                //ComboBox box = SafeGetField<ComboBox>(window.selector, "tileToGenerate");
                //ComboBoxItem item = SafeGetProperty<ComboBoxItem>(box, "SelectedItem");
                height.InnerText = "HEIGHT_MEDIUM";
                IncrementProgress();

                string iceg = props.iceToGenerate;

                if (props.randomizeIce)
                    iceg = props.icesToGenerate.ElementAt(rand.Next(0, props.icesToGenerate.Count));

                if (iceg != "NOICE")
                {
                    XmlElement ice = (XmlElement)tinfo.AppendChild(doc.CreateElement("IceType"));
                    ice.InnerText = iceg;
                }
                IncrementProgress();
            }

            XmlWriter writer = XmlWriter.Create(targetFilename, new XmlWriterSettings() {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = true,
                NewLineChars = "\r\n",
                Encoding = Encoding.UTF8
            });

            doc.Save(writer);
            IncrementProgress();
        }

        private void ProgressBarChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Progress.IsIndeterminate)
                TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            else
                TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

            TaskbarInfo.ProgressValue = (Progress.Value-Progress.Minimum)/Progress.Maximum;
            
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Thread.IsAlive)
                Thread.Abort();
            OnGenerateClose();
        }
    }
}
