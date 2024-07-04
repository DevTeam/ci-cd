using Cake.Common;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core;
using Cake.Frosting;

new CakeHost().UseContext<BuildContext>().Run(args);

public class BuildContext(ICakeContext context)
    : FrostingContext(context)
{
    public const string Solution = "../CI-CD.sln";
    public string BuildConfiguration { get; } = context.Argument("configuration", "Release");
}

[TaskName("Build")]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetBuild(BuildContext.Solution,
        new DotNetBuildSettings { Configuration = context.BuildConfiguration, NoLogo = true });
}

[TaskName("Test"), IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetTest(BuildContext.Solution,
        new DotNetTestSettings { Configuration = context.BuildConfiguration, NoLogo = true, NoBuild = true });
}
