namespace PdbSourceIndexer
{
    using System.Collections.Generic;

    public class WgetHttpDownloader : HttpDownloader
    {
        public WgetHttpDownloader(int relativePathVariableIndex) : base(relativePathVariableIndex)
        {
        }

        public WgetHttpDownloader(string targetFile) : base(targetFile)
        {
        }

        public override string GetExtractCommand(string urlVarName, string targetFileVarName, IDictionary<string, string> variables)
        {
            return $"wget -O \"%{targetFileVarName}%\" \"%{urlVarName}%\"";
        }
    }
}
