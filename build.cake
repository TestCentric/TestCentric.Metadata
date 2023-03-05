#tool nuget:?package=GitVersion.CommandLine&version=5.6.3
#tool nuget:?package=GitReleaseManager&version=0.12.1

#load ./versioning.cake

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//
// Arguments taking a value may use  `=` or space to separate the name
// from the value. Examples of each are shown here.
//
// --target=TARGET
// -t Target
//
//    The name of the task to be run, e.g. Test. Defaults to Build.
//
// --configuration=CONFIG
// -c CONFIG
//
//     The name of the configuration to build, test and/or package, e.g. Debug.
//     Defaults to Release.
//
// --packageVersion=VERSION
// --package=VERSION
//     Specifies the full package version, including any pre-release
//     suffix. This version is used directly instead of the default
//     version from the script or that calculated by GitVersion.
//     Note that all other versions (AssemblyVersion, etc.) are
//     derived from the package version.
//
//     NOTE: We can't use "version" since that's an argument to Cake itself.
//
//////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////
// PROJECT-SPECIFIC
//////////////////////////////////////////////////////////////////////

var SOLUTION_FILE = "TestCentric.Metadata.sln";
var NUGET_ID = "TestCentric.Metadata";

string Configuration = Argument("configuration", Argument("c", "Release"));

string PackageVersion;
string PackageName;
bool IsProductionRelease;
bool IsDevelopmentRelease;

//////////////////////////////////////////////////////////////////////
// SETUP
//////////////////////////////////////////////////////////////////////

Setup((context) =>
{
	var buildVersion = new BuildVersion(context);
	
	PackageVersion = buildVersion.PackageVersion;
	PackageName = NUGET_ID + "." + PackageVersion + ".nupkg";
	IsProductionRelease = !PackageVersion.Contains("-");
	IsDevelopmentRelease = PackageVersion.Contains("-dev");

	if (BuildSystem.IsRunningOnAppVeyor)
		AppVeyor.UpdateBuildVersion(PackageVersion + "-" + AppVeyor.Environment.Build.Number);

    Information($"Building {Configuration} version {PackageVersion} of TestCentric.Metadata.");
});

//////////////////////////////////////////////////////////////////////
// DEFINE CONSTANTS AND RUN SETTINGS
//////////////////////////////////////////////////////////////////////

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var BIN_DIR = PROJECT_DIR + "bin/" + Configuration + "/";
var TEST_DIR = BIN_DIR + "tests/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var NUGET_DIR = PROJECT_DIR + "nuget/";

// Testing
var NETFX_TEST_ASSEMBLY = "testcentric.engine.metadata.tests.exe";
var NETCORE_TEST_ASSEMBLY = "testcentric.engine.metadata.tests.dll";
var TEST_RUNTIMES = new [] {"net35", "net40", "net45", "netcoreapp2.1", "netcoreapp3.1", "net5.0", "net6.0", "net7.0"};

const string MYGET_PUSH_URL = "https://www.myget.org/F/testcentric/api/v2";
var MYGET_API_KEY = EnvironmentVariable("MYGET_API_KEY");

const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
var NUGET_API_KEY = EnvironmentVariable("NUGET_API_KEY");

const string GITHUB_OWNER = "TestCentric";
const string GITHUB_REPO = "TestCentric.Metadata";
readonly string GITHUB_ACCESS_TOKEN = EnvironmentVariable("GITHUB_ACCESS_TOKEN");

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
	{
		Information("Cleaning " + BIN_DIR);
		CleanDirectory(BIN_DIR);

        Information("Cleaning Package Directory");
        CleanDirectory(PACKAGE_DIR);
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
// TESTING
//////////////////////////////////////////////////////////////////////

Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
	{
		var errors = new List<string>();

		foreach (var runtime in TEST_RUNTIMES)
		{
			Console.WriteLine("\n----------------------------------------");
			Console.WriteLine("TESTING UNDER " + runtime.ToUpper());
			Console.WriteLine("----------------------------------------\r\n");

			bool isNetFX = runtime.StartsWith("net2") || runtime.StartsWith("net3") || runtime.StartsWith("net4");
			int rc = isNetFX
				? StartProcess($"{TEST_DIR}{runtime}/{NETFX_TEST_ASSEMBLY}")
				: StartProcess("dotnet", $"{TEST_DIR}{runtime}/{NETCORE_TEST_ASSEMBLY}");

			if (rc != 0)
				errors.Add($"Testing under {runtime}, rc={rc}");
		}

		if (errors.Count > 0)
			throw new Exception("\r\n" + string.Join("\r\n", errors));
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
	.Does(() =>
	{
		if (IsProductionRelease || IsDevelopmentRelease)
		{
			Information($"Publishing {PackageName} to MyGet");

			NuGetPush(PACKAGE_DIR + PackageName, new NuGetPushSettings()
			{
				ApiKey = MYGET_API_KEY,
				Source = MYGET_PUSH_URL
			});
		}
		else
			Information($"Skipping - {PackageName} is not a development or production release");
	});

//////////////////////////////////////////////////////////////////////
// CREATE A PRODUCTION RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateProductionRelease")
	.Does(() =>
	{
		if (IsProductionRelease)
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
		}
		else
			Information($"Skipping - {PackageName} is not a production release");
	});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("AppVeyor")
	.IsDependentOn("BuildTestAndPackage")
	.IsDependentOn("PublishToMyGet")
	.IsDependentOn("CreateProductionRelease");

Task("BuildTestAndPackage")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(Argument("target", Argument("t", "Default")));
