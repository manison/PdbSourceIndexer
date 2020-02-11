namespace PdbSourceIndexer
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public abstract class SourceServerProvider : IDisposable
    {
        /// <summary>
        /// Source files root path.
        /// </summary>
        public DirectoryInfo SourceRootPath { get; set; }

        public abstract string Name { get; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/language-specification-2
        /// </summary>
        public virtual int LanguageSpecification => 2;

        public abstract IEnumerable<(string Name, string Value)> Variables { get; }

        public ILogger Log { get; set; }

        protected SourceServerProvider()
        {
        }

        public virtual void Reset()
        {
        }

        public abstract SourceFileInfo GetFileInfo(string sourceFile);

        public virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
