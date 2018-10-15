using System;
using System.Collections.Generic;
using System.Text;

namespace DbDarwin.Model
{
    public static class StringExtention
    {
        public static string Convert_ON_OFF(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return "OFF";
            return str.ToLower() == "true" ? "ON" : "OFF";
        }
    }
}
