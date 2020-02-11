
namespace PdbSourceIndexer
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using LibGit2Sharp;

    public class GitlabSourceFileInfo : SourceFileInfo
    {
        private GitSourceFileInfo _innerInfo;

        public string ProjectId { get; }

        public string ProjectRelativeFileName => _innerInfo.RepositoryRelativeFileName;

        public string CommitId => _innerInfo.CommitId;

        public override IEnumerable<object> Fields
        {
            get
            {
                yield return Uri.EscapeDataString(ProjectId);                   // var2
                yield return Uri.EscapeDataString(ProjectRelativeFileName);     // var3
                yield return CommitId;                                          // var4
            }
        }

        public GitlabSourceFileInfo(GitSourceFileInfo innerInfo, string projectId) : base(innerInfo.FileName)
        {
            this._innerInfo = innerInfo;
            this.ProjectId = projectId;
        }
    }

    public class GitlabSourceServerProvider : SourceServerProvider
    {
        private readonly GitSourceServerProvider _gitProvider;

        private readonly Dictionary<string, string> _repositoryToProjectId;

        [Option("--server-url", "GitLab server URL.")]
        public Uri ServerUrl { get; set; }

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

        public override IEnumerable<(string Name, string Value)> Variables
        {
            get
            {
                yield return ("SRCSRVVERCTRL", Name);
                yield return ("SRCSRVCMD", String.Empty);
                yield return ("SRCSRVTRG", "%HTTP_EXTRACT_TARGET%");
                yield return ("GITLAB_API_V4_URL", ApiUrl.ToString());
                yield return ("GITLAB_SRCSRV_TOKEN", "<placeholder>");
                yield return ("HTTP_EXTRACT_TARGET", "%GITLAB_API_V4_URL%/projects/%var2%/repository/files/%var3%/raw?ref=%var4%&private_token=%GITLAB_SRCSRV_TOKEN%");
            }
        }

        public GitlabSourceServerProvider()
        {
            _gitProvider = new GitSourceServerProvider();
            _repositoryToProjectId = new Dictionary<string, string>();
        }

        public override SourceFileInfo GetFileInfo(string sourceFile)
        {
            SourceFileInfo gitlabSourceInfo = null;

            var gitSourceInfo = _gitProvider.GetFileInfo(sourceFile) as GitSourceFileInfo;
            if (gitSourceInfo != null)
            {
                string projectId = FindGitlabProjectIdByRepository(gitSourceInfo.Repository);
                if (projectId != null)
                {
                    gitlabSourceInfo = new GitlabSourceFileInfo(gitSourceInfo, projectId);
                }
            }

            return gitlabSourceInfo;
        }

        private static (string Host, string Path) ParseGitUrl(string url)
        {
            var match = Regex.Match(url, "(.*@)?(.+):(.+)");
            if (match.Success)
            {
                return (match.Groups[2].Value, match.Groups[3].Value);
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return (uri.Host, uri.AbsolutePath);
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
                }
            }

            if (projectId == null && !alreadyWarned)
            {
                Log.Warn($"Could not find project ID for repository {repository.Info.WorkingDirectory}.");
            }

            _repositoryToProjectId.Add(repositoryPath, projectId);
            return projectId;
        }
    }
}
