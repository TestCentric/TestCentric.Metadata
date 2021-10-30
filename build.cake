#tool nuget:?package=GitReleaseManager&version=0.11.0

//////////////////////////////////////////////////////////////////////
// PROJECT-SPECIFIC
//////////////////////////////////////////////////////////////////////

var SOLUTION_FILE = "TestCentric.Metadata.sln";
var GITHUB_SITE = "https://github.com/TestCentric/TestCentric.Metadata";
var NUGET_ID = "TestCentric.Metadata";
var DEFAULT_VERSION = "1.7.1";
var MAIN_BRANCH = "main";

//////////////////////////////////////////////////////////////////////
// ARGUMENTS  
//////////////////////////////////////////////////////////////////////

var Configuration = Argument("configuration", "Release");
var PackageVersion = Argument("packageVersion", DEFAULT_VERSION);

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var dash = PackageVersion.IndexOf('-');
var BaseVersion = dash > 0 
	? PackageVersion.Substring(0, dash)
	: PackageVersion;

// Any prerelease label in packageVersion is ignored on AppVeyor
// where the baseVersion is used to calculate a new packageVersion.
if (BuildSystem.IsRunningOnAppVeyor)
{
	var tag = AppVeyor.Environment.Repository.Tag;

	if (tag.IsTag)
	{
		// A tag is used directly as the package Version.
		// It may include a prerelease lable like beta1.
		PackageVersion = tag.Name;
	}
	else
	{
		var buildNumber = AppVeyor.Environment.Build.Number.ToString("00000");
		var branch = AppVeyor.Environment.Repository.Branch;
		var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

		if (branch == MAIN_BRANCH && !isPullRequest)
		{
			// When merging to main or pushing directly, use dev label
			PackageVersion = BaseVersion + "-dev-" + buildNumber;
		}
		else
		{
			var suffix = "-ci-" + buildNumber;

			if (isPullRequest)
				suffix += "-pr-" + AppVeyor.Environment.PullRequest.Number;
			else
				suffix += "-" + branch;

			// Nuget limits "special version part" to 20 chars. Add one for the hyphen.
			if (suffix.Length > 21)
				suffix = suffix.Substring(0, 21);

			suffix = suffix.Replace(".", "");

			PackageVersion = BaseVersion + suffix;
		}
	}

	AppVeyor.UpdateBuildVersion(PackageVersion);
}

//////////////////////////////////////////////////////////////////////
// DEFINE CONSTANTS AND RUN SETTINGS
//////////////////////////////////////////////////////////////////////

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var BIN_DIR = PROJECT_DIR + "bin/" + Configuration + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var NUGET_DIR = PROJECT_DIR + "nuget/";

// Publishing
var PackageName = NUGET_ID + "." + PackageVersion + ".nupkg";
bool IsProductionRelease = !PackageVersion.Contains("-");
bool IsDevelopmentRelease = PackageVersion.Contains("-dev-");

const string MYGET_PUSH_URL = "https://www.myget.org/F/testcentric/api/v2";
var MYGET_API_KEY = EnvironmentVariable("MYGET_API_KEY");

const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
var NUGET_API_KEY = EnvironmentVariable("NUGET_API_KEY");

const string GITHUB_OWNER = "TestCentric";
const string GITHUB_REPO = "TestCentric.Metadata";
var GITHUB_ACCESS_TOKEN = EnvironmentVariable("GITHUB_ACCESS_TOKEN");

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
		MSBuild(SOLUTION_FILE, new MSBuildSettings()
			.SetConfiguration(Configuration)
			.SetMSBuildPlatform(MSBuildPlatform.Automatic)
			.SetVerbosity(Verbosity.Minimal)
			.SetNodeReuse(false)
			.SetPlatformTarget(PlatformTarget.MSIL)
			.WithProperty("Version", PackageVersion)
		);
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
            Version = PackageVersion,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

//////////////////////////////////////////////////////////////////////
// PUBLISH PACKAGES
//////////////////////////////////////////////////////////////////////

Task("PublishToMyGet")
	.WithCriteria(IsProductionRelease || IsDevelopmentRelease)
	.IsDependentOn("Package")
	.Does(() =>
	{
		Information($"Publishing {PackageName} to MyGet");

		NuGetPush(PACKAGE_DIR + PackageName, new NuGetPushSettings()
		{
			ApiKey = MYGET_API_KEY,
			Source = MYGET_PUSH_URL
		});
	});

//////////////////////////////////////////////////////////////////////
// CREATE A PRODUCTION RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateProductionRelease")
	.WithCriteria(IsProductionRelease)
	.Does(() =>
	{
		Information($"Publishing {PackageName} to NuGet");

		NuGetPush(PACKAGE_DIR + PackageName, new NuGetPushSettings()
		{
			ApiKey = NUGET_API_KEY,
			Source = NUGET_PUSH_URL
		});

		Information($"Publishing release {PackageVersion} on GitHub");

		GitReleaseManagerCreate(GITHUB_ACCESS_TOKEN, GITHUB_OWNER, GITHUB_REPO,
			new GitReleaseManagerCreateSettings()
			{
				Name = $"TestCentric.Metadata {PackageVersion}",
				Milestone = PackageVersion
			});

		GitReleaseManagerAddAssets(GITHUB_ACCESS_TOKEN, GITHUB_OWNER, GITHUB_REPO,
			PackageVersion, PACKAGE_DIR + PackageName);
		GitReleaseManagerClose(GITHUB_ACCESS_TOKEN, GITHUB_OWNER, GITHUB_REPO, PackageVersion);
	});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("AppVeyor")
	.IsDependentOn("Build")
	.IsDependentOn("Package")
	.IsDependentOn("PublishToMyGet")
	.IsDependentOn("CreateProductionRelease");

Task("Full")
	.IsDependentOn("Build")
	.IsDependentOn("Package");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(Argument("target", "Default"));
