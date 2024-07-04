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
    await new HttpClient().GetAsync(
        "https://api.telegram.org/bot7102686717:AAEHw7HZinme_5kfIRV7TwXK4Xql9WPPpM3/"
        + "sendMessage?chat_id=878745093&text="
        + HttpUtility.UrlEncode(warning));

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
var results = await Task.WhenAll(
    RunTestsAsync("7.0", "bookworm-slim", "alpine"),
    RunTestsAsync("8.0", "bookworm-slim", "alpine", "noble"));
results.SelectMany(i => i).EnsureSuccess();

async Task<IEnumerable<IBuildResult>> RunTestsAsync(string framework, params string[] platforms)
{
    var publish = new DotNetPublish().WithWorkingDirectory("MySampleLib.Tests")
        .WithFramework($"net{framework}").WithConfiguration(configuration).WithNoBuild(true);
    await publish.BuildAsync(cancellationToken: cts.Token).EnsureSuccess();
    var publishPath = Path.Combine(publish.WorkingDirectory, "bin", configuration, $"net{framework}", "publish");

    var test = new VSTest().WithTestFileNames("*.Tests.dll");
    var testInDocker = new DockerRun().WithCommandLine(test).WithAutoRemove(true).WithQuiet(true)
        .WithVolumes((Path.GetFullPath(publishPath), "/app")).WithContainerWorkingDirectory("/app");
    var tasks = from platform in platforms
                let image = $"mcr.microsoft.com/dotnet/sdk:{framework}-{platform}"
                select testInDocker.WithImage(image).BuildAsync(CancellationOnFirstFailedTest, cts.Token);
    return await Task.WhenAll(tasks);
}
