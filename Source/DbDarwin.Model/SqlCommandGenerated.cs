using System;
using System.Collections.Generic;
using System.Text;
using Olive;

namespace DbDarwin.Model
{
    public class SqlCommandGenerated
    {
        public string Body { get; set; }
        public string AfterCommit { get; set; }
        public string Full => Body + "\r\n" + AfterCommit;

        public string Line = "\r\n";
        public string Tab = "\t";

        public void AppendBody(string command, LineEnum liner = LineEnum.NoLine)
        {
            Body = Build(Body, command, liner);
        }

        public string Build(string message, string command, LineEnum liner)
        {
            switch (liner)
            {
                case LineEnum.NoLine:
                    message = message + command;
                    break;
                case LineEnum.FirstLine:
                    message = message + Line + command;
                    break;
                case LineEnum.LastLine:
                    message = message + command + Line;
                    break;
                case LineEnum.FullLine:
                    message = message + Line + command + Line;
                    break;
                case LineEnum.Tab:
                    message = message + Tab + command;
                    break;
                case LineEnum.FirstLineWithTab:
                    message = message + Line + Tab + command;
                    break;
                case LineEnum.FullLineWithTab:
                    message = message + Line + Tab + command + Line;
                    break;
                case LineEnum.FirstLineWith2LastLine:
                    message = message + Line + command + Line + Line;
                    break;
            }

            return message;
        }

        public void AppendAfterCommit(string command, LineEnum liner = LineEnum.NoLine)
        {
            AfterCommit = Build(AfterCommit, command, liner);
        }
    }

    public enum LineEnum
    {
        NoLine,
        FirstLine,
        LastLine,
        FullLine,
        Tab,
        FirstLineWithTab,
        FullLineWithTab,
        FirstLineWith2LastLine
    }
}
