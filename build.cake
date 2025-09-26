#addin nuget:?package=Cake.Docker&version=1.1.0
#addin nuget:?package=YamlDotNet&version=13.1.1

//////////////////////////////////////////////////////////////////////
// Command-line parameters
//////////////////////////////////////////////////////////////////////
var target = Argument("Target", "Default");

//////////////////////////////////////////////////////////////////////
// Paths & images
//////////////////////////////////////////////////////////////////////
var sln            = File("SeeYouThereApi.sln");
var openApiFile    = File("spec/openapi.yaml");
var dockerRegistry = EnvironmentVariable("DOCKER_REGISTRY") ?? "local";
var dockerImage    = "seeyouthereapi";
var generatorImage = "openapitools/openapi-generator-cli:latest";
var validatorImage = "wework/speccy:latest";

//////////////////////////////////////////////////////////////////////
// Helper: extract info.version from openapi.yaml
//////////////////////////////////////////////////////////////////////
string GetApiVersion()
{
    var yaml  = System.IO.File.ReadAllText(openApiFile);
    var regex = new System.Text.RegularExpressions.Regex(@"version:\s*([0-9]+\.[0-9]+\.[0-9]+)");
    var m     = regex.Match(yaml);
    if (!m.Success) throw new Exception("Version not found in openapi.yaml");
    return m.Groups[1].Value;
}

//////////////////////////////////////////////////////////////////////
// Tasks
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./gen/**");
    CleanDirectories("./src/**/bin");
    CleanDirectories("./src/**/obj");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => DotNetRestore(sln));

Task("Validate")
    .Does(() =>
{
    var dir = MakeAbsolute(Directory("./"));
    DockerRun(
        new DockerContainerRunSettings { Rm = true, Volume = new[] { $"{dir}:/data" } },
        validatorImage,
        $"lint /data/{openApiFile}"
    );
});

Task("GenerateServer")
    .IsDependentOn("Validate")
    .Does(() =>
{
    var dir = MakeAbsolute(Directory("./"));
    CleanDirectories("./gen/SeeYouThere.Api.Specification/**");

    DockerRun(
        new DockerContainerRunSettings { Rm = true, Volume = new[] { $"{dir}:/data" } },
        generatorImage,
        "generate " +
        $"-i /data/{openApiFile} " +
        "-g aspnetcore " +
        "-o /data/gen/SeeYouThere.Api.Specification " +
        "--additional-properties=" +
        "aspnetCoreVersion=6.0," +
        "buildTarget=library," +
        "classModifier=abstract," +
        "useSwashbuckle=false"
    );
});

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("GenerateServer")
    .Does(() =>
{
    DotNetBuild(sln, new DotNetBuildSettings { Configuration = "Release" });
});

Task("Docker")
    .IsDependentOn("Build")
    .Does(() =>
{
    var version  = GetApiVersion();
    var imageTag = $"{dockerRegistry}/{dockerImage}:{version}";
    var latest   = $"{dockerRegistry}/{dockerImage}:latest";

    Information($"Building Docker image {imageTag}");

    DockerBuild(new DockerImageBuildSettings
    {
        Tag = new[] { imageTag, latest }
    }, ".");
});

Task("Default").IsDependentOn("Build");

RunTarget(target);
