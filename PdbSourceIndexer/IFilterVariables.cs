namespace PdbSourceIndexer
{
    using System.Collections.Generic;

    public interface IFilterVariables
    {
        IEnumerable<(string Name, string Value)> FilterVariables(IEnumerable<(string Name, string Value)> variables);

        (string Name, string Value) FilterVariable((string Name, string Value) variable);
    }
}
