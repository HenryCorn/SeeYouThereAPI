#addin nuget:?package=Cake.Docker&version=1.0.0
#addin nuget:?package=YamlDotNet&version=13.1.1   // for parsing YAML

//////////////////////////////////////////////////////////////////////
// Command line parameters
//////////////////////////////////////////////////////////////////////
var target = Argument("Target", "Default");

//////////////////////////////////////////////////////////////////////
// Declarations
//////////////////////////////////////////////////////////////////////
var sln = File("SeeYouThereAPI.sln");
var openApiFile = File("spec/openapi.yaml");
var dockerRegistry = EnvironmentVariable("DOCKER_REGISTRY") ?? "local";
var dockerImageName = "seeyouthere/api";
var generatorImage = "openapitools/openapi-generator-cli:latest";
var validatorImage = "wework/speccy:latest";

//////////////////////////////////////////////////////////////////////
// Helper to read info.version from openapi.yaml
//////////////////////////////////////////////////////////////////////
string GetApiVersion()
{
    var yaml = System.IO.File.ReadAllText(openApiFile);
    var regex = new System.Text.RegularExpressions.Regex(@"version:\s*([0-9]+\.[0-9]+\.[0-9]+)");
    var match = regex.Match(yaml);
    if (!match.Success)
    {
        throw new Exception("Could not find version in openapi.yaml");
    }
    return match.Groups[1].Value;
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
    .Does(() => DotNetRestore(sln));

Task("Validate")
    .Does(() => {
        var dir = MakeAbsolute(Directory("./"));
        DockerRun(new DockerContainerRunSettings { Rm = true, Volume = new [] { $"{dir}:/data" } },
            validatorImage, $"lint /data/{openApiFile}");
    });

Task("GenerateServer")
    .IsDependentOn("Validate")
    .Does(() => {
        var dir = MakeAbsolute(Directory("./"));
        DockerRun(new DockerContainerRunSettings { Rm = true, Volume = new [] { $"{dir}:/data" } },
            generatorImage,
            $"generate -i /data/{openApiFile} -g aspnetcore -o /data/gen/SeeYouThere.Api.Specification " +
            "--additional-properties=aspnetCoreVersion=6.0,classModifier=abstract,useSwashbuckle=false");
    });

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("GenerateServer")
    .Does(() => {
        DotNetBuild(sln, new DotNetBuildSettings { Configuration = "Release" });
    });
    
Task("Docker")
    .IsDependentOn("Build")
    .Does(() =>
{
    var version = "latest"; // or parse from openapi.yaml if you like
    var imageTag = $"seeyouthereapi:{version}";
    Information($"Building Docker image {imageTag}");

    var settings = new DockerImageBuildSettings
    {
        Tag = new[] { imageTag }
    };

    DockerBuild(settings, ".");
});


Task("Default").IsDependentOn("Build");

RunTarget(target);
