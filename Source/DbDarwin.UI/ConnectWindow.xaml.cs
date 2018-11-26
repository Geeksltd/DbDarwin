using DbDarwin.Service;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DbDarwin.UI
{
    /// <summary>
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        public string ConnectionString { get; set; }
        public string ConnectionName { get; set; }
        public string _connectionType { get; set; }
        public ConnectWindow(string connectionType)
        {
            _connectionType = connectionType;
            InitializeComponent();

            var data = ManageConnectionData.ReadConnection(_connectionType);
            if (data != null)
            {
                UserName.Text = data.UserName;
                Password.Password = data.Password;
                RememberPassword.IsChecked = true;
            }

            ServerName.Text = System.Environment.MachineName;
        }

        void DatabaseName_DropDownOpened(object sender, EventArgs e)
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
                    connection = $"Data Source={ServerName.Text};Initial Catalog=master;Integrated Security=False;User Id={UserName.Text};Password={Password.Password};Connect Timeout=30;";
                }

                using (var sql = new System.Data.SqlClient.SqlConnection(connection))
                {
                    sql.Open();
                    var databases = SqlService.LoadDataAsString(sql, "References",
                        "SELECT name FROM [master].[dbo].[sysdatabases] order by name");
                    DatabaseName.Items.Clear();
                    foreach (var database in databases)
                        DatabaseName.Items.Add(database);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectionName = ServerName.Text + "." + DatabaseName.SelectedValue;
            if (((ComboBoxItem)Authentication.SelectedItem).Tag.ToString() == "1")
                ConnectionString = $"Data Source={ServerName.Text};Initial Catalog={DatabaseName.SelectedValue};Integrated Security=True;Connect Timeout=60;";
            else
                ConnectionString = $"Data Source={ServerName.Text};Initial Catalog={DatabaseName.SelectedValue};Integrated Security=False;User Id={UserName.Text};Password={Password.Password};Connect Timeout=60;";

            if (RememberPassword.IsEnabled)
                ManageConnectionData.SaveConnection(_connectionType, UserName.Text, Password.Password);

            DialogResult = true;
        }

        void Authentication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = (ComboBox)sender;
            var item = (ComboBoxItem)combo.SelectedItem;
            if (item?.Tag != null)
                RememberPassword.IsEnabled = Password.IsEnabled = UserName.IsEnabled = item.Tag.ToString() != "1";
        }
    }
}