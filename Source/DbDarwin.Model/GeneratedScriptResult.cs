using Olive;
using System.Linq;

namespace DbDarwin.Model
{
    public class GeneratedScriptResult
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string SQLScriptInitial { get; set; }
        public string SQLScript { get; set; }
        public ViewMode Mode { get; set; }
        public int Order { get; set; }
        public string FullTableName { get; set; }

        public string TableName => FullTableName.Split('.').LastOrDefault();
        public string Schema => FullTableName.Split('.').FirstOrDefault();
        public string ObjectName { get; set; }
        public SQLObject ObjectType { get; set; }
        public dynamic OrginalObject { get; set; }
    }

    public enum ViewMode
    {
        Add = 1,
        Update = 2,
        Rename = 3,
        Delete = 4,

    }
}
