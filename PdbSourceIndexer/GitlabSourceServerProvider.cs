
namespace PdbSourceIndexer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using LibGit2Sharp;

    public class GitlabSourceFileInfo : SourceFileInfo
    {
        private readonly GitSourceFileInfo _innerInfo;

        public string ProjectId { get; }

        public string ProjectRelativeFileName => _innerInfo.RepositoryRelativeFileName;

        public string CommitId => _innerInfo.CommitId;

        public override IEnumerable<object> Fields
        {
            get
            {
                yield return SourceServerConvert.EscapeString(Uri.EscapeDataString(ProjectId));               // var2
                yield return SourceServerConvert.EscapeString(Uri.EscapeDataString(ProjectRelativeFileName)); // var3
                yield return CommitId;                                                                        // var4
            }
        }

        public GitlabSourceFileInfo(GitSourceFileInfo innerInfo, string projectId) : base(innerInfo.FileName)
        {
            this._innerInfo = innerInfo;
            this.ProjectId = projectId;
        }
    }

    public class GitlabHttpDownloader : IFilterVariables, IFilterSourceFileInfo
    {
        private readonly IFilterVariables _innerDownloader;

        public SourceFileInfo FilterSourceFileInfo(SourceFileInfo input)
            => (input is GitlabSourceFileInfo gitlab) ? new GitlabHttpSourceFileInfo(gitlab) : input;

        public IEnumerable<(string Name, string Value)> FilterVariables(IEnumerable<(string Name, string Value)> variables)
            => _innerDownloader.FilterVariables(variables);

        public (string Name, string Value) FilterVariable((string Name, string Value) variable)
        {
            var match = Regex.Match(variable.Name, "^var(\\d+)$");
            if (!match.Success)
            {
                return variable;
            }

            // We prepend relative file name to the list of the variables (see GitlabHttpSourceFileInfo.Fields getter),
            // so we have to shift all parent variables.
            int index = int.Parse(match.Groups[1].Value);
            return ($"var{index + 1}", variable.Value);
        }

        public GitlabHttpDownloader()
        {
            _innerDownloader = new WgetHttpDownloader("%targ%\\%var2%");
        }

        private class GitlabHttpSourceFileInfo : SourceFileInfo
        {
            private readonly GitlabSourceFileInfo _innerInfo;

            public GitlabHttpSourceFileInfo(GitlabSourceFileInfo innerInfo) : base(innerInfo.FileName)
            {
                this._innerInfo = innerInfo;
            }

            public override IEnumerable<object> Fields
                => _innerInfo.Fields.Prepend(_innerInfo.ProjectRelativeFileName.Replace('/', '\\')); // var2
        }
    }

    public class GitlabSourceServerProvider : SourceServerProvider
    {
        private readonly GitSourceServerProvider _gitProvider;

        private readonly Dictionary<string, string> _repositoryToProjectId;

        private readonly IFilterVariables _httpDownloader;

        [Option("--server-url", "GitLab server URL.")]
        public Uri ServerUrl { get; set; }

        [Option("--no-warn-relative-paths", "Suppress warnings for relative source file paths.")]
        public bool NoWarnRelativePaths
        {
            get => _gitProvider.NoWarnRelativePaths;
            set => _gitProvider.NoWarnRelativePaths = value;
        }

        public Uri ApiUrl
        {
            get
            {
                var builder = new UriBuilder(ServerUrl);
                if (!builder.Path.EndsWith("/"))
                {
                    builder.Path += "/";
                }
                builder.Path += "api/v4";
                return builder.Uri;
            }
        }

        public override string Name => "http";

        private IEnumerable<(string Name, string Value)> _variablesCore
        {
            get
            {
                yield return ("SRCSRVVERCTRL", Name);
                yield return ("SRCSRVCMD", String.Empty);
                yield return ("SRCSRVTRG", "%HTTP_EXTRACT_TARGET%");
                yield return ("GITLAB_API_V4_URL", ApiUrl.ToString());
                yield return ("GITLAB_SRCSRV_TOKEN", "<placeholder>");

                var var2 = _httpDownloader.FilterVariable(("var2", null));
                var var3 = _httpDownloader.FilterVariable(("var3", null));
                var var4 = _httpDownloader.FilterVariable(("var4", null));
                yield return ("HTTP_EXTRACT_TARGET", $"%GITLAB_API_V4_URL%/projects/%{var2.Name}%/repository/files/%{var3.Name}%/raw?ref=%{var4.Name}%&private_token=%GITLAB_SRCSRV_TOKEN%");
            }
        }

        public override IEnumerable<(string Name, string Value)> Variables => _httpDownloader.FilterVariables(_variablesCore);

        public GitlabSourceServerProvider()
        {
            _gitProvider = new GitSourceServerProvider();
            _repositoryToProjectId = new Dictionary<string, string>();
            _httpDownloader = new GitlabHttpDownloader();
        }

        public override SourceFileInfo GetFileInfo(string sourceFile)
        {
            SourceFileInfo gitlabSourceInfo = null;

            if (_gitProvider.GetFileInfo(sourceFile) is GitSourceFileInfo gitSourceInfo)
            {
                string projectId = FindGitlabProjectIdByRepository(gitSourceInfo.Repository);
                if (projectId != null)
                {
                    gitlabSourceInfo = new GitlabSourceFileInfo(gitSourceInfo, projectId);
                    if (_httpDownloader is IFilterSourceFileInfo filter)
                    {
                        gitlabSourceInfo = filter.FilterSourceFileInfo(gitlabSourceInfo);
                    }
                }
            }

            return gitlabSourceInfo;
        }

        private static (string Host, string Path) ParseGitUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return (uri.Host, uri.AbsolutePath);
            }

            var match = Regex.Match(url, "(.*@)?(.+):(.+)");
            if (match.Success)
            {
                return (match.Groups[2].Value, match.Groups[3].Value);
            }

            return default;
        }

        private string FindGitlabProjectIdByRepository(IRepository repository)
        {
            string repositoryPath = repository.Info.Path;

            if (_repositoryToProjectId.TryGetValue(repositoryPath, out string projectId))
            {
                return projectId;
            }

            bool alreadyWarned = false;

            var originUrlString = repository.Config.GetValueOrDefault<string>("remote.origin.url");
            if (originUrlString != null)
            {
                (var host, var path) = ParseGitUrl(originUrlString);
                if (host == null)
                {
                    Log.Warn($"Failed to parse origin URL for repository {repository.Info.WorkingDirectory}.");
                    alreadyWarned = true;
                }
                else if (host == ServerUrl.Host && path.EndsWith(".git"))
                {
                    projectId = path.Substring(0, path.Length - 4);
                    projectId = projectId.TrimStart('/');
                }
            }

            if (projectId == null && !alreadyWarned)
            {
                Log.Warn($"Could not find project ID for repository {repository.Info.WorkingDirectory}.");
            }

            _repositoryToProjectId.Add(repositoryPath, projectId);
            return projectId;
        }

        protected override void OnLoggerChanged()
        {
            base.OnLoggerChanged();
            _gitProvider.Log = this.Log;
        }
    }
}
