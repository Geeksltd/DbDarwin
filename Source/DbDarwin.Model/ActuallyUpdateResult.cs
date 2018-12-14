using System.Xml.Linq;

namespace DbDarwin.Model
{
    public class ActuallyUpdateResult
    {
        public bool Result { get; set; }
        public XElement Data { get; set; }
    }
}
