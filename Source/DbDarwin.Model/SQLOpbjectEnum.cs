namespace DbDarwin.Model
{
    public enum SQLObject
    {
        Table = 1,
        Column = 2,
        Index = 3,
        PrimaryKey = 4,
        ForeignKey = 5,
        RowData = 6
    }

    public enum SQLAuthenticationType
    {
        WindowsAuthentication = 1,
        SQLAuthentication = 2
    }
    public enum CompareType
    {
        Schema = 1,
        Data = 2
    }
}
