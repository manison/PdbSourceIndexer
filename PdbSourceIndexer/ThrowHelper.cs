namespace PdbSourceIndexer
{
    using System;
    using System.IO;

    internal static class ThrowHelper
    {
        public static Exception DebuggingToolsNotFound(DirectoryInfo path = null)
            => new ArgumentException("Debugging tools for Windows were not found. Use --tools-path command line option to specify the installation path.");
    }
}
