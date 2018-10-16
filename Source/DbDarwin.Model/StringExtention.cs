using System;
using System.Collections.Generic;
using System.Text;

namespace DbDarwin.Model
{
    public static class StringExtention
    {
        public static string To_ON_OFF(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return "OFF";
            return str.ToLower().Trim() == "true" ? "ON" : "OFF";
        }

        public static bool ToBoolean(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            return str.ToLower().Trim() == "true" ? true : false;
        }

    }
}
