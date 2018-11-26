using Olive;

namespace DbDarwin.Model.Command
{
    public class GenerateScript
    {
        public string CurrentDiffFile { get; set; }
        public string MigrateSqlFile { get; set; }

        public bool IsValid => CurrentDiffFile.HasValue() && MigrateSqlFile.HasValue();
    }
}
