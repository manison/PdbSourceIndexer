namespace PdbSourceIndexer
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Source server does not support downloading files from URLs containing the query part.
    /// For these cases we have to use workaround with external downloader (e.g. wget, curl).
    /// Using the <see cref="IFilterVariables"/> service we transform variables from direct
    /// download to indirect download.
    /// </summary>
    public abstract class HttpDownloader : IFilterVariables
    {
        protected HttpDownloader()
        {
        }

        protected HttpDownloader(int relativePathVariableIndex) : this($"%targ%\\%fnbksl%(%var{relativePathVariableIndex}%)")
        {
        }

        protected HttpDownloader(string targetFile)
        {
            TargetFile = targetFile;
        }

        public virtual IEnumerable<(string Name, string Value)> FilterVariables(IEnumerable<(string Name, string Value)> variables)
        {
            var dict = new Dictionary<string, string>();
            foreach (var (name, value) in variables)
            {
                dict.Add(name, value);
            }

            string httpExtract = dict["HTTP_EXTRACT_TARGET"];
            dict.Remove("HTTP_EXTRACT_TARGET");
            dict["RAWURL"] = httpExtract;
            dict["TRGFILE"] = TargetFile;
            dict["SRCSRVTRG"] = "%TRGFILE%";
            string newExtract = GetExtractCommand("RAWURL", "TRGFILE", dict);
            dict["SRCSRVCMD"] = newExtract;

            return dict.Select(variable => (variable.Key, variable.Value));
        }

        public abstract string GetExtractCommand(string urlVarName, string targetFileVarName, IDictionary<string, string> variables);

        public (string Name, string Value) FilterVariable((string Name, string Value) variable)
            => variable;

        public virtual string TargetFile
        {
            get;
            protected set;
        }
    }
}
