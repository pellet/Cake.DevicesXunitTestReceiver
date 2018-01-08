
//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "Cake.FileHelpers"

//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool GitVersion.CommandLine
#tool GitLink

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// should MSBuild & GitLink treat any errors as warnings.
var treatWarningsAsErrors = false;

// Parse release notes.
var releaseNotes = ParseReleaseNotes("RELEASENOTES.md");

// Get version.
var version = releaseNotes.Version.ToString();
var epoch = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
var gitSha = GitVersion().Sha;

var semVersion = string.Format("{0}.{1}", version, epoch);

// Define directories.
var artifactDirectory = "./artifacts/";

// Define global marcos.
Action Abort = () => { throw new Exception("a non-recoverable fatal error occurred."); };

Action<string> RestorePackages = (solution) =>
{
    NuGetRestore(solution);
};

Action<string> SourceLink = (solutionFileName) =>
{
    GitLink("./", new GitLinkSettings() {
        RepositoryUrl = "https://github.com/pellet/Cake.DevicesXunitTestReceiver",
        SolutionFileName = solutionFileName,
        ErrorsAsWarnings = treatWarningsAsErrors,
    });
};


///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup((context) =>
{
    Information("Building version {0} of Cake.DevicesXunitTestReceiver.", semVersion);
});

Teardown((context) =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("RestorePackages")
    .IsDependentOn("UpdateAssemblyInfo")
    .Does (() =>
{
    Action<string> build = (filename) =>
    {
        var solution = System.IO.Path.Combine("./src/", filename);

        // UWP (project.json) needs to be restored before it will build.
        RestorePackages(solution);

        Information("Building {0}", solution);

        MSBuild(solution, new MSBuildSettings()
            .SetConfiguration(configuration)
            .WithProperty("NoWarn", "1591") // ignore missing XML doc warnings
            .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false));

        //Information("Running GitLink for {0}", solution);
        //SourceLink(solution);
    };

    build("Cake.DevicesXunitTestReceiver.sln");
});

Task("UpdateAssemblyInfo")
    .Does (() =>
{
    var file = "./src/CommonAssemblyInfo.cs";

    CreateAssemblyInfo(file, new AssemblyInfoSettings {
        Product = "Cake.DevicesXunitTestReceiver",
        Version = version,
        FileVersion = version,
        InformationalVersion = semVersion,
        Copyright = "Copyright (c) Benjamin Pettit"
    });
});

Task("RestorePackages").Does (() =>
{
    RestorePackages("./src/Cake.DevicesXunitTestReceiver.sln");
});

Task("Default")
    .IsDependentOn("Build")
    .Does (() =>
{
    // switched to msbuild-based nuget creation
    // see here for parameters: https://docs.microsoft.com/en-us/nuget/schema/msbuild-targets
    MSBuild ("./src/Cake.DevicesXunitTestReceiver/Cake.DevicesXunitTestReceiver.csproj", c => {
		c.Configuration = configuration;
		c.Targets.Add ("pack");
		c.Properties.Add ("IncludeSymbols", new List<string> { "true" });
		c.Properties.Add ("PackageReleaseNotes", new List<string>(releaseNotes.Notes));
	});
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
