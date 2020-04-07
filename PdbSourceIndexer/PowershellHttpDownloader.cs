namespace PdbSourceIndexer
{
    using System.Collections.Generic;

    /// <summary>
    /// This is implementation of <see cref="IFilterVariables" /> service using the
    /// PowerShell cmdlet.
    /// </summary>
    /// <remarks>
    /// This downloader does not work if your URLs contain escaped characters (e.g. '%2F' instead of '/')
    /// because of a bug in older .NET. It decodes the escape sequences back to the characters which
    /// can cause 404.
    /// See https://stackoverflow.com/questions/25596564/percent-encoded-slash-is-decoded-before-the-request-dispatch
    /// </remarks>
    public class PowershellHttpDownloader : HttpDownloader
    {
        public PowershellHttpDownloader(int relativePathVariableIndex) : base(relativePathVariableIndex)
        {
        }

        public override string GetExtractCommand(string urlVarName, string targetFileVarName, IDictionary<string, string> variables)
        {
            return $"powershell -NoProfile -Command \"(New-Object System.Net.WebClient).DownloadFile('%{urlVarName}%', '%{targetFileVarName}%')\"";
        }
    }
}
