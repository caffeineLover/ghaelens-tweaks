/*
 * Applies the Harmony patches that let learned Better Ruins schematics act
 * as virtual crafting ingredients.
 *
 * The patch targets only GridRecipe.Matches and GridRecipe.ConsumeInput.
 * Matching keeps Vintage Story's recipe events pointed at the original
 * recipe, then evaluates a temporary ingredient list where a missing learned
 * schematic is treated as an empty slot. Consumption uses a cloned recipe
 * with the same temporary removal so the base game still consumes ordinary
 * ingredients through its normal code path.
 */

using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace GhaelenTweaks.BetterRuins;

internal static class BetterRuinsBlueprintRecipePatches
{
	private static ILogger? logger;



	//// Applies the Better Ruins learned-blueprint crafting patches.
	////
	//// The main mod system calls this on both client and server. Missing
	//// method targets are logged rather than thrown so a future Vintage Story
	//// API change disables only this feature instead of preventing the whole
	//// mod from loading.
	////
	internal static void Apply(Harmony harmony, ILogger patchLogger)
	{
		logger = patchLogger;

		try
		{
			MethodInfo? matchesMethod = AccessTools.Method(
				typeof(GridRecipe),
				nameof(GridRecipe.Matches),
				new[] { typeof(IPlayer), typeof(IWorldAccessor), typeof(ItemSlot[]), typeof(int) });

			MethodInfo? consumeInputMethod = AccessTools.Method(
				typeof(GridRecipe),
				nameof(GridRecipe.ConsumeInput),
				new[] { typeof(IPlayer), typeof(ItemSlot[]), typeof(int) });

			MethodInfo? matchesPrefix = AccessTools.Method(
				typeof(BetterRuinsBlueprintRecipePatches),
				nameof(PrefixGridRecipeMatches));

			MethodInfo? consumeInputPrefix = AccessTools.Method(
				typeof(BetterRuinsBlueprintRecipePatches),
				nameof(PrefixGridRecipeConsumeInput));

			if (matchesMethod == null || consumeInputMethod == null || matchesPrefix == null || consumeInputPrefix == null)
			{
				patchLogger.Warning("Better Ruins blueprint-learning recipe patch targets were not found.");
				return;
			}

			harmony.Patch(matchesMethod, prefix: new HarmonyMethod(matchesPrefix));
			harmony.Patch(consumeInputMethod, prefix: new HarmonyMethod(consumeInputPrefix));
			patchLogger.Notification("Better Ruins blueprint-learning crafting feature initialized.");
		}
		catch (Exception exception)
		{
			patchLogger.Warning(
				$"Better Ruins blueprint-learning crafting feature could not patch grid recipes: {exception.Message}");
		}
	}



	//// Replaces GridRecipe.Matches only for Better Ruins schematic recipes
	//// that can be satisfied by the player's learned knowledge.
	////
	//// Recipes with a matching physical schematic in the grid are passed
	//// through to vanilla unchanged. That preserves Better Ruins' original
	//// reusable-blueprint behavior and limits this patch to the missing-item
	//// case it needs to add.
	////
	private static bool PrefixGridRecipeMatches(
		GridRecipe __instance,
		IPlayer forPlayer,
		IWorldAccessor world,
		ItemSlot[] ingredients,
		int gridWidth,
		ref bool __result)
	{
		try
		{
			if (!TryCreateEffectiveIngredients(
				__instance,
				forPlayer,
				ingredients,
				out CraftingRecipeIngredient?[] effectiveIngredients))
			{
				return true;
			}

			if (!forPlayer.Entity.Api.Event.TriggerMatchesRecipe(forPlayer, __instance, ingredients))
			{
				__result = false;
				return false;
			}

			if (!forPlayer.Entity.Api.Event.TriggerMatchesRecipe(forPlayer, __instance, ingredients, gridWidth))
			{
				__result = false;
				return false;
			}

			__result = MatchesWithEffectiveIngredients(
				__instance,
				world,
				ingredients,
				gridWidth,
				effectiveIngredients);

			return false;
		}
		catch (Exception exception)
		{
			logger?.Warning(
				$"Better Ruins blueprint-learning match patch failed for recipe {__instance.Name}: {exception.Message}");
			return true;
		}
	}



	//// Replaces GridRecipe.ConsumeInput after a virtual schematic match.
	////
	//// The method prepares a clone whose missing learned schematic ingredient
	//// slots are null, then lets Vintage Story consume the remaining inputs
	//// normally. If the virtual recipe no longer matches, consumption fails
	//// closed instead of falling through to a mismatched vanilla consume path.
	////
	private static bool PrefixGridRecipeConsumeInput(
		GridRecipe __instance,
		IPlayer byPlayer,
		ItemSlot[] inputSlots,
		int gridWidth,
		ref bool __result)
	{
		try
		{
			if (!TryCreateEffectiveIngredients(
				__instance,
				byPlayer,
				inputSlots,
				out CraftingRecipeIngredient?[] effectiveIngredients))
			{
				return true;
			}

			IWorldAccessor world = byPlayer.Entity.World;
			if (!MatchesWithEffectiveIngredients(
				__instance,
				world,
				inputSlots,
				gridWidth,
				effectiveIngredients))
			{
				__result = false;
				return false;
			}

			GridRecipe virtualRecipe = __instance.Clone();
			if (virtualRecipe.ResolvedIngredients == null)
			{
				__result = false;
				return false;
			}

			for (int index = 0; index < effectiveIngredients.Length; index++)
			{
				if (effectiveIngredients[index] == null)
				{
					virtualRecipe.ResolvedIngredients[index] = null;
				}
			}

			__result = virtualRecipe.ConsumeInput(byPlayer, inputSlots, gridWidth);
			return false;
		}
		catch (Exception exception)
		{
			logger?.Warning(
				$"Better Ruins blueprint-learning consume patch failed for recipe {__instance.Name}: {exception.Message}");
			return true;
		}
	}



	//// Builds the ingredient list used when a learned schematic is absent
	//// from the crafting grid.
	////
	//// If the matching physical schematic is present anywhere in the grid,
	//// the method declines to handle the recipe so vanilla can process the
	//// original reusable blueprint item exactly as Better Ruins defined it.
	////
	private static bool TryCreateEffectiveIngredients(
		GridRecipe recipe,
		IPlayer player,
		ItemSlot[] inputSlots,
		out CraftingRecipeIngredient?[] effectiveIngredients)
	{
		effectiveIngredients = Array.Empty<CraftingRecipeIngredient?>();
		CraftingRecipeIngredient?[]? resolvedIngredients = recipe.ResolvedIngredients;
		if (!GhaelenTweaksConfig.Current.BetterRuinsBlueprintLearning
			|| resolvedIngredients == null
			|| resolvedIngredients.Length == 0)
		{
			return false;
		}

		bool hasSchematicIngredient = resolvedIngredients.Any(
			ingredient => BetterRuinsBlueprintKnowledge.TryGetExactSchematicIngredientCode(ingredient, out _));
		if (!hasSchematicIngredient || HasMatchingPhysicalSchematic(recipe, inputSlots, resolvedIngredients))
		{
			return false;
		}

		effectiveIngredients = new CraftingRecipeIngredient?[resolvedIngredients.Length];
		bool removedLearnedSchematic = false;

		for (int index = 0; index < resolvedIngredients.Length; index++)
		{
			CraftingRecipeIngredient? ingredient = resolvedIngredients[index];
			if (BetterRuinsBlueprintKnowledge.TryGetExactSchematicIngredientCode(ingredient, out string schematicCode)
				&& BetterRuinsBlueprintKnowledge.HasLearned(player, schematicCode))
			{
				effectiveIngredients[index] = null;
				removedLearnedSchematic = true;
				continue;
			}

			effectiveIngredients[index] = ingredient;
		}

		return removedLearnedSchematic;
	}



	//// Checks whether any supplied grid slot already contains a schematic
	//// that satisfies one of the recipe's schematic ingredients.
	////
	//// This intentionally ignores grid position. Any matching physical
	//// schematic means the patch can let vanilla decide whether the whole
	//// shaped or shapeless layout is valid.
	////
	private static bool HasMatchingPhysicalSchematic(
		GridRecipe recipe,
		ItemSlot[] inputSlots,
		CraftingRecipeIngredient?[] resolvedIngredients)
	{
		foreach (ItemSlot inputSlot in inputSlots)
		{
			ItemStack? inputStack = inputSlot.Itemstack;
			if (inputStack == null || !BetterRuinsBlueprintKnowledge.TryGetSchematicCode(inputStack, out _))
			{
				continue;
			}

			foreach (CraftingRecipeIngredient? ingredient in resolvedIngredients)
			{
				if (ingredient == null
					|| !BetterRuinsBlueprintKnowledge.TryGetExactSchematicIngredientCode(ingredient, out _))
				{
					continue;
				}

				if (ingredient.SatisfiesAsIngredient(inputStack)
					&& inputStack.Collectible.MatchesForCrafting(inputStack, recipe, ingredient))
				{
					return true;
				}
			}
		}

		return false;
	}



	//// Runs a vanilla-equivalent recipe match against the temporary
	//// ingredient list.
	////
	//// The caller has already handled Vintage Story's recipe events. This
	//// method performs only the grid and ingredient checks so the original
	//// recipe remains visible to other event subscribers.
	////
	private static bool MatchesWithEffectiveIngredients(
		GridRecipe recipe,
		IWorldAccessor world,
		ItemSlot[] inputSlots,
		int gridWidth,
		CraftingRecipeIngredient?[] effectiveIngredients)
	{
		if (gridWidth <= 0)
		{
			return false;
		}

		int gridHeight = inputSlots.Length / gridWidth;
		if (gridWidth < recipe.Width || gridHeight < recipe.Height)
		{
			return false;
		}

		return recipe.Shapeless
			? MatchesShapeless(recipe, world, inputSlots, effectiveIngredients)
			: MatchesShaped(recipe, inputSlots, gridWidth, effectiveIngredients);
	}



	//// Checks a shaped recipe against the temporary ingredient list.
	////
	//// This mirrors Vintage Story's shaped grid scan: each possible recipe
	//// offset is tested, and all non-recipe grid cells must be empty.
	////
	private static bool MatchesShaped(
		GridRecipe recipe,
		ItemSlot[] inputSlots,
		int gridWidth,
		CraftingRecipeIngredient?[] effectiveIngredients)
	{
		int gridHeight = inputSlots.Length / gridWidth;

		for (int colStart = 0; colStart <= gridWidth - recipe.Width; colStart++)
		{
			for (int rowStart = 0; rowStart <= gridHeight - recipe.Height; rowStart++)
			{
				if (MatchesAtPosition(
					recipe,
					colStart,
					rowStart,
					inputSlots,
					gridWidth,
					effectiveIngredients))
				{
					return true;
				}
			}
		}

		return false;
	}



	//// Checks one shaped-recipe offset against the full crafting grid.
	////
	//// Learned schematic entries have already been replaced with null. That
	//// makes the original schematic slot behave like an ordinary empty slot
	//// while every other ingredient and every extra input remains strict.
	////
	private static bool MatchesAtPosition(
		GridRecipe recipe,
		int colStart,
		int rowStart,
		ItemSlot[] inputSlots,
		int gridWidth,
		CraftingRecipeIngredient?[] effectiveIngredients)
	{
		int gridHeight = inputSlots.Length / gridWidth;

		for (int col = 0; col < gridWidth; col++)
		{
			for (int row = 0; row < gridHeight; row++)
			{
				ItemStack? inputStack = GetElementInGrid(row, col, inputSlots, gridWidth)?.Itemstack;
				IRecipeIngredient? ingredient = GetElementInGrid(
					row - rowStart,
					col - colStart,
					effectiveIngredients,
					recipe.Width);

				if (!MatchStackToIngredient(recipe, inputStack, ingredient))
				{
					return false;
				}
			}
		}

		return true;
	}



	//// Checks a shapeless recipe against the temporary ingredient list.
	////
	//// The implementation follows Vintage Story's matching phases: merge
	//// supplied stacks, satisfy wildcard ingredients, merge exact ingredients,
	//// then compare the exact ingredient stacks against the remaining inputs.
	////
	private static bool MatchesShapeless(
		GridRecipe recipe,
		IWorldAccessor world,
		ItemSlot[] suppliedSlots,
		CraftingRecipeIngredient?[] effectiveIngredients)
	{
		List<ItemStack> suppliedStacks = new();
		MergeStacks(suppliedSlots, suppliedStacks);

		if (!MatchWildcardIngredients(recipe, suppliedStacks, effectiveIngredients))
		{
			return false;
		}

		List<(ItemStack Stack, IRecipeIngredient Ingredient)> ingredientStacks = new();
		MergeIngredientStacks(world, ingredientStacks, effectiveIngredients);
		if (ingredientStacks.Count != suppliedStacks.Count)
		{
			return false;
		}

		return MatchIngredientStacks(recipe, ingredientStacks, suppliedStacks);
	}



	//// Merges equivalent supplied input stacks for shapeless matching.
	////
	//// Vintage Story performs shapeless matching against merged stack copies
	//// so multiple grid slots containing the same item can satisfy one larger
	//// quantity requirement.
	////
	private static void MergeStacks(
		ItemSlot[] slots,
		List<ItemStack> stacks)
	{
		foreach (ItemStack suppliedStack in slots.Select(slot => slot.Itemstack).OfType<ItemStack>())
		{
			ItemStack? similarStack = stacks.Find(stack => stack.Satisfies(suppliedStack));
			if (similarStack != null)
			{
				similarStack.StackSize += suppliedStack.StackSize;
			}
			else
			{
				stacks.Add(suppliedStack.Clone());
			}
		}
	}



	//// Satisfies shapeless wildcard and tool ingredients from the supplied
	//// stack list.
	////
	//// This mirrors the base game's behavior of removing the matched supplied
	//// stack from further matching once a wildcard ingredient accepts it.
	////
	private static bool MatchWildcardIngredients(
		GridRecipe recipe,
		List<ItemStack> suppliedStacks,
		CraftingRecipeIngredient?[] effectiveIngredients)
	{
		foreach (IRecipeIngredient ingredient in effectiveIngredients
			.OfType<IRecipeIngredient>()
			.Where(recipeIngredient => recipeIngredient.MatchingType != EnumRecipeMatchType.Exact))
		{
			bool found = false;
			int foundIndex = -1;

			for (int index = 0; index < suppliedStacks.Count; index++)
			{
				ItemStack inputStack = suppliedStacks[index];
				found = ingredient.Type == inputStack.Class
					&& WildcardUtil.Match(ingredient.Code, inputStack.Collectible.Code, ingredient.AllowedVariants)
					&& inputStack.StackSize >= ingredient.Quantity
					&& ingredient.Tags.Matches(in inputStack.Collectible.Tags)
					&& inputStack.Collectible.MatchesForCrafting(inputStack, recipe, ingredient);

				if (found)
				{
					foundIndex = index;
					break;
				}
			}

			if (!found)
			{
				return false;
			}

			suppliedStacks.RemoveAt(foundIndex);
		}

		return true;
	}



	//// Merges exact shapeless ingredients into the stack list that will be
	//// compared against the remaining supplied inputs.
	////
	//// Null entries represent learned missing schematics and are skipped;
	//// other exact ingredients preserve their recipe attributes and normal
	//// stack-size requirements.
	////
	private static void MergeIngredientStacks(
		IWorldAccessor world,
		List<(ItemStack Stack, IRecipeIngredient Ingredient)> stacks,
		CraftingRecipeIngredient?[] effectiveIngredients)
	{
		foreach (IRecipeIngredient ingredient in effectiveIngredients
			.OfType<IRecipeIngredient>()
			.Where(recipeIngredient => recipeIngredient.MatchingType == EnumRecipeMatchType.Exact))
		{
			ItemStack? ingredientStack = ingredient.ResolvedItemStack;
			if (ingredientStack == null)
			{
				continue;
			}

			ItemStack? similarStack = null;
			foreach ((ItemStack stack, IRecipeIngredient _) in stacks)
			{
				if (stack.Equals(world, ingredientStack, GlobalConstants.IgnoredStackAttributes)
					&& ingredient.RecipeAttributes == null)
				{
					similarStack = stack;
					break;
				}
			}

			if (similarStack != null)
			{
				similarStack.StackSize += ingredientStack.StackSize;
			}
			else
			{
				stacks.Add((ingredientStack.Clone(), ingredient));
			}
		}
	}



	//// Compares merged exact ingredient stacks against the remaining supplied
	//// stacks for a shapeless recipe.
	////
	//// A supplied stack must satisfy the resolved ingredient stack, provide
	//// enough quantity, and pass the collectible's normal crafting filter.
	////
	private static bool MatchIngredientStacks(
		GridRecipe recipe,
		List<(ItemStack Stack, IRecipeIngredient Ingredient)> ingredientStacks,
		List<ItemStack> suppliedStacks)
	{
		foreach ((ItemStack stack, IRecipeIngredient ingredient) in ingredientStacks)
		{
			bool found = false;

			for (int index = 0; index < suppliedStacks.Count; index++)
			{
				ItemStack suppliedStack = suppliedStacks[index];
				found = stack.Satisfies(suppliedStack)
					&& stack.StackSize <= suppliedStack.StackSize
					&& suppliedStack.Collectible.MatchesForCrafting(suppliedStack, recipe, ingredient);

				if (found)
				{
					suppliedStacks.RemoveAt(index);
					break;
				}
			}

			if (!found)
			{
				return false;
			}
		}

		return true;
	}



	//// Applies Vintage Story's normal stack-to-ingredient checks.
	////
	//// The temporary ingredient array already contains null where a learned
	//// schematic should be virtual, so the ordinary null/non-null comparison
	//// remains correct for empty slots and extra input stacks.
	////
	private static bool MatchStackToIngredient(
		GridRecipe recipe,
		ItemStack? inputStack,
		IRecipeIngredient? ingredient)
	{
		if (inputStack != null && ingredient != null)
		{
			return ingredient.SatisfiesAsIngredient(inputStack)
				&& inputStack.Collectible.MatchesForCrafting(inputStack, recipe, ingredient);
		}

		return inputStack == null && ingredient == null;
	}



	//// Retrieves a grid element using the same row and column bounds rules as
	//// Vintage Story's RecipeBase helper.
	////
	//// Returning null for out-of-recipe coordinates lets the shaped matcher
	//// require all cells outside the current recipe offset to be empty.
	////
	private static TGridCellElement? GetElementInGrid<TGridCellElement>(
		int row,
		int column,
		TGridCellElement[] cells,
		int gridWidth)
	{
		int gridHeight = cells.Length / gridWidth;
		if (row < 0 || column < 0 || row >= gridHeight || column >= gridWidth)
		{
			return default;
		}

		return cells[row * gridWidth + column];
	}
}
