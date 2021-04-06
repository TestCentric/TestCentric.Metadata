//////////////////////////////////////////////////////////////////////
// PROJECT-SPECIFIC
//////////////////////////////////////////////////////////////////////

// When copying the script to support a different extension, the
// main changes needed should be in this section.

var SOLUTION_FILE = "TestCentric.Metadata.sln";
var GITHUB_SITE = "https://github.com/TestCentric/TestCentric.Metadata";
var NUGET_ID = "TestCentric.Metadata";
var VERSION = "1.7.0";

//////////////////////////////////////////////////////////////////////
// ARGUMENTS  
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var NUGET_DIR = PROJECT_DIR + "nuget/";

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
	{
		CleanDirectory(BIN_DIR);
	});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
	.IsDependentOn("Clean")
    .Does(() =>
	{
		if (IsRunningOnWindows())
		{
			MSBuild(SOLUTION_FILE, new MSBuildSettings()
				.SetConfiguration(configuration)
				.SetMSBuildPlatform(MSBuildPlatform.Automatic)
				.SetVerbosity(Verbosity.Minimal)
				.SetNodeReuse(false)
				.SetPlatformTarget(PlatformTarget.MSIL)
			);
		}
		else
		{
			XBuild(SOLUTION_FILE, new XBuildSettings()
				.WithTarget("Build")
				.WithProperty("Configuration", configuration)
				.SetVerbosity(Verbosity.Minimal)
			);
		}
	});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("Package")
	.IsDependentOn("Build")
	.Does(() =>
	{
        NuGetPack(NUGET_DIR + NUGET_ID + ".nuspec", new NuGetPackSettings()
        {
            Version = VERSION,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("AppVeyor")
	.IsDependentOn("Build")
	.IsDependentOn("Package");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(Argument("target", "Default"));
