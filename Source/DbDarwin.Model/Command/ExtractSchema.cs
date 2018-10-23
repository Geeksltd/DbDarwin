using System;
using System.Collections.Generic;
using System.Text;
using GCop.Core;

namespace DbDarwin.Model.Command
{
    public class ExtractSchema
    {
        public string ConnectionString { get; set; }
        public string OutputFile { get; set; }

        public bool IsValid => ConnectionString.HasValue() && OutputFile.HasValue();
    }
}
