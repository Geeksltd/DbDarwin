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
using System.Windows.Shapes;

namespace DbDarwin.UI
{
    /// <summary>
    /// Interaction logic for SetDefaultValueWindow.xaml
    /// </summary>
    public partial class SetDefaultValueWindow : Window
    {
        public string DefaultValue { get; set; }
        public SetDefaultValueWindow(string description)
        {
            InitializeComponent();
            Description.Content = description;
        }



        private void SetDefaultValueButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DefaultValueTextBox.Text.Length > 0)
            {
                DefaultValue = DefaultValueTextBox.Text;
                DialogResult = true;
            }
        }
    }
}
