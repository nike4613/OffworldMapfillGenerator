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

        private Thread Thread;

        public event Action OnGenerateComplete = new Action(()=> { });
        public event Action OnGenerateClose = new Action(() => { });

        private int TileGenerationCount = 0;

        public Thread Generate(MainWindow window)
        {
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
                GenerateThread(window);
                Dispatcher.Invoke(new Action(() => {
                    Progress.IsIndeterminate = true;
                    OnGenerateComplete();
                    Close();
                }));
            });

            thread.Start();

            Thread = thread;

            return thread;
        }

        private void IncrementProgress(int amount=1)
        {
            Dispatcher.InvokeAsync(new Action(() => { Progress.Value += amount; }));
        }

        private T SafeGetProperty<T>(UIElement o, string name)
        {
            T outp = default(T);

            Dispatcher.Invoke(new Action(() =>
            {
                Type t = o.GetType();
                var prop = t.GetProperty(name);
                outp = (T)t.InvokeMember(prop.Name, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, o, new object[] { });
            }));

            return outp;
        }

        private T SafeGetField<T>(UIElement o, string name)
        {
            T outp = default(T);

            Dispatcher.Invoke(new Action(() =>
            {
                Type t = o.GetType();
                var prop = t.GetField(name, BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                outp = (T)t.InvokeMember(prop.Name, BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, o, new object[] { });
            }));

            return outp;
        }

        private void GenerateThread(MainWindow window)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", ""));

            XmlElement root = (XmlElement)doc.AppendChild(doc.CreateElement("Root"));
            IncrementProgress();

            Type owntype = typeof(MainWindow);

            foreach (var compn in DIRECT_COMPONENTS)
            {
                var field = owntype.GetField(compn, BindingFlags.NonPublic | BindingFlags.Instance);

                var membr = owntype.InvokeMember(field.Name, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, window, new object[] { });

                if (membr.GetType() == typeof(TextBox))
                {
                    TextBox box = (TextBox)membr;

                    root.SetAttribute(compn, SafeGetProperty<string>(box, "Text"));
                }
                if (membr.GetType() == typeof(ComboBox))
                {
                    ComboBox box = (ComboBox)membr;
                    ComboBoxItem item = SafeGetProperty<ComboBoxItem>(box, "SelectedItem");
                    string value = SafeGetProperty<string>(item, "Name");

                    root.SetAttribute(compn, value);
                }
                if (membr.GetType() == typeof(CheckBox))
                {
                    CheckBox box = (CheckBox)membr;
                    bool? isChecked = SafeGetProperty<bool?>(box, "IsChecked");
                    string value = isChecked.GetValueOrDefault() ? "True" : "False";
                    root.SetAttribute(compn, value);
                }

                IncrementProgress();
            }

            Type terrsel = typeof(TerrainSelector);

            for (int i = 0; i < TileGenerationCount; i++)
            {
                XmlElement tinfo = (XmlElement)root.AppendChild(doc.CreateElement("tInfo"));
                IncrementProgress();

                tinfo.SetAttribute("ID", i.ToString());
                IncrementProgress();

                XmlElement terrain = (XmlElement)tinfo.AppendChild(doc.CreateElement("Terrain"));
                
                ComboBox box = SafeGetField<ComboBox>(window.selector, "tileToGenerate");
                ComboBoxItem item = SafeGetProperty<ComboBoxItem>(box, "SelectedItem");
                terrain.InnerText = SafeGetProperty<string>(item, "Name").Replace("cmbx_","");
                IncrementProgress();

                XmlElement height = (XmlElement)tinfo.AppendChild(doc.CreateElement("Height"));

                //ComboBox box = SafeGetField<ComboBox>(window.selector, "tileToGenerate");
                //ComboBoxItem item = SafeGetProperty<ComboBoxItem>(box, "SelectedItem");
                height.InnerText = "HEIGHT_MEDIUM";
                IncrementProgress();

                ComboBox icebox = SafeGetField<ComboBox>(window.selector, "iceToGenerate");
                ComboBoxItem iceitem = SafeGetProperty<ComboBoxItem>(icebox, "SelectedItem");
                if (SafeGetProperty<string>(iceitem, "Name").Replace("cmbx_", "") != "NOICE")
                {
                    XmlElement ice = (XmlElement)tinfo.AppendChild(doc.CreateElement("IceType"));
                    ice.InnerText = SafeGetProperty<string>(iceitem, "Name").Replace("cmbx_", "");
                }
                IncrementProgress();
            }

            XmlWriter writer = XmlWriter.Create("product.xml", new XmlWriterSettings() {
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
