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

namespace DbDarwin.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


        }


        private void SelectSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectSource.SelectedItem != null && ((ComboBoxItem)SelectSource.SelectedItem).Tag.ToString() == "1")
            {
                var result = new ConnectWindow().ShowDialog();
                if (result ?? false)
                {

                }
                else
                {
                    SelectSource.SelectedIndex = -1;
                }
            }
        }

        private void SelectTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectTarget.SelectedItem != null && ((ComboBoxItem)SelectTarget.SelectedItem).Tag.ToString() == "1")
            {
                var connect = new ConnectWindow();
                var result = connect.ShowDialog();
                if (result ?? false)
                {
                    //SelectTarget.Items.Add(new ComboBoxItem()
                    //{
                    //   Content = C
                    //});
                }
                else
                {
                    SelectTarget.SelectedIndex = -1;
                }
            }
        }
    }
}
