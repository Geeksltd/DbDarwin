using System;
using System.Collections.Generic;
using System.Text;

namespace DbDarwin.Model
{
    public class GeneratedScriptResult
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string SQLScript { get; set; }
        public ViewMode Mode { get; set; }
        public int Order { get; set; }
    }

    public enum ViewMode
    {
        Add = 1,
        Update = 2,
        Delete = 3
    }
}
