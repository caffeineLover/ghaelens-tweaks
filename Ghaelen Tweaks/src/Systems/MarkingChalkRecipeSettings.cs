/*
 * Applies marking chalk recipe quantities from the mod configuration.
 *
 * The JSON recipes use the default dye batch size so they work without code.
 * This helper updates the resolved grid and barrel recipes at runtime when a
 * world loads or Config Lib changes the batch size. Barrel recipe types live
 * in the survival assembly, so the updater uses the public recipe registry
 * plus reflection and keeps the project free of an extra compile-time
 * reference.
 */

using System.Collections;
using System.Reflection;
using Vintagestory.API.Common;

namespace GhaelenTweaks;

internal static class MarkingChalkRecipeSettings
{
	private const string RecipePrefix = "marking-chalk-dye-";
	private const string PlainChalkCode = "marking-chalk-white";



	//// Applies the active configured dye batch size to all marking chalk dye
	//// recipes that have already been loaded and resolved.
	////
	internal static void Apply(ICoreAPI api)
	{
		int batchSize = GhaelenTweaksConfig.Current.MarkingChalkDyeBatchSize;

		ApplyGridRecipes(api, batchSize);
		ApplyBarrelRecipes(api, batchSize);
	}



	//// Updates bowl dyeing grid recipes.
	////
	private static void ApplyGridRecipes(ICoreAPI api, int batchSize)
	{
		foreach (GridRecipe recipe in api.World.GridRecipes)
		{
			if (!IsMarkingChalkDyeRecipe(recipe.Name?.Path))
			{
				continue;
			}

			if (recipe.Output == null)
			{
				continue;
			}

			recipe.Output.Quantity = batchSize;
			if (recipe.Output.ResolvedItemStack != null)
			{
				recipe.Output.ResolvedItemStack.StackSize = batchSize;
			}

			if (recipe.ResolvedIngredients == null)
			{
				continue;
			}

			foreach (CraftingRecipeIngredient? ingredient in recipe.ResolvedIngredients)
			{
				UpdatePlainChalkIngredient(ingredient, batchSize);
			}
		}
	}



	//// Updates barrel dyeing recipes through the generic recipe registry.
	////
	private static void ApplyBarrelRecipes(ICoreAPI api, int batchSize)
	{
		RecipeRegistryBase? registry = api.World.GetRecipeRegistry("barrelrecipes");
		if (registry == null)
		{
			return;
		}

		FieldInfo? recipesField = registry.GetType().GetField("Recipes");
		if (recipesField?.GetValue(registry) is not IEnumerable recipes)
		{
			return;
		}

		foreach (object recipe in recipes)
		{
			if (!IsMarkingChalkDyeRecipe(GetStringProperty(recipe, "Code")))
			{
				continue;
			}

			UpdateBarrelRecipe(recipe, batchSize);
		}
	}



	//// Updates one reflected barrel recipe's output and plain chalk
	//// ingredient quantities.
	////
	private static void UpdateBarrelRecipe(object recipe, int batchSize)
	{
		Type recipeType = recipe.GetType();
		object? output = recipeType.GetProperty("Output")?.GetValue(recipe);
		if (output != null)
		{
			SetStackSize(output, batchSize);
		}

		if (recipeType.GetProperty("Ingredients")?.GetValue(recipe) is not IEnumerable ingredients)
		{
			return;
		}

		foreach (object? ingredient in ingredients)
		{
			if (ingredient is CraftingRecipeIngredient craftingIngredient)
			{
				UpdatePlainChalkIngredient(craftingIngredient, batchSize);
			}
		}
	}



	//// Updates an ingredient if it is the plain marking chalk input.
	////
	private static void UpdatePlainChalkIngredient(CraftingRecipeIngredient? ingredient, int batchSize)
	{
		if (ingredient?.Code?.Domain != "ghaelentweaks"
			|| ingredient.Code.Path != PlainChalkCode)
		{
			return;
		}

		ingredient.Quantity = batchSize;
		if (ingredient.ResolvedItemStack != null)
		{
			ingredient.ResolvedItemStack.StackSize = batchSize;
		}
	}



	//// Sets a JsonItemStack-like object's quantity fields through reflection.
	////
	private static void SetStackSize(object stackLikeObject, int batchSize)
	{
		Type type = stackLikeObject.GetType();
		type.GetProperty("Quantity")?.SetValue(stackLikeObject, batchSize);

		FieldInfo? stackSizeField = type.GetField("StackSize");
		stackSizeField?.SetValue(stackLikeObject, batchSize);

		object? resolvedStack = type.GetProperty("ResolvedItemStack")?.GetValue(stackLikeObject);
		if (resolvedStack is ItemStack itemStack)
		{
			itemStack.StackSize = batchSize;
		}
	}



	//// Reads a string property from a reflected recipe object.
	////
	private static string? GetStringProperty(object target, string propertyName)
	{
		return target.GetType().GetProperty(propertyName)?.GetValue(target) as string;
	}



	//// Identifies marking chalk dye recipes by their explicit recipe code or
	//// name prefix.
	////
	private static bool IsMarkingChalkDyeRecipe(string? recipeCode)
	{
		return recipeCode != null && recipeCode.StartsWith(RecipePrefix, StringComparison.Ordinal);
	}
}
