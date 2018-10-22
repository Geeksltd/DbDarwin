
using Olive;

namespace DbDarwin.Model
{
    public static class StringExtention
    {
        public static string To_ON_OFF(this string str)
        {
            if (str.IsEmpty()) return "OFF";
            return str.ToLower().Trim() == "true" ? "ON" : "OFF";
        }

        public static bool ToBoolean(this string str)
        {
            if (str.IsEmpty()) return false;
            return str.ToLower().Trim() == "true";
        }
    }
}