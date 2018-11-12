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
using DbDarwin.Service;

namespace DbDarwin.UI
{
    /// <summary>
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        public ConnectWindow()
        {
            InitializeComponent();
        }

        private void DatabaseName_DropDownOpened(object sender, EventArgs e)
        {
            try
            {


                var connection = "";
                if (((ComboBoxItem)Authentication.SelectedItem).Tag.ToString() == "1")
                {
                    connection = $"Data Source={ServerName.Text};Initial Catalog=master;Integrated Security=True;Connect Timeout=30;";
                }
                else
                {
                    connection = $"Data Source={ServerName.Text};Initial Catalog=master;Integrated Security=False;User Id={UserName.Text};Password={Password.Text};Connect Timeout=30;";
                }

                using (var sql = new System.Data.SqlClient.SqlConnection(connection))
                {

                    sql.Open();
                    var databases = SqlService.LoadDataAsString(sql, "References",
                        "SELECT name FROM [master].[dbo].[sysdatabases] order by name");
                    DatabaseName.Items.Clear();
                    foreach (var database in databases)
                    {
                        DatabaseName.Items.Add(database);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
