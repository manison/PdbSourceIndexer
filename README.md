# PDB Source Indexer

Extensible and easy to use PDB source indexer written in C#.

## Highlights

* Easy to integrate with your CI workflow.
* Easily extensible to fit your needs.
* Handles projects with Git submodules.

## Usage

```
> PdbSourceIndexer <global options> <provider> <provider options>
```

### Global options

|option|description|
|------|-----------|
|`--tools-path <tools-path>`|Debugging Tools for Windows installation path. Specifies location of _pdbstr.exe_ and _srctool.exe_ utils. These are part of the Windows SDK. Usually will be something like _C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\srcsrv_.|
|`--symbol-root <symbol-root>`|Specifies directory where the PDBs to index are located.|
|`--recursive`|Search symbol files recursively.|

### Provider

Specifies the source server provider to use. The following providers are currently available:

|provider|source server|
|--------|-------------|
|`gitlab`|[GitLab](https://www.gitlab.com)|

#### GitLab provider

|option|description|
|------|-----------|
|`--server-url <server-url>`|GitLab server URL (e.g. _https://mygitlab.example.com_).|

When using the GitLab provider the git repository origin url must point to GitLab server URL (i.e. either _git@mygitlab.example.com:/project.git_ or _https://mygitlab.example.com/project.git_). This is ok when indexing PDBs under GitLab-CI.
Also when using the indexed PDB under debugger, the `GITLAB_SRCSRV_TOKEN` variable in user's _srvsrv.ini_ must be set to repository read access PAT (see below). Also due to the inability of source server to download files from URLs with the query part you will have to install the _wget_ utility somewhere on the _%PATH%_ or point the _srcsrv.ini_ to it.

**Example**

```
PdbSourceIndexer --tools-path "C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\srcsrv" --symbol-root D:\MyProject\bin\Debug gitlab --server-url %CI_SERVER_URL%
```

## Debugger Setup

Some source server providers require additional setup for the debugger. E.g. the GitLab source server provider requires the user's personal access token to authenticate and download the specific source file from the repository.

### WinDbg

The source server is controlled through the _srcsrv.ini_ file. Its location is specified by the `SRCSRV_INI_FILE` environment variable. So set the variable to e.g. _C:\SrcSrv\srcsrv.ini_, create that INI file in that location and set its content to (for GitLab source provider)

```ini
[variables]
GITLAB_SRCSRV_TOKEN=AaBbCcDdEeFf12345678
```

where the value is personal access token with at least the _read_repository_ scope. The token can be created in user's GitLab settings page.

In this case you may want to save the file into your profile directory so other users can't access your PAT.

If the source server provider uses external program to extract the source file from the version control system you may want to specify the path to the utility in the _srcsrv.ini_. E.g. the GitLab provider uses _wget_ to download the source file, you tell source server where to find _wget_ as follows:

```ini
[trusted commands]
wget=C:\wget\wget.exe
```

### Visual Studio

Besides authoring the _srcsrv.ini_ file as above you also have to enable source server support in Visual Studio. Check the _Tools > Options > Debugging > Enable source server support_ checkbox.
