using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Reflection;
using System.IO;

namespace OffworldMapfillGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public TerrainSelector selector;

        public MainWindow()
        {
            InitializeComponent();
            selector = new TerrainSelector();
        }

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            var gen = new Generating
            {
                Owner = this
            };
            gen.Show();

            IsEnabled = false;

            gen.OnGenerateClose += new Action(()=> { IsEnabled = true; });

            gen.Generate(this);
            //t.Join();
        }

        private void SelectTerrain(object sender, RoutedEventArgs e)
        {
            selector.Owner = this;
            selector.Show();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            selector.ReallyClose = true;
            selector.Close();
        }
    }
}
