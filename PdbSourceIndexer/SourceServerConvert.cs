namespace PdbSourceIndexer
{
    public static class SourceServerConvert
    {
        // From https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/language-specification-1:
        // Two consecutive percent sign characters are interpreted as a single percent sign.
        public static string EscapeString(string s) => s.Replace("%", "%%");
    }
}
