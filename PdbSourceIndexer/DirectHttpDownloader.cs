namespace PdbSourceIndexer
{
    using System.Collections.Generic;

    public sealed class DirectHttpDownloader : IFilterVariables
    {
        public IEnumerable<(string Name, string Value)> FilterVariables(IEnumerable<(string Name, string Value)> variables)
            => variables;

        public (string Name, string Value) FilterVariable((string Name, string Value) variable)
            => variable;
    }
}
