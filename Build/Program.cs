using System.Web;
using HostApi;

var configuration = Props.Get("configuration", "Release");

var buildResult = new DotNetBuild().WithConfiguration(configuration).WithNoLogo(true)
    .Build().EnsureSuccess();

var warnings = buildResult.Warnings
    .Where(warn => Path.GetFileName(warn.File) == "Calculator.cs")
    .Select(warn => $"{warn.Code}({warn.LineNumber}:{warn.ColumnNumber})")
    .Distinct();

foreach (var warning in warnings)
{
    await new HttpClient().GetAsync(
        "https://api.telegram.org/bot7102686717:AAEHw7HZinme_5kfIRV7TwXK4Xql9WPPpM3/" +
        "sendMessage?chat_id=878745093&text="
        + HttpUtility.UrlEncode(warning));
}

// Asynchronous way
var cts = new CancellationTokenSource();
/*await new DotNetTest()
    .WithConfiguration(configuration)
    .WithNoLogo(true).WithNoBuild(true)
    .BuildAsync(CancellationOnFirstFailedTest, cts.Token)
    .EnsureSuccess();*/

void CancellationOnFirstFailedTest(BuildMessage message)
{
    if (message.TestResult is { State: TestState.Failed }) cts.Cancel();
}

// Parallel tests
var tempDir = Directory.CreateTempSubdirectory();
try
{
    new DotNetPublish()
        .WithConfiguration(configuration).WithNoLogo(true).WithNoBuild(true)
        .WithFramework("net8.0").AddProps(("PublishDir", tempDir.FullName)).Build().EnsureSuccess();

    var test = new VSTest().WithTestFileNames("*.Tests.dll");

    var tasks = from tagSuffix in new[] {"bookworm-slim", "alpine", "noble"}
        let image = $"mcr.microsoft.com/dotnet/sdk:8.0-{tagSuffix}"
        let dockerRun = new DockerRun(image).WithCommandLine(test).WithAutoRemove(true)
            .WithVolumes((tempDir.FullName, "/app")).WithContainerWorkingDirectory("/app")
        select dockerRun.BuildAsync(CancellationOnFirstFailedTest, cts.Token);

    await Task.WhenAll(tasks).EnsureSuccess();
}
finally { tempDir.Delete(); }
