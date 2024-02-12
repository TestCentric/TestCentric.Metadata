// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.0-dev00082
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

//////////////////////////////////////////////////////////////////////
// INITIALIZE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

BuildSettings.Initialize(
	context: Context,
	title: "TestCentric Metadata",
	solutionFile: "TestCentric.Metadata.sln",
	githubRepository: "TestCentric.Metadata",
	// Most files here are from Mono.Cecil, so we don't change their header
	suppressHeaderCheck: true );

//////////////////////////////////////////////////////////////////////
// DEFINE PACKAGE
//////////////////////////////////////////////////////////////////////

BuildSettings.Packages.Add(new NuGetPackage(
	id: "TestCentric.Metadata",
	source: "src/TestCentric.Metadata/TestCentric.Metadata.csproj",
	checks: new PackageCheck[] {
		HasFiles(
			"LICENSE", "README.md", "NOTICE.md", "testcentric.png",
			"lib/net20/TestCentric.Metadata.dll",
			"lib/netstandard2.0/TestCentric.Metadata.dll") },
	symbols: new PackageCheck[] {
		HasFiles(
			"lib/net20/TestCentric.Metadata.pdb",
			"lib/netstandard2.0/TestCentric.Metadata.pdb") }));

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("AppVeyor")
	.Description("Targets to run on AppVeyor")
	.IsDependentOn("DumpSettings")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package")
	.IsDependentOn("Publish")
	.IsDependentOn("CreateDraftRelease")
	.IsDependentOn("CreateProductionRelease");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(CommandLineOptions.Target.Value);
