var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var solution = "./SeeYouThereAPI.sln";

Task("Restore")
    .Does(() => DotNetRestore(solution));

Task("Build")
    .IsDependentOn("Restore")
    .Does(() => DotNetBuild(solution,
        new DotNetBuildSettings { Configuration = configuration }));

Task("Test")
    .IsDependentOn("Build")
    .Does(() => DotNetTest("./tests/SeeYouThereAPI.Tests/SeeYouThereAPI.Tests.csproj"));

Task("Publish")
    .IsDependentOn("Test")
    .Does(() => DotNetPublish("./src/SeeYouThereAPI.Api/SeeYouThereAPI.Api.csproj",
        new DotNetPublishSettings { Configuration = configuration,
                                    OutputDirectory = "./publish" }));

Task("Default").IsDependentOn("Publish");
RunTarget(target);