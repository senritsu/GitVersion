using System.Linq;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(PackageNuget))]
    [TaskDescription("Creates the nuget packages")]
    [IsDependentOn(typeof(PackagePrepare))]
    public class PackageNuget : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists(Paths.Nuget);

            foreach (var package in context.Packages!.Nuget)
            {
                if (context.FileExists(package.NuspecPath))
                {
                    var artifactPath = context.MakeAbsolute(context.PackagesBuildMap[package.Id]).FullPath;
                    var version = context.Version;
                    var gitVersion = version?.GitVersion;
                    var nugetSettings = new NuGetPackSettings
                    {
                        // KeepTemporaryNuSpecFile = true,
                        Version = version?.NugetVersion,
                        NoPackageAnalysis = true,
                        OutputDirectory = Paths.Nuget,
                        Repository = new NuGetRepository
                        {
                            Branch = gitVersion?.BranchName,
                            Commit = gitVersion?.Sha
                        },
                        Files = context.GetFiles(artifactPath + "/**/*.*")
                            .Select(file => new NuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
                            .Concat(context.GetFiles("docs/**/package_icon.png").Select(file => new NuSpecContent { Source = file.FullPath, Target = "package_icon.png" }))
                            .ToArray()
                    };

                    context.NuGetPack(package.NuspecPath, nugetSettings);
                }
            }

            var settings = new DotNetCorePackSettings
            {
                Configuration = context.MsBuildConfiguration,
                OutputDirectory = Paths.Nuget,
                MSBuildSettings = context.MsBuildSettings,
                ArgumentCustomization = arg => arg.Append("/p:PackAsTool=true")
            };

            // GitVersion.MsBuild, global tool & core
            context.DotNetCorePack("./src/GitVersion.App/GitVersion.App.csproj", settings);

            settings.ArgumentCustomization = arg => arg.Append("/p:IsPackaging=true");
            context.DotNetCorePack("./src/GitVersion.MsBuild", settings);

            settings.ArgumentCustomization = null;
            context.DotNetCorePack("./src/GitVersion.Core", settings);
        }
    }
}