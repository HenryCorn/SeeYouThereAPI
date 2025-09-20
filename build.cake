//////////////////////////////////////////////////////
// Arguments
//////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var solution = "./SeeYouThereAPI.sln";
var publishDir = "./publish";

//////////////////////////////////////////////////////
// Tasks
//////////////////////////////////////////////////////
Task("Restore").Does(() =>
{
    DotNetRestore(solution);
});

Task("Build").IsDependentOn("Restore").Does(() =>
{
    DotNetBuild(solution, new DotNetBuildSettings { Configuration = configuration });
});

Task("Test").IsDependentOn("Build").Does(() =>
{
    DotNetTest("./tests/SeeYouThereAPI.Tests/SeeYouThereAPI.Tests.csproj");
});

Task("Publish").IsDependentOn("Test").Does(() =>
{
    DotNetPublish("./src/SeeYouThereAPI.Api/SeeYouThereAPI.Api.csproj",
        new DotNetPublishSettings { Configuration = configuration, OutputDirectory = publishDir });
});

Task("DockerBuild").IsDependentOn("Publish").Does(() =>
{
    StartProcess("docker", $"build -t seeyouthereapi .");
});

Task("Default").IsDependentOn("Publish");
RunTarget(target);