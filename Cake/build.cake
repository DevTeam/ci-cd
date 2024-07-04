var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");
var solution = "../CI-CD.sln";

Task("Build").Does(() =>
{
    DotNetBuild(solution,
        new DotNetBuildSettings { Configuration = configuration, NoLogo = true });
});

Task("Test").IsDependentOn("Build").Does(() =>
{     
    DotNetTest(solution,
        new DotNetTestSettings { Configuration = configuration, NoLogo = true, NoBuild = true });
});

RunTarget(target);
