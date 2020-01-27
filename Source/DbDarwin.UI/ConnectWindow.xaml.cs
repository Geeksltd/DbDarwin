using DbDarwin.Model;
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
        public SQLAuthenticationType CurrentAuthentication { get; set; }
        public string DefaultServer => @".\SQLEXPRESS";

        public ConnectWindow(string connectionType)
        {
            _connectionType = connectionType;
            InitializeComponent();

            var data = ManageConnectionData.ReadConnection(_connectionType);
            if(data != null)
            {

                ServerName.Text = data.ServerName ?? DefaultServer;
                UserName.Text = data.UserName;
                if(data.Authentication != 0)
                    Authentication.SelectedIndex = ((int) data.Authentication) - 1;
                Password.Password = data.Password;
                RememberPassword.IsChecked = data.RememberPassword ?? true;
                DatabaseName.SelectedValue = DatabaseName.Text = data.DatabaseName;
            } else
                ServerName.Text = DefaultServer; //System.Environment.MachineName;



        }

        private void DatabaseName_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                var connection = "";
                CurrentAuthentication = (SQLAuthenticationType) int.Parse(((ComboBoxItem) Authentication.SelectedItem).Tag.ToString());

                if(CurrentAuthentication == SQLAuthenticationType.WindowsAuthentication)
                    connection = $"Data Source={ServerName.Text};Initial Catalog=master;Integrated Security=True;Connect Timeout=30;";
                else
                    connection = $"Data Source={ServerName.Text};Initial Catalog=master;Integrated Security=False;User Id={UserName.Text};Password={Password.Password};Connect Timeout=30;";

                using(var sql = new System.Data.SqlClient.SqlConnection(connection))
                {
                    sql.Open();
                    var databases = SqlService.LoadDataAsString(sql, "References",
                        "SELECT name FROM [master].[dbo].[sysdatabases] order by name");
                    DatabaseName.Items.Clear();
                    foreach(var database in databases)
                        DatabaseName.Items.Add(database);
                }
            } catch(Exception exception)
            {
                App.ShowAndLogError(exception);
                MessageBox.Show(exception.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectionName = ServerName.Text + "." + DatabaseName.SelectedValue;
            CurrentAuthentication = (SQLAuthenticationType) int.Parse(((ComboBoxItem) Authentication.SelectedItem).Tag.ToString());

            if(CurrentAuthentication == SQLAuthenticationType.WindowsAuthentication)
                ConnectionString = $"Data Source={ServerName.Text};Initial Catalog={DatabaseName.SelectedValue};Integrated Security=True;Connect Timeout=60;";
            else
                ConnectionString = $"Data Source={ServerName.Text};Initial Catalog={DatabaseName.SelectedValue};Integrated Security=False;User Id={UserName.Text};Password={Password.Password};Connect Timeout=60;";

            ManageConnectionData.SaveConnection(_connectionType, new ConnectionData {

                Authentication = CurrentAuthentication,
                Password = (RememberPassword.IsChecked ?? true) ? Password.Password : "",
                RememberPassword = RememberPassword.IsChecked,
                DatabaseName = DatabaseName.SelectedValue?.ToString(),
                ServerName = ServerName.Text,
                UserName = UserName.Text,
            });
            DialogResult = true;
        }

        private void Authentication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = (ComboBox) sender;
            var item = (ComboBoxItem) combo.SelectedItem;
            if(item?.Tag != null)
                RememberPassword.IsEnabled = Password.IsEnabled = UserName.IsEnabled = item.Tag.ToString() != "1";
        }
    }
}
