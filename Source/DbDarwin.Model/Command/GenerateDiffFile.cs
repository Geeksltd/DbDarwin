using Olive;

namespace DbDarwin.Model.Command
{
    public class GenerateDiffFile
    {
        public string SourceSchemaFile { get; set; }
        public string TargetSchemaFile { get; set; }
        public string OutputFile { get; set; }

        public bool IsValid => TargetSchemaFile.HasValue() && SourceSchemaFile.HasValue() &&
                               OutputFile.HasValue();

        public CompareType CompareType { get; set; }
    }
}
