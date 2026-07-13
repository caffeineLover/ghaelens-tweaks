using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Frosting;

namespace CakeBuild;

[TaskName("Package")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
	public override void Run(BuildContext context)
	{
		context.DotNetBuild($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj", new DotNetBuildSettings
		{
			Configuration = "Release"
		});

		context.EnsureDirectoryExists("../Releases");
		context.CleanDirectory("../Releases");
		context.EnsureDirectoryExists($"../Releases/{context.Name}");

		if (context.DirectoryExists($"../{BuildContext.ProjectName}/assets"))
		{
			context.CopyDirectory($"../{BuildContext.ProjectName}/assets", $"../Releases/{context.Name}/assets");
		}

		context.CopyFile($"../{BuildContext.ProjectName}/modinfo.json", $"../Releases/{context.Name}/modinfo.json");
		if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png"))
		{
			context.CopyFile($"../{BuildContext.ProjectName}/modicon.png", $"../Releases/{context.Name}/modicon.png");
		}

		context.CopyFile(
			$"../{BuildContext.ProjectName}/bin/Release/Mods/{context.Name}/GhaelenTweaks.dll",
			$"../Releases/{context.Name}/GhaelenTweaks.dll");

		context.Zip($"../Releases/{context.Name}", $"../Releases/{context.Name}_{context.Version}.zip");
	}
}