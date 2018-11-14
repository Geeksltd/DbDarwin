using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Threading;
using DbDarwin.Model.Command;
using DbDarwin.Service;

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
        public string SourceConnection { get; set; }
        public string TargetConnection { get; set; }

        public string SourceName { get; set; }
        public string TargetName { get; set; }

        private void SelectSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = ((ComboBoxItem)SelectSource.SelectedItem)?.Tag;
            if (tag?.ToString() == "1")
            {
                var connect = new ConnectWindow();
                var result = connect.ShowDialog();
                if (result == true)
                {
                    if (SelectSource.Items.Count > 1)
                        SelectSource.Items.RemoveAt(1);

                    SourceConnection = connect.ConnectionString;
                    SourceName = connect.ConnectionName;
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

                    TargetConnection = connect.ConnectionString;
                    TargetName = connect.ConnectionName;
                    SelectTarget.Items.Add(new ComboBoxItem
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


            CompareButton.IsEnabled = false;

            Task.Factory.StartNew(() =>
              {
                  UpdateState($"Extracting {SourceName} Schema...");
                  ExtractSchemaService.ExtractSchema(new ExtractSchema
                  {
                      ConnectionString = SourceConnection,
                      OutputFile = "Source.xml"
                  });
                  UpdateState($"Extracted {SourceName} Schema.");


                  UpdateState($"Extracting {TargetName} Schema...");
                  ExtractSchemaService.ExtractSchema(new ExtractSchema
                  {
                      ConnectionString = TargetConnection,
                      OutputFile = "Target.xml"
                  });
                  UpdateState($"Extracted {TargetName} Schema.");




                  UpdateState("Comparing Databases...");
                  CompareSchemaService.StartCompare(new GenerateDiffFile
                  {
                      SourceSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Source.xml",
                      TargetSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Target.xml",
                      OutputFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml"
                  });
                  UpdateState("Databases Compared.");








                  Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                  {
                      if (Application.Current.MainWindow is MainWindow mainWindow)
                      {
                          mainWindow.CompareButton.IsEnabled = true;

                          var database = CompareSchemaService.LoadXMLFile(AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml");
                          if (database.Add?.Tables != null)
                              foreach (var table in database.Add.Tables)
                              {
                                  ListBoxAdd.Items.Add(new CheckBox
                                  {
                                      IsChecked = true,
                                      Content = "New table " + table.FullName
                                  });
                              }
                          if (database.Update?.Tables != null)
                              foreach (var table in database.Update.Tables)
                              {
                                  ListBoxAdd.Items.Add(new CheckBox
                                  {
                                      IsChecked = true,
                                      Content = "Update table " + table.FullName
                                  });
                              }

                          if (database.Remove?.Tables != null)
                              foreach (var table in database.Remove.Tables)
                              {
                                  ListBoxRemove.Items.Add(new CheckBox
                                  {
                                      IsChecked = true,
                                      Content = "Remove table " + table.FullName
                                  });
                              }
                      }
                  }));

              });






        }


        public void UpdateState(string content)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                    mainWindow.StatusLabel.Content = content;
            }));
        }

        //private void OnDataUpdate(string data)
        //{
        //    var handler = DataUpdate;
        //    handler?.Invoke(this, new PerformanceEventArgs(data));
        //}


        //private void HandleDataUpdate(object sender, PerformanceEventArgs e)
        //{
        //    // dispatch the modification to the text box to the UI thread (main window dispatcher)
        //    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new OneArgDelegate(e.Data));
        //}
        //private delegate void OneArgDelegate(String arg);
        //public event EventHandler<PerformanceEventArgs> DataUpdate;
    }

    public class PerformanceEventArgs : EventArgs
    {
        public string Data { get; set; }
        public PerformanceEventArgs(string data)
        {
            Data = data;
        }


    }
}
