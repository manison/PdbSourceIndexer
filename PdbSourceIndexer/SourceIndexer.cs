namespace PdbSourceIndexer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class SourceIndexer
    {
        /// <summary>
        /// Debugging Tools for Windows installation path.
        /// </summary>
        public DirectoryInfo DebuggingToolsPath { get; set; }

        /// <summary>
        /// Source files root path.
        /// </summary>
        public DirectoryInfo SourceRootPath { get; set; }

        /// <summary>
        /// Collection of PDB files to index.
        /// </summary>
        public IEnumerable<FileInfo> SymbolFiles { get; set; }

        /// <summary>
        /// Source server information provider.
        /// </summary>
        public SourceServerProvider SourceServerProvider { get; set; }

        public ILogger Log { get; set; } 

        public void Run()
        {
            CheckDebuggingToolsPath();

            SourceRootPath = SourceRootPath ?? new DirectoryInfo(".");
            SourceServerProvider.SourceRootPath = SourceRootPath;
            SourceServerProvider.Log = Log;
            SourceServerProvider.Reset();

            int symbolFilesTotal = 0;
            int symbolFilesIndexed = 0;

            foreach (var symbolFile in SymbolFiles)
            {
                if (IndexSymbolFile(symbolFile))
                {
                    ++symbolFilesIndexed;
                }

                ++symbolFilesTotal;
            }

            if (symbolFilesTotal == 0)
            {
                Log.Warn("No symbol file found.");
            }
            else if (symbolFilesIndexed == 0)
            {
                Log.Warn("No symbol file indexed.");
            }
        }

        private void CheckDebuggingToolsPath()
        {
            if (DebuggingToolsPath == null)
            {
                DebuggingToolsPath = FindDebuggingToolsPath();
            }

            if (DebuggingToolsPath == null)
            {
                throw ThrowHelper.DebuggingToolsNotFound();
            }

            if (!DebuggingToolExists("pdbstr.exe"))
            {
                // Try the srcsrv subfolder.
                DebuggingToolsPath = new DirectoryInfo(Path.Combine(DebuggingToolsPath.FullName, "srcsrv"));
                if (!DebuggingToolExists("pdbstr.exe"))
                {
                    throw ThrowHelper.DebuggingToolsNotFound(DebuggingToolsPath);
                }
            }

            if (!DebuggingToolExists("srctool.exe"))
            {
                throw ThrowHelper.DebuggingToolsNotFound(DebuggingToolsPath);
            }
        }

        private DirectoryInfo FindDebuggingToolsPath()
        {
            // TODO
            return null;
        }

        private bool DebuggingToolExists(string toolName)
            => File.Exists(Path.Combine(DebuggingToolsPath.FullName, toolName));

        private bool IndexSymbolFile(FileInfo symbolFile)
        {
            var infos = GatherSourceFileInfos(symbolFile);
            if (infos.Any())
            {
                WriteSrcSrvStream(symbolFile, infos);
                Log.Info($"Symbol file {symbolFile.Name} indexed.");
                return true;
            }

            return false;
        }

        private IEnumerable<SourceFileInfo> GatherSourceFileInfos(FileInfo symbolFile)
        {
            var sourceInfos = new List<SourceFileInfo>();

            var sourceFiles = ReadToolLines("srctool.exe", $"-r \"{symbolFile.FullName}\"");
            int sourceFilesTotal = 0;
            int sourceFilesProcessed = 0;
            foreach (var sourceFile in sourceFiles)
            {
                var sourceFileInfo = SourceServerProvider.GetFileInfo(sourceFile);
                if (sourceFileInfo != null)
                {
                    ++sourceFilesProcessed;
                    sourceInfos.Add(sourceFileInfo);
                }

                ++sourceFilesTotal;
            }

            if (sourceFilesProcessed == 0)
            {
                Log.Warn($"Skipping symbol file {symbolFile.Name}. Source files unversioned.");
            }
            else if (sourceFilesTotal == 0)
            {
                Log.Warn($"Skipping symbol file {symbolFile.Name}. No source information available.");
            }

            return sourceInfos;
        }

        private IEnumerable<string> ReadToolLines(string toolName, string arguments)
            => ProcessHelper.ReadProcessLines(Path.Combine(DebuggingToolsPath.FullName, toolName), arguments);

        private void WriteSrcSrvStream(FileInfo symbolFile, IEnumerable<SourceFileInfo> sourceFiles)
        {
            string tmpFileName = Path.GetTempFileName();

            try
            {
                using (var writer = new StreamWriter(tmpFileName, false, Encoding.Default))
                {
                    writer.Write("SRCSRV: ini ------------------------------------------------\n");
                    writer.Write($"VERSION={SourceServerProvider.LanguageSpecification}\n");
                    writer.Write("INDEXVERSION=2\n");
                    writer.Write($"VERCTRL={SourceServerProvider.Name}\n");
                    writer.Write($"DATETIME={DateTime.Now:yyyy'/'MM'/'dd HH:mm:ss}\n");
                    writer.Write("SRCSRV: variables ------------------------------------------\n");
                    foreach (var (name, value) in SourceServerProvider.Variables)
                    {
                        writer.Write($"{name}={value}\n");
                    }
                    writer.Write("SRCSRV: source files ---------------------------------------\n");
                    foreach (var sourceFile in sourceFiles)
                    {
                        writer.Write($"{sourceFile.SrcSrvRecord}\n");
                    }
                    writer.Write("SRCSRV: end ------------------------------------------------\n");
                }

                int exitCode = ProcessHelper.Execute(Path.Combine(DebuggingToolsPath.FullName, "pdbstr.exe"), $"-w \"-p:{symbolFile.FullName}\" -s:srcsrv \"-i:{tmpFileName}\"");
                if (exitCode != 0)
                {
                    throw new Exception("Failed to write source server stream.");
                }
            }
            finally
            {
                File.Delete(tmpFileName);
            }
        }
    }
}
