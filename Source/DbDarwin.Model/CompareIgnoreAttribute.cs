using System;

namespace DbDarwin.Model
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CompareIgnoreAttribute : Attribute
    {
    }
}
