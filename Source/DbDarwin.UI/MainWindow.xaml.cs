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

        public void EnableCompare()
        {
            CompareButton.IsEnabled = SelectSource.Items.Count > 1 && SelectTarget.Items.Count > 1;
        }


        private void SelectSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = ((ComboBoxItem)SelectSource.SelectedItem)?.Tag;
            if (tag != null && tag.ToString() == "1")
            {
                var connect = new ConnectWindow();
                var result = connect.ShowDialog();
                if (result ?? false)
                {
                    if (SelectSource.Items.Count > 1)
                        SelectSource.Items.RemoveAt(1);
                    SelectSource.Items.Add(new ComboBoxItem()
                    {
                        Content = connect.ConnectionName,
                        DataContext = connect.ConnectionString,
                        IsSelected = true,
                    });



                }
                else
                {
                    SelectSource.SelectedIndex = -1;
                }
            }
            EnableCompare();
        }

        private void SelectTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = ((ComboBoxItem)SelectTarget.SelectedItem)?.Tag;
            if (tag != null && tag.ToString() == "1")
            {
                var connect = new ConnectWindow();
                var result = connect.ShowDialog();
                if (result ?? false)
                {
                    if (SelectTarget.Items.Count > 1)
                        SelectTarget.Items.RemoveAt(1);
                    SelectTarget.Items.Add(new ComboBoxItem()
                    {
                        Content = connect.ConnectionName,
                        DataContext = connect.ConnectionString,
                        IsSelected = true,
                    });
                }
                else
                {
                    SelectTarget.SelectedIndex = -1;
                }
            }
            EnableCompare();
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
