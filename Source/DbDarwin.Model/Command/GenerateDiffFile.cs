using System;
using System.Collections.Generic;
using System.Text;
using GCop.Core;

namespace DbDarwin.Model.Command
{
    public class GenerateDiffFile
    {
        public string CurrentFile { get; set; }
        public string NewSchemaFile { get; set; }
        public string OutputFile { get; set; }

        public bool IsValid => CurrentFile.HasValue() && NewSchemaFile.HasValue() &&
                               OutputFile.HasValue();
    }
}
