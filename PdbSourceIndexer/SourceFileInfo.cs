namespace PdbSourceIndexer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SourceFileInfo
    {
        public string FileName { get; }

        public virtual IEnumerable<object> Fields => Array.Empty<object>();

        public string SrcSrvRecord => String.Join("*", new string[] { FileName }.Concat(Fields));

        public SourceFileInfo(string fileName)
        {
            this.FileName = fileName;
        }
    }
}
