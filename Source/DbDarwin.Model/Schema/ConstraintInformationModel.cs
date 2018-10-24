using DbDarwin.Model.Schema;

namespace DbDarwin.Model.Schema
{
    public class ConstraintInformationModel
    {
        public Index Index { get; set; }
        public IndexColumns IndexColumn { get; set; }
        public SystemColumns SystemColumn { get; set; }
    }
}