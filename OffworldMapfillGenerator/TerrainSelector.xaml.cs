using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Shapes;

namespace OffworldMapfillGenerator
{
    /// <summary>
    /// Interaction logic for TerrainSelector.xaml
    /// </summary>
    public partial class TerrainSelector : Window
    {
        public TerrainSelector()
        {
            InitializeComponent();
        }

        public bool ReallyClose = false;

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ReallyClose)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class IsCheckedVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Visibility) && parameter.GetType() == typeof(string) && value.GetType() == typeof(bool))
            {
                bool invert = (string)parameter == "invert";
                bool input = (bool)value;
                Visibility vis = (invert ? !input : input) ? Visibility.Visible : Visibility.Collapsed;

                return vis;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(bool) && parameter.GetType() == typeof(string) && value.GetType() == typeof(Visibility))
            {
                bool invert = (string)parameter == "invert";
                bool input = (Visibility)value == Visibility.Visible;
                

                return invert ? !input : input;
            }

            return false;
        }
    }
}
