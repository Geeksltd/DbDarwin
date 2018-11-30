using DbDarwin.Model;
using DbDarwin.Model.Command;
using DbDarwin.Service;
using Olive;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using DbDarwin.Model.Schema;

namespace DbDarwin.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RadioButton SelectedAddOrUpdateRadio { get; set; }

        public GeneratedScriptResult SelectedAddOrUpdate;

        public RadioButton SelectedRemoveRadio { get; private set; }

        public GeneratedScriptResult SelectedRemove;
        readonly Color RemoveItem = Color.FromArgb(255, 255, 192, 192);
        readonly Color AddItem = Color.FromArgb(255, 175, 220, 255);

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

        void SelectSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = ((ComboBoxItem)SelectSource.SelectedItem)?.Tag;
            if (tag?.ToString() == "1")
            {
                var connect = new ConnectWindow("SelectSource");
                var result = connect.ShowDialog();
                if (result == true)
                {
                    if (SelectSource.Items.Count > 1)
                        SelectSource.Items.RemoveAt(1);

                    SourceConnection = connect.ConnectionString;
                    SourceName = connect.ConnectionName;
                    SelectSource.Items.Add(new ComboBoxItem
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

        void SelectTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = ((ComboBoxItem)SelectTarget.SelectedItem)?.Tag;
            if (tag?.ToString() == "1")
            {
                var connect = new ConnectWindow("SelectTarget");
                var result = connect.ShowDialog();
                if (result == true)
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

        void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            CompareButton.IsEnabled = false;

            Task.Factory.StartNew(() =>
              {
                  UpdateState($"Extracting {SourceName} Schema...");
                  using (var service = new ExtractSchemaService(new ExtractSchema
                  { ConnectionString = SourceConnection, OutputFile = "Source.xml" }))
                      service.ExtractSchema();
                  UpdateState($"Extracted {SourceName} Schema.");

                  UpdateState($"Extracting {TargetName} Schema...");
                  using (var service = new ExtractSchemaService(new ExtractSchema
                  { ConnectionString = TargetConnection, OutputFile = "Target.xml" }))
                      service.ExtractSchema();
                  UpdateState($"Extracted {TargetName} Schema.");

                  UpdateState("Comparing Databases...");
                  using (var service = new CompareSchemaService())
                      service.StartCompare(new GenerateDiffFile
                      {
                          SourceSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Source.xml",
                          TargetSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Target.xml",
                          OutputFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml"
                      });
                  UpdateState("Databases Compared.");
                  GenerateSqlFileAndShowUpdates();
              });
        }

        void GenerateSqlFileAndShowUpdates()
        {
            var engine = new GenerateScriptService();
            var result = engine.GenerateScript(
                new GenerateScript
                {
                    CurrentDiffFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml",
                    MigrateSqlFile = AppContext.BaseDirectory + "\\output.sql"
                });

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.CompareButton.IsEnabled = true;

                    TreeViewRoot.Items.Clear();

                    foreach (var table in result.GroupBy(x => x.FullTableName).OrderBy(x => x.Key))
                    {
                        var treeViewItems = new TreeViewItem
                        {
                            Header = table.Key,
                            IsExpanded = true
                        };

                        TreeViewRoot.Items.Add(treeViewItems);

                        foreach (var script in table.ToList())
                        {
                            RadioButton checkbox;

                            var removed = new SolidColorBrush(RemoveItem);
                            var add = new SolidColorBrush(AddItem);
                            if (script.Mode == Model.ViewMode.Add || script.Mode == Model.ViewMode.Update || script.Mode == Model.ViewMode.Rename)
                            {
                                checkbox = new RadioButton
                                {
                                    Tag = "AddOrUpdate",
                                    Content = script.Title,
                                    DataContext = script,
                                    GroupName = "AddOrUpdateGroup",
                                    Background = add,


                                };
                            }
                            else
                            {
                                checkbox = new RadioButton
                                {
                                    Tag = "Remove",
                                    Content = script.Title,
                                    DataContext = script,
                                    GroupName = "RemoveGroup",
                                    Background = removed,

                                };
                            }

                            checkbox.Click += Checkbox_Click;
                            treeViewItems.Items.Add(checkbox);
                        }
                    }

                    GenerateButton.IsEnabled = true;
                }
            }));
        }

        public void ValidateSelectedObject()
        {
            ActuallyRename.IsEnabled = SelectedAddOrUpdate != null &&
                                       SelectedRemove != null &&
                                       SelectedAddOrUpdate.Mode == ViewMode.Add &&
                                       SelectedRemove.Mode == ViewMode.Delete &&
                                       SelectedAddOrUpdate.ObjectType == SQLObject.Column &&
                                       SelectedRemove.ObjectType == SQLObject.Column
                                       && String.Equals(SelectedAddOrUpdate.FullTableName, SelectedRemove.FullTableName, StringComparison.CurrentCultureIgnoreCase);

            ActuallyUpdate.IsEnabled = SelectedAddOrUpdate != null &&
                                       SelectedRemove != null &&
                                       SelectedAddOrUpdate.Mode == ViewMode.Add &&
                                       SelectedRemove.Mode == ViewMode.Delete &&
                                       SelectedAddOrUpdate.ObjectType == SQLObject.RowData &&
                                       SelectedRemove.ObjectType == SQLObject.RowData
                                       && String.Equals(SelectedAddOrUpdate.FullTableName, SelectedRemove.FullTableName, StringComparison.CurrentCultureIgnoreCase);
        }

        void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).Tag.ToString() == "AddOrUpdate")
            {
                SelectedAddOrUpdateRadio = (RadioButton)sender;
                SelectedAddOrUpdate = (GeneratedScriptResult)((RadioButton)sender).DataContext;
            }
            else if (((RadioButton)sender).Tag.ToString() == "Remove")
            {
                SelectedRemoveRadio = (RadioButton)sender;
                SelectedRemove = (GeneratedScriptResult)((RadioButton)sender).DataContext;
            }

            ValidateSelectedObject();
            ShowScript((RadioButton)sender);
        }

        public void UpdateState(string content)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                    mainWindow.StatusLabel.Content = content;
            }));
        }

        public void ShowScript(RadioButton control)
        {
            ShowScript(((GeneratedScriptResult)control.DataContext).SQLScript);
        }

        public void ShowScript(string message)
        {
            RichTextBox.Document.Blocks.Clear();
            RichTextBox.Document.Blocks.Add(new Paragraph(new Run(message)));
        }

        void GenerateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var diffFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml";
            var sqlOutput = AppContext.BaseDirectory + "\\output.sql";
            var engine = new GenerateScriptService();
            var result = engine.GenerateScript(
                new GenerateScript
                {
                    CurrentDiffFile = diffFile,
                    MigrateSqlFile = sqlOutput
                });
            if (result.Any(x =>
                x.Mode == ViewMode.Add && x.ObjectType == SQLObject.Column &&
                x.SQLScript.ToUpper().Contains("NOT NULL") && !x.SQLScript.ToUpper().Contains("DEFAULT")))
            {
                var database = CompareSchemaService.LoadXMLFile(diffFile);
                var count = database.Update?.Tables?
                    .Where(v => v.Add?.Columns != null)
                    .SelectMany(x => x.Add.Columns)
                    .Count(c => c.IS_NULLABLE == "NO" && c.COLUMN_DEFAULT.IsEmpty());
                if (count > 0)
                {
                    var needToSave = false;
                    var tables = database.Update?.Tables?.Where(v => v.Add?.Columns != null).ToList();
                    foreach (var table in tables)
                    {
                        foreach (var column in table.Add.Columns.Where(x => x.IS_NULLABLE == "NO" && x.COLUMN_DEFAULT.IsEmpty()))
                        {
                            var description = $"Column {column.Name} on table {table.Name} need default value";
                            var defaultWindow = new SetDefaultValueWindow(description);
                            if (defaultWindow.ShowDialog() == true)
                            {
                                needToSave = true;
                                column.COLUMN_DEFAULT = defaultWindow.DefaultValue;
                            }
                        }
                    }

                    if (needToSave)
                    {
                        ExtractSchemaService.SaveToFile(database, "diff.xml");
                        engine = new GenerateScriptService();
                        engine.GenerateScript(
                            new GenerateScript
                            {
                                CurrentDiffFile = diffFile,
                                MigrateSqlFile = sqlOutput
                            });
                    }
                }
            }

            Process.Start(sqlOutput);
        }

        void ActuallyRename_OnClick(object sender, RoutedEventArgs e)
        {
            var database = CompareSchemaService.LoadXMLFile(AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml");
            var table = database.Update?.Tables?.FirstOrDefault(x =>
                String.Equals(x.FullName, SelectedAddOrUpdate.FullTableName, StringComparison.CurrentCultureIgnoreCase));
            if (table == null) return;

            var newSchema = table.Add.Columns.FirstOrDefault(x => x.Name == SelectedAddOrUpdate.ObjectName);
            var oldSchema = table.Remove.Columns.FirstOrDefault(x => x.Name == SelectedRemove.ObjectName);
            if (oldSchema == null || newSchema == null) return;

            newSchema.SetName = newSchema.Name;
            newSchema.COLUMN_NAME = oldSchema.Name;
            table.Add.Columns.Remove(newSchema);
            table.Remove.Columns.Remove(oldSchema);

            if (table.Update == null)
                table.Update = new Model.Schema.Table
                {
                    Columns = new List<Model.Schema.Column> { newSchema }
                };
            else if (table.Update.Columns == null)
                table.Update.Columns = new List<Model.Schema.Column> { newSchema };
            else
                table.Update.Columns.Add(newSchema);
            ExtractSchemaService.SaveToFile(database, "diff.xml");
            GenerateSqlFileAndShowUpdates();
        }

        void TreeViewRemove_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tree = (TreeView)sender;
            if (tree.SelectedItem != null)
            {
                if (tree.SelectedItem is RadioButton control)
                    ShowScript(control);
                else if (tree.SelectedItem is TreeViewItem tab)
                    ShowScript(tab);
            }
        }

        void ShowScript(TreeViewItem tab)
        {
            var builder = new StringBuilder();
            foreach (var item in tab.Items)
                if (item is RadioButton radio)
                    builder.AppendLine(((GeneratedScriptResult)radio.DataContext).SQLScript);
            ShowScript(builder.ToString());
        }

        private void ActuallyUpdate_OnClick(object sender, RoutedEventArgs e)
        {

            var path = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml";
            var database = XDocument.Parse(File.ReadAllText(path));



            var tablesElements = (from tables in database.Descendants("Table")
                                  where tables.Attribute("Name").Value.Equals(SelectedAddOrUpdate.TableName) &&
                                        (tables.Attribute("Schema")?.Value == String.Empty || tables.Attribute("Schema")?.Value == SelectedAddOrUpdate.Schema)
                                  select tables).FirstOrDefault();

            if (tablesElements == null)
                return;
            var addRecords = tablesElements.Descendants("add").Descendants("Data").Descendants("Row");
            var deleteRecords = tablesElements.Descendants("remove").Descendants("Data").Descendants("Row");


            var findRemove = (from rows in deleteRecords
                              where rows.Attributes("Name").Any() && rows.Attribute("Name").Value.Equals(SelectedRemove.ObjectName)
                              select rows).FirstOrDefault();
            findRemove?.Remove();

            var findAdd = (from rows in addRecords
                           where rows.Attributes("Name").Any() && rows.Attribute("Name").Value.Equals(SelectedAddOrUpdate.ObjectName)
                           select rows).FirstOrDefault();


            if (findAdd != null)
            {
                var updateElement = tablesElements.Descendants("update").FirstOrDefault();
                var columnTypes = tablesElements.Descendants("add").Descendants("Data").Descendants("ColumnTypes")
                    .FirstOrDefault();



                if (updateElement != null)
                {
                    var dataNode = updateElement.Descendants("Data").FirstOrDefault();
                    if (dataNode != null)
                        dataNode.Add(findAdd);
                    else
                    {
                        var updateData = new XElement(XName.Get("Data"));
                        updateData.Add(columnTypes);
                        updateData.Add(findAdd);
                        updateElement.Add(updateData);
                    }
                }
                else
                {
                    var updateAdd = new XElement(XName.Get("update"));
                    var updateData = new XElement(XName.Get("Data"));
                    updateData.Add(columnTypes);
                    updateData.Add(findAdd);
                    updateAdd.Add(updateData);
                    tablesElements.Add(updateAdd);
                }

                findAdd.Remove();


                var tree = TreeViewRoot.Items.Cast<TreeViewItem>().FirstOrDefault(x => x.Header.ToString() == SelectedRemove.FullTableName);
                tree?.Items.Remove(SelectedRemoveRadio);

                var xRoot = new XmlRootAttribute { ElementName = "Data", IsNullable = true };
                var dataAdd = tablesElements?.Descendants("add").Descendants("Data").FirstOrDefault();
                var reader = new StringReader(dataAdd?.ToString());
                var ser = new XmlSerializer(typeof(TableData), xRoot);
                var addTableData = (TableData)ser.Deserialize(reader);

                var dic = (IDictionary<string, object>)SelectedRemove.OrginalObject;
                var dicAdd = (IDictionary<string, object>)SelectedAddOrUpdate.OrginalObject;
                SelectedAddOrUpdate.SQLScript = GenerateScriptService.CreateUpdateRowScript(dicAdd,
                    SelectedAddOrUpdate.TableName, SelectedAddOrUpdate.Schema, addTableData.ColumnTypes);
                dicAdd.Remove("Name");
                SelectedAddOrUpdateRadio.Content = $"Update record {GenerateScriptService.GenerateName(dic)} to set {GenerateScriptService.GenerateName(dicAdd)}";

                SelectedAddOrUpdate.Mode = ViewMode.Update;








            }
            database.Save(path);


            //GenerateSqlFileAndShowUpdates();
        }
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