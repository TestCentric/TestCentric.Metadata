//////////////////////////////////////////////////////////////////////
// PROJECT-SPECIFIC
//////////////////////////////////////////////////////////////////////

// When copying the script to support a different extension, the
// main changes needed should be in this section.

var SOLUTION_FILE = "TestCentric.Metadata.sln";
var GITHUB_SITE = "https://github.com/TestCentric/TestCentric.Metadata";
var NUGET_ID = "TestCentric.Metadata";
var DEFAULT_VERSION = "1.7.0";
var MAIN_BRANCH = "main";

//////////////////////////////////////////////////////////////////////
// ARGUMENTS  
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = DEFAULT_VERSION + dbgSuffix;

if (BuildSystem.IsRunningOnAppVeyor)
{
	var tag = AppVeyor.Environment.Repository.Tag;

	if (tag.IsTag)
	{
		packageVersion = tag.Name;
	}
	else
	{
		var buildNumber = AppVeyor.Environment.Build.Number.ToString("00000");
		var branch = AppVeyor.Environment.Repository.Branch;
		var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

		if (branch == MAIN_BRANCH && !isPullRequest)
		{
			packageVersion = DEFAULT_VERSION + "-dev-" + buildNumber + dbgSuffix;
		}
		else
		{
			var suffix = "-ci-" + buildNumber + dbgSuffix;

			if (isPullRequest)
				suffix += "-pr-" + AppVeyor.Environment.PullRequest.Number;
			else
				suffix += "-" + branch;

			// Nuget limits "special version part" to 20 chars. Add one for the hyphen.
			if (suffix.Length > 21)
				suffix = suffix.Substring(0, 21);

			suffix = suffix.Replace(".", "");

			packageVersion = DEFAULT_VERSION + suffix;
		}
	}

	AppVeyor.UpdateBuildVersion(packageVersion);
}

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
// RESTORE NUGET PACKAGES
//////////////////////////////////////////////////////////////////////

Task("RestorePackages")
	.Does(() =>
	{
		NuGetRestore(SOLUTION_FILE);
	});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("RestorePackages")
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
            Version = packageVersion,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

//////////////////////////////////////////////////////////////////////
// PUBLISH PACKAGES
//////////////////////////////////////////////////////////////////////

Task("Publish")
	.IsDependentOn("Package")
	.Does(() =>
	{
		const string MYGET_PUSH_URL = "https://www.myget.org/F/testcentric/api/v2";
		const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";

		var package = PACKAGE_DIR + NUGET_ID + "." + packageVersion + ".nupkg";
		var mygetApiKey = EnvironmentVariable("MYGET_API_KEY");

		if (packageVersion.Contains("-dev-"))
			NuGetPush(package, new NuGetPushSettings()
			{ 
				ApiKey = mygetApiKey,
				Source = MYGET_PUSH_URL
			});
		else
			Information("Nothing to publish from this run.");
	});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("AppVeyor")
	.IsDependentOn("Build")
	.IsDependentOn("Package")
	.IsDependentOn("Publish");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(Argument("target", "Default"));
