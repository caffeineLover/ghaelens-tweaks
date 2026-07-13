/*
	Build pipeline for Ghaelen Tweaks. It reads release metadata from modinfo.json, validates the JSON assets that
	Vintage Story will load, builds the code mod, and creates the versioned ModDB-ready zip.
*/

using Cake.Frosting;

namespace CakeBuild;



public static class Program
{
	public static int Main(string[] args)
	{
		return new CakeHost()
			.UseContext<BuildContext>()
			.Run(args);
	}
}



// Cake constructs this context by reflection; there is intentionally no direct new BuildContext(...) call.
// ReSharper disable once ClassNeverInstantiated.Global