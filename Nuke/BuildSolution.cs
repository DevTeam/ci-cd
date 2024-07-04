using System.ComponentModel;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug = new() { Value = nameof(Debug) };
    public static Configuration Release = new() { Value = nameof(Release) };
    public static implicit operator string(Configuration configuration) => configuration.Value;
}

class BuildSolution : NukeBuild
{
    public static int Main () => Execute<BuildSolution>(x => x.Test);

    [Parameter("Configuration to build")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    Target Build => _ => _
        .Executes(() => DotNetTasks.DotNetBuild(
            new DotNetBuildSettings().SetConfiguration(Configuration).SetNoLogo(true)));

    Target Test => _ => _ .DependsOn(Build)
        .Executes(() => DotNetTasks.DotNetTest(
            new DotNetTestSettings().SetConfiguration(Configuration).SetNoLogo(true).SetNoBuild(true)));
}
