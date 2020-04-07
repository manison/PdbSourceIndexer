namespace PdbSourceIndexer
{
    using LibGit2Sharp;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class GitSourceFileInfo : SourceFileInfo
    {
        public IRepository Repository { get; }

        public string RepositoryRelativeFileName { get; }

        public string CommitId { get; }

        public override IEnumerable<object> Fields
        {
            get
            {
                yield return Repository.Info.Path;
                yield return CommitId;
            }
        }

        public GitSourceFileInfo(string sourceFileName, IRepository repository, string repositoryRelativeFileName, string commitId) : base(sourceFileName)
        {
            this.Repository = repository;
            this.RepositoryRelativeFileName = repositoryRelativeFileName;
            this.CommitId = commitId;
        }
    }

    [Browsable(false)]
    public class GitSourceServerProvider : SourceServerProvider
    {
        public override string Name => "git";

        private Dictionary<string, Repository> _repositories;

        public override IEnumerable<(string Name, string Value)> Variables => Array.Empty<(string, string)>();

        public GitSourceServerProvider()
        {
            _repositories = new Dictionary<string, Repository>();
        }

        public override SourceFileInfo GetFileInfo(string sourceFile)
        {
            var sourceFilePath = CanonicalizeSourceFilePath(sourceFile);
            if (sourceFilePath == null || !sourceFilePath.Exists)
            {
                return null;
            }

            // Obtain file name with exact letter-casing as it is stored on filesystem.
            // Filenames in PDB seem to be sometimes lowercased.
            sourceFilePath = GetExactFileName(sourceFilePath);

            var repository = FindGitRepository(sourceFilePath);
            if (repository == null)
            {
                return null;
            }

            string repositoryRootPath = repository.Info.WorkingDirectory;

            if (!sourceFilePath.FullName.StartsWith(repositoryRootPath, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Assert(false);
                return null;
            }

            string repositoryRelativeFilePath = sourceFilePath.FullName.Substring(repositoryRootPath.Length);
            repositoryRelativeFilePath = repositoryRelativeFilePath.Replace('\\', '/');

            // Latest commit.
            var commit = repository.Head.Commits.First();

            var treeEntry = commit[repositoryRelativeFilePath];
            if (treeEntry == null)
            {
                Log.Info($"Untracked file {repositoryRelativeFilePath} will not be indexed.");
                return null;
            }

            return new GitSourceFileInfo(sourceFile, repository, repositoryRelativeFilePath, commit.Id.Sha);
        }

        private FileInfo CanonicalizeSourceFilePath(string sourceFile)
        {
            try
            {
                if (Path.IsPathRooted(sourceFile))
                {
                    return new FileInfo(sourceFile);
                }
            }
            catch (NotSupportedException)
            {
                return null;
            }

            Log.Warn("Relative source file paths are not supported yet.");
            return null;
        }

        private static FileInfo GetExactFileName(FileInfo file)
        {
            var exactPath = new StringBuilder();

            exactPath.Append(file.Directory.EnumerateFiles(file.Name).Single().Name);
            var directory = file.Directory;
            while (directory != null)
            {
                var parentDirectory = directory.Parent;
                if (parentDirectory == null)
                {
                    var drive = new DriveInfo(directory.Name);
                    exactPath.Insert(0, DriveInfo.GetDrives()
                        .Single(d => d.Name.Equals(drive.Name, StringComparison.OrdinalIgnoreCase))
                        .RootDirectory.FullName);
                }
                else
                {
                    exactPath.Insert(0, Path.DirectorySeparatorChar);
                    exactPath.Insert(0, parentDirectory.EnumerateDirectories(directory.Name).Single().Name);
                }
                directory = parentDirectory;
            }

            return new FileInfo(exactPath.ToString());
        }

        private string FindGitRepositoryRootPath(FileInfo sourceFile)
        {
            var directory = sourceFile.Directory;
            bool found = false;
            string gitPath;
            do
            {
                gitPath = Path.Combine(directory.FullName, ".git");
                found = Directory.Exists(gitPath) || File.Exists(gitPath);
                directory = directory.Parent;
            }
            while (directory != null && !found);

            return found ? gitPath : null;
        }

        private Repository FindGitRepository(FileInfo sourceFile)
        {
            string gitRepoRoot = FindGitRepositoryRootPath(sourceFile);
            if (gitRepoRoot == null)
            {
                return null;
            }

            if (!_repositories.TryGetValue(gitRepoRoot, out var repository))
            {
                repository = new Repository(gitRepoRoot);
                _repositories.Add(gitRepoRoot, repository);
            }

            return repository;
        }

        public override void Reset()
        {
            DisposeRepositories();
            base.Reset();
        }

        private void DisposeRepositories()
        {
            foreach (var kv in _repositories)
            {
                kv.Value.Dispose();
            }

            _repositories.Clear();
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeRepositories();
            }

            base.Dispose(disposing);
        }
    }
}
