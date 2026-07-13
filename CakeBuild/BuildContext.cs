using Cake.Common;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Vintagestory.API.Common;

namespace CakeBuild;

public sealed class BuildContext : FrostingContext
{
	public const string ProjectName = "Ghaelen Tweaks";
	public string Version { get; }
	public string Name { get; }
	public bool SkipJsonValidation { get; }

	public BuildContext(ICakeContext context)
		: base(context)
	{
		SkipJsonValidation = context.Argument("skipJsonValidation", false);
		var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{ProjectName}/modinfo.json");
		Version = modInfo.Version;
		Name = modInfo.ModID;
	}
}