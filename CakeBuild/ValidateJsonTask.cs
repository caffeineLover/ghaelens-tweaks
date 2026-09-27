using System;
using System.IO;
using Cake.Common.IO;
using Cake.Frosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CakeBuild;

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
	public override void Run(BuildContext context)
	{
		if (context.SkipJsonValidation)
		{
			return;
		}

		var modOutputDirectory = $"../{context.ProjectName}/bin";
		var jsonFiles = context.GetFiles($"../{context.ProjectName}/assets/**/*.json");
		foreach (var file in jsonFiles)
		{
			if (file.FullPath.StartsWith(context.MakeAbsolute(context.Directory(modOutputDirectory)).FullPath))
			{
				continue;
			}

			try
			{
				var json = File.ReadAllText(file.FullPath);
				JToken.Parse(json);
			}
			catch (JsonException ex)
			{
				throw new Exception(
					$"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
			}
		}
	}
}
