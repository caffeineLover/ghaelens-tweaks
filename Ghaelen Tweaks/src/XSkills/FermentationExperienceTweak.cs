#nullable disable
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace GhaelenTweaks.XSkills
{
    /// <summary>
    /// Adds process-based Fermentation XP to xSkills without compile-time
    /// references to xSkills, xLib, or VSSurvivalMod.
    ///
    /// The tweak leaves xSkills Fork's existing +3 XP barrel-sealing award intact.
    /// It supplements that award with XP for actual juice production, successful
    /// fermentation, recipe discovery, and successful distillation.
    /// </summary>
    public sealed class FermentationExperienceTweak : ModSystem
    {
        private const string HarmonyId =
            "ghaelentweaks.xskills.fermentationexperience";

        private const string PendingAwardsSaveKey =
            "ghaelentweaks-xskills-fermentation-pending-awards";

        private const string DiscoveryTreeKey =
            "ghaelentweaks-fermentation-discoveries";

        private const string BarrelActiveKey =
            "gtFermentationActive";

        private const string BarrelBrewerUidKey =
            "gtFermentationBrewerUid";

        private const string BarrelRecipeCodeKey =
            "gtFermentationRecipeCode";

        private const string BarrelStartLitresKey =
            "gtFermentationStartLitres";

        private const string BoilerOwnerUidKey =
            "gtFermentationDistillerUid";

        internal const float FruitPressXpPerLitre = 0.10f;
        internal const float CompletionLinearXpPerLitre = 0.75f;
        internal const float CompletionLargeBatchCoefficient = 0.002f;
        internal const float FirstBrewTypeBonus = 8.0f;
        internal const float DistillationXpPerLitre = 4.0f;
        internal const float FirstDistillationBonus = 5.0f;

        private const float PressXpFlushThreshold = 0.25f;

        private static readonly ConditionalWeakTable<object, BarrelState>
            BarrelStates = new ConditionalWeakTable<object, BarrelState>();

        private static readonly ConditionalWeakTable<object, PressState>
            PressStates = new ConditionalWeakTable<object, PressState>();

        private static readonly ConditionalWeakTable<object, BoilerState>
            BoilerStates = new ConditionalWeakTable<object, BoilerState>();

        private static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        private static ICoreServerAPI sapi;

        private static Dictionary<string, PendingPlayerAwards> pendingAwards =
            new Dictionary<string, PendingPlayerAwards>();

        private static MethodInfo getContainablePropsMethod;
        private static bool liquidReflectionInitialized;

        private Harmony harmony;
        private bool active;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            XSkillsBridge.Initialize(api);

            if (!XSkillsBridge.IsXSkillsLoaded(api))
            {
                api.Logger.Notification(
                    "[Ghaelen's Tweaks] xSkills is not installed; "
                    + "Fermentation XP tweaks are inactive.");
                return;
            }

            LoadPendingAwards();

            api.Event.PlayerReady += OnPlayerReady;
            api.Event.GameWorldSave += SavePendingAwards;

            harmony = new Harmony(HarmonyId);

            Type barrelType = ResolveGameType(
                "Barrel",
                "Vintagestory.GameContent.BlockEntityBarrel");

            Type fruitPressType = ResolveGameType(
                "FruitPress",
                "Vintagestory.GameContent.BlockEntityFruitPress");

            Type boilerEntityType = ResolveGameType(
                "Boiler",
                "Vintagestory.GameContent.BlockEntityBoiler");

            Type boilerBlockType =
                AccessTools.TypeByName(
                    "Vintagestory.GameContent.BlockBoiler");

            Patch(
                AccessTools.Method(
                    barrelType,
                    "OnReceivedClientPacket",
                    new[] { typeof(IPlayer), typeof(int), typeof(byte[]) }),
                postfixName: nameof(BarrelSealPostfix));

            Patch(
                AccessTools.Method(
                    barrelType,
                    "OnEvery3Second",
                    new[] { typeof(float) }),
                prefixName: nameof(BarrelCompletionPrefix),
                postfixName: nameof(BarrelCompletionPostfix));

            Patch(
                AccessTools.Method(
                    barrelType,
                    "ToTreeAttributes",
                    new[] { typeof(ITreeAttribute) }),
                postfixName: nameof(BarrelToTreePostfix));

            Patch(
                AccessTools.Method(
                    barrelType,
                    "FromTreeAttributes",
                    new[] { typeof(ITreeAttribute), typeof(IWorldAccessor) }),
                postfixName: nameof(BarrelFromTreePostfix));

            Patch(
                AccessTools.Method(
                    fruitPressType,
                    "OnReceivedClientPacket",
                    new[] { typeof(IPlayer), typeof(int), typeof(byte[]) }),
                postfixName: nameof(FruitPressPacketPostfix));

            Patch(
                AccessTools.Method(
                    fruitPressType,
                    "onTick100msServer",
                    new[] { typeof(float) }),
                prefixName: nameof(FruitPressTickPrefix),
                postfixName: nameof(FruitPressTickPostfix));

            Patch(
                AccessTools.Method(
                    fruitPressType,
                    "OnBlockInteractStop",
                    new[] { typeof(float), typeof(IPlayer) }),
                postfixName: nameof(FruitPressInteractionEndedPostfix));

            Patch(
                AccessTools.Method(
                    fruitPressType,
                    "OnBlockInteractCancel",
                    new[] { typeof(float), typeof(IPlayer) }),
                postfixName: nameof(FruitPressInteractionEndedPostfix));

            Patch(
                AccessTools.Method(
                    boilerBlockType,
                    "OnBlockInteractStart",
                    new[]
                    {
                        typeof(IWorldAccessor),
                        typeof(IPlayer),
                        typeof(BlockSelection)
                    }),
                postfixName: nameof(BoilerInteractionPostfix));

            Patch(
                AccessTools.Method(
                    boilerEntityType,
                    "onBurnTick",
                    new[] { typeof(float) }),
                prefixName: nameof(BoilerBurnTickPrefix),
                postfixName: nameof(BoilerBurnTickPostfix));

            Patch(
                AccessTools.Method(
                    boilerEntityType,
                    "ToTreeAttributes",
                    new[] { typeof(ITreeAttribute) }),
                postfixName: nameof(BoilerToTreePostfix));

            Patch(
                AccessTools.Method(
                    boilerEntityType,
                    "FromTreeAttributes",
                    new[] { typeof(ITreeAttribute), typeof(IWorldAccessor) }),
                postfixName: nameof(BoilerFromTreePostfix));

            active = true;

            api.Logger.Notification(
                "[Ghaelen's Tweaks] xSkills Fermentation XP tweaks enabled.");
        }

        public override void Dispose()
        {
            if (active && sapi != null)
            {
                sapi.Event.PlayerReady -= OnPlayerReady;
                sapi.Event.GameWorldSave -= SavePendingAwards;
                SavePendingAwards();
            }

            if (harmony != null)
            {
                harmony.UnpatchAll(HarmonyId);
            }

            active = false;
            harmony = null;
            sapi = null;

            base.Dispose();
        }

        private static Type ResolveGameType(
            string blockEntityClassName,
            string fallbackFullName)
        {
            Type type = sapi?.ClassRegistry.GetBlockEntity(blockEntityClassName);
            return type ?? AccessTools.TypeByName(fallbackFullName);
        }

        private void Patch(
            MethodBase original,
            string prefixName = null,
            string postfixName = null)
        {
            if (original == null)
            {
                sapi.Logger.Warning(
                    "[Ghaelen's Tweaks] A Fermentation XP hook could not be "
                    + "resolved for this Vintage Story version.");
                return;
            }

            HarmonyMethod prefix = prefixName == null
                ? null
                : new HarmonyMethod(
                    AccessTools.Method(
                        typeof(FermentationExperienceTweak),
                        prefixName));

            HarmonyMethod postfix = postfixName == null
                ? null
                : new HarmonyMethod(
                    AccessTools.Method(
                        typeof(FermentationExperienceTweak),
                        postfixName));

            harmony.Patch(original, prefix, postfix);
        }

        private static void BarrelSealPostfix(
            object __instance,
            IPlayer player,
            int packetid)
        {
            if (packetid != 1337
                || player == null
                || !GetMemberValue(__instance, "Sealed", false))
            {
                return;
            }

            object recipe = GetMemberValue(__instance, "CurrentRecipe");
            string recipeCode =
                GetMemberValue<string>(recipe, "Code", null);

            if (!IsBrewingRecipe(recipeCode))
            {
                return;
            }

            BarrelState state = BarrelStates.GetOrCreateValue(__instance)!;
            state.Active = true;
            state.BrewerUid = player.PlayerUID;
            state.RecipeCode = recipeCode;
            state.StartLitres = GetRecipeOutputLitres(__instance);

            if (state.StartLitres <= 0f)
            {
                state.StartLitres = GetBarrelLiquidLitres(__instance);
            }

            MarkDirty(__instance);
        }

        private static void BarrelCompletionPrefix(
            object __instance,
            out BarrelTickState __state)
        {
            object recipe = GetMemberValue(__instance, "CurrentRecipe");

            __state = new BarrelTickState
            {
                WasSealed =
                    GetMemberValue(__instance, "Sealed", false),
                RecipeCode =
                    GetMemberValue<string>(recipe, "Code", null)
            };
        }

        private static void BarrelCompletionPostfix(
            object __instance,
            BarrelTickState __state)
        {
            if (!__state.WasSealed
                || GetMemberValue(__instance, "Sealed", false)
                || !IsBrewingRecipe(__state.RecipeCode))
            {
                return;
            }

            BarrelState state = BarrelStates.GetOrCreateValue(__instance)!;

            if (!state.Active
                || string.IsNullOrEmpty(state.BrewerUid)
                || !IsBrewingRecipe(state.RecipeCode))
            {
                return;
            }

            float litres = state.StartLitres;

            if (litres <= 0f)
            {
                litres = GetBarrelLiquidLitres(__instance);
            }

            if (litres > 0f)
            {
                AwardOrQueue(
                    state.BrewerUid,
                    CompletionExperience(litres, state.RecipeCode),
                    GetBrewDiscoveryKey(state.RecipeCode));
            }

            ClearBarrelState(state);
            MarkDirty(__instance);
        }

        private static void BarrelToTreePostfix(
            object __instance,
            ITreeAttribute tree)
        {
            BarrelState state = BarrelStates.GetOrCreateValue(__instance)!;

            tree.SetBool(BarrelActiveKey, state.Active);

            if (!state.Active)
            {
                tree.RemoveAttribute(BarrelBrewerUidKey);
                tree.RemoveAttribute(BarrelRecipeCodeKey);
                tree.RemoveAttribute(BarrelStartLitresKey);
                return;
            }

            tree.SetString(
                BarrelBrewerUidKey,
                state.BrewerUid ?? string.Empty);

            tree.SetString(
                BarrelRecipeCodeKey,
                state.RecipeCode ?? string.Empty);

            tree.SetFloat(
                BarrelStartLitresKey,
                state.StartLitres);
        }

        private static void BarrelFromTreePostfix(
            object __instance,
            ITreeAttribute tree)
        {
            BarrelState state = BarrelStates.GetOrCreateValue(__instance)!;

            state.Active = tree.GetBool(BarrelActiveKey, false);

            state.BrewerUid = state.Active
                ? tree.GetString(BarrelBrewerUidKey, null)
                : null;

            state.RecipeCode = state.Active
                ? tree.GetString(BarrelRecipeCodeKey, null)
                : null;

            state.StartLitres = state.Active
                ? tree.GetFloat(BarrelStartLitresKey, 0f)
                : 0f;
        }

        private static void FruitPressPacketPostfix(
            object __instance,
            IPlayer fromPlayer,
            int packetid)
        {
            if (fromPlayer == null
                || (packetid != 1002 && packetid != 1004))
            {
                return;
            }

            PressState state = PressStates.GetOrCreateValue(__instance)!;
            state.PlayerUid = fromPlayer.PlayerUID;
        }

        private static void FruitPressTickPrefix(
            object __instance,
            out float __state)
        {
            __state = GetInventoryContainerLitres(__instance, 1);
        }

        private static void FruitPressTickPostfix(
            object __instance,
            float __state)
        {
            float after = GetInventoryContainerLitres(__instance, 1);
            float produced = after - __state;

            if (produced <= 0f)
            {
                return;
            }

            PressState state = PressStates.GetOrCreateValue(__instance)!;

            if (string.IsNullOrEmpty(state.PlayerUid))
            {
                return;
            }

            state.PendingExperience += produced * FruitPressXpPerLitre;

            ItemStack mashStack =
                GetInventorySlot(__instance, 0)?.Itemstack;

            bool mashExhausted =
                (mashStack?.Attributes.GetDouble(
                    "juiceableLitresLeft",
                    0.0) ?? 0.0) <= 0.01;

            if (state.PendingExperience >= PressXpFlushThreshold
                || mashExhausted)
            {
                FlushFruitPressExperience(state);
            }
        }

        private static void FruitPressInteractionEndedPostfix(
            object __instance)
        {
            FlushFruitPressExperience(
                PressStates.GetOrCreateValue(__instance));
        }

        private static void FlushFruitPressExperience(
            PressState state)
        {
            if (state.PendingExperience <= 0f
                || string.IsNullOrEmpty(state.PlayerUid))
            {
                return;
            }

            float experience = state.PendingExperience;
            state.PendingExperience = 0f;

            AwardOrQueue(
                state.PlayerUid,
                experience,
                null);
        }

        private static void BoilerInteractionPostfix(
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            bool __result)
        {
            if (!__result
                || byPlayer == null
                || blockSel == null)
            {
                return;
            }

            object boiler =
                world.BlockAccessor.GetBlockEntity(
                    blockSel.Position);

            if (boiler == null)
            {
                return;
            }

            BoilerState state = BoilerStates.GetOrCreateValue(boiler)!;
            state.PlayerUid = byPlayer.PlayerUID;

            MarkDirty(boiler);
        }

        private static void BoilerBurnTickPrefix(
            object __instance,
            out float __state)
        {
            __state = GetAdjacentDistillateLitres(__instance);
        }

        private static void BoilerBurnTickPostfix(
            object __instance,
            float __state)
        {
            float after = GetAdjacentDistillateLitres(__instance)!;
            float produced = after - __state;

            if (produced <= 0f)
            {
                return;
            }

            BoilerState state = BoilerStates.GetOrCreateValue(__instance)!;

            if (string.IsNullOrEmpty(state.PlayerUid))
            {
                return;
            }

            AwardOrQueue(
                state.PlayerUid,
                produced * DistillationXpPerLitre,
                "distillation");
        }

        private static void BoilerToTreePostfix(
            object __instance,
            ITreeAttribute tree)
        {
            BoilerState state = BoilerStates.GetOrCreateValue(__instance)!;

            if (string.IsNullOrEmpty(state.PlayerUid))
            {
                tree.RemoveAttribute(BoilerOwnerUidKey);
                return;
            }

            tree.SetString(
                BoilerOwnerUidKey,
                state.PlayerUid);
        }

        private static void BoilerFromTreePostfix(
            object __instance,
            ITreeAttribute tree)
        {
            BoilerState state = BoilerStates.GetOrCreateValue(__instance)!;
            state.PlayerUid =
                tree.GetString(BoilerOwnerUidKey, null);
        }

        private static float CompletionExperience(
            float litres,
            string recipeCode)
        {
            float baseExperience =
                CompletionLinearXpPerLitre * litres
                + CompletionLargeBatchCoefficient
                    * litres
                    * litres;

            return baseExperience
                * GetRecipeMultiplier(recipeCode);
        }

        private static float GetRecipeMultiplier(
            string recipeCode)
        {
            string code =
                recipeCode?.ToLowerInvariant()
                ?? string.Empty;

            if (code.Contains("beer")
                || code.Contains("ale"))
            {
                return 1.25f;
            }

            if (code.Contains("mead"))
            {
                return 1.15f;
            }

            if (code.Contains("wine"))
            {
                return 1.10f;
            }

            return 1.00f;
        }

        private static bool IsBrewingRecipe(
            string recipeCode)
        {
            if (string.IsNullOrEmpty(recipeCode))
            {
                return false;
            }

            string code = recipeCode.ToLowerInvariant();

            return code.Contains("cider")
                || code.Contains("mead")
                || code.Contains("beer")
                || code.Contains("wine")
                || code.Contains("ale")
                || code.Contains("perry");
        }

        private static string GetBrewDiscoveryKey(
            string recipeCode)
        {
            string code =
                recipeCode?.ToLowerInvariant()
                ?? string.Empty;

            if (code.Contains("cider"))
            {
                return "cider";
            }

            if (code.Contains("perry"))
            {
                return "perry";
            }

            if (code.Contains("mead"))
            {
                return "mead";
            }

            if (code.Contains("beer"))
            {
                return "beer";
            }

            if (code.Contains("ale"))
            {
                return "ale";
            }

            if (code.Contains("wine"))
            {
                return "wine";
            }

            return null;
        }

        private static float GetRecipeOutputLitres(
            object barrel)
        {
            object recipe =
                GetMemberValue(barrel, "CurrentRecipe");

            object output =
                GetMemberValue(recipe, "Output");

            ItemStack outputStack =
                GetMemberValue<ItemStack>(
                    output,
                    "ResolvedItemStack",
                    null);

            int outputSize =
                GetMemberValue(
                    barrel,
                    "CurrentOutSize",
                    0);

            if (outputStack == null || outputSize <= 0)
            {
                return 0f;
            }

            float itemsPerLitre =
                GetItemsPerLitre(outputStack);

            if (itemsPerLitre <= 0f)
            {
                return 0f;
            }

            return outputSize / itemsPerLitre;
        }

        private static float GetBarrelLiquidLitres(
            object barrel)
        {
            InventoryBase inventory = GetInventory(barrel);

            if (inventory == null)
            {
                return 0f;
            }

            float litres = 0f;

            for (int i = 0; i < inventory.Count; i++)
            {
                ItemStack stack = inventory[i]?.Itemstack;

                if (stack == null)
                {
                    continue;
                }

                float itemsPerLitre =
                    GetItemsPerLitre(stack);

                if (itemsPerLitre > 0f)
                {
                    litres += stack.StackSize / itemsPerLitre;
                }
            }

            return litres;
        }

        private static float GetAdjacentDistillateLitres(
            object boiler)
        {
            BlockEntity blockEntity = boiler as BlockEntity;

            if (blockEntity?.Api?.World == null
                || blockEntity.Pos == null)
            {
                return 0f;
            }

            float total = 0f;

            for (int i = 0;
                i < BlockFacing.HORIZONTALS.Length;
                i++)
            {
                BlockPos pos =
                    blockEntity.Pos.AddCopy(
                        BlockFacing.HORIZONTALS[i]);

                object condenser =
                    blockEntity.Api.World.BlockAccessor
                        .GetBlockEntity(pos);

                if (condenser == null
                    || condenser.GetType().Name
                        .IndexOf(
                            "Condenser",
                            StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                total +=
                    GetInventoryContainerLitres(
                        condenser,
                        1);
            }

            return total;
        }

        private static float GetInventoryContainerLitres(
            object blockEntity,
            int slotIndex)
        {
            ItemStack containerStack =
                GetInventorySlot(
                    blockEntity,
                    slotIndex)?.Itemstack;

            if (containerStack?.Collectible == null)
            {
                return 0f;
            }

            try
            {
                MethodInfo method =
                    containerStack.Collectible.GetType().GetMethod(
                        "GetCurrentLitres",
                        InstanceFlags,
                        binder: null,
                        types: new[] { typeof(ItemStack) },
                        modifiers: null);

                if (method == null)
                {
                    return 0f;
                }

                object result =
                    method.Invoke(
                        containerStack.Collectible,
                        new object[] { containerStack });

                return Convert.ToSingle(result);
            }
            catch
            {
                return 0f;
            }
        }

        private static float GetItemsPerLitre(
            ItemStack liquidStack)
        {
            if (liquidStack == null)
            {
                return 0f;
            }

            EnsureLiquidReflection();

            if (getContainablePropsMethod == null)
            {
                return 0f;
            }

            try
            {
                object props =
                    getContainablePropsMethod.Invoke(
                        null,
                        new object[] { liquidStack });

                if (props == null)
                {
                    return 0f;
                }

                object value =
                    GetMemberValue(
                        props,
                        "ItemsPerLitre");

                return value == null
                    ? 0f
                    : Convert.ToSingle(value);
            }
            catch
            {
                return 0f;
            }
        }

        private static void EnsureLiquidReflection()
        {
            if (liquidReflectionInitialized)
            {
                return;
            }

            liquidReflectionInitialized = true;

            Type liquidContainerType =
                AccessTools.TypeByName(
                    "Vintagestory.GameContent.BlockLiquidContainerBase");

            getContainablePropsMethod =
                AccessTools.Method(
                    liquidContainerType,
                    "GetContainableProps",
                    new[] { typeof(ItemStack) });
        }

        private static InventoryBase GetInventory(
            object blockEntity)
        {
            return GetMemberValue<InventoryBase>(
                blockEntity,
                "Inventory",
                null);
        }

        private static ItemSlot GetInventorySlot(
            object blockEntity,
            int index)
        {
            InventoryBase inventory =
                GetInventory(blockEntity);

            if (inventory == null
                || index < 0
                || index >= inventory.Count)
            {
                return null;
            }

            return inventory[index];
        }

        private static void MarkDirty(object instance)
        {
            (instance as BlockEntity)?.MarkDirty(true);
        }

        private static object GetMemberValue(
            object instance,
            string name)
        {
            if (instance == null)
            {
                return null;
            }

            Type type = instance.GetType();

            PropertyInfo property =
                type.GetProperty(name, InstanceFlags);

            if (property != null)
            {
                return property.GetValue(instance);
            }

            FieldInfo field =
                type.GetField(name, InstanceFlags);

            return field?.GetValue(instance);
        }

        private static T GetMemberValue<T>(
            object instance,
            string name,
            T fallback)
        {
            object value =
                GetMemberValue(instance, name);

            if (value == null)
            {
                return fallback;
            }

            if (value is T typed)
            {
                return typed;
            }

            try
            {
                return (T)Convert.ChangeType(
                    value,
                    typeof(T));
            }
            catch
            {
                return fallback;
            }
        }

        private static void AwardOrQueue(
            string playerUid,
            float experience,
            string discoveryKey)
        {
            if (string.IsNullOrEmpty(playerUid))
            {
                return;
            }

            IServerPlayer player =
                sapi?.World.PlayerByUid(playerUid)
                    as IServerPlayer;

            if (player?.Entity != null
                && (experience <= 0f
                    || XSkillsBridge.TryAddFermentationExperience(
                        player,
                        experience)))
            {
                if (!string.IsNullOrEmpty(discoveryKey)
                    && !TryAwardDiscovery(
                        player,
                        discoveryKey))
                {
                    QueuePendingDiscovery(
                        playerUid,
                        discoveryKey);
                }

                return;
            }

            PendingPlayerAwards pending =
                GetPendingPlayer(playerUid);

            pending.Experience +=
                Math.Max(0f, experience);

            if (!string.IsNullOrEmpty(discoveryKey)
                && !pending.Discoveries.Contains(
                    discoveryKey))
            {
                pending.Discoveries.Add(discoveryKey);
            }
        }

        private static bool TryAwardDiscovery(
            IServerPlayer player,
            string discoveryKey)
        {
            if (player?.Entity == null
                || string.IsNullOrEmpty(discoveryKey))
            {
                return false;
            }

            ITreeAttribute discoveries =
                player.Entity.WatchedAttributes
                    .GetOrAddTreeAttribute(
                        DiscoveryTreeKey);

            if (discoveries.GetBool(
                discoveryKey,
                false))
            {
                return true;
            }

            float bonus =
                discoveryKey == "distillation"
                    ? FirstDistillationBonus
                    : FirstBrewTypeBonus;

            if (!XSkillsBridge
                .TryAddFermentationExperience(
                    player,
                    bonus))
            {
                return false;
            }

            discoveries.SetBool(
                discoveryKey,
                true);

            player.Entity.WatchedAttributes
                .MarkPathDirty(DiscoveryTreeKey);

            return true;
        }

        private static PendingPlayerAwards GetPendingPlayer(
            string playerUid)
        {
            if (!pendingAwards.TryGetValue(
                playerUid,
                out PendingPlayerAwards pending))
            {
                pending = new PendingPlayerAwards();
                pendingAwards[playerUid] = pending;
            }

            return pending;
        }

        private static void QueuePendingDiscovery(
            string playerUid,
            string discoveryKey)
        {
            PendingPlayerAwards pending =
                GetPendingPlayer(playerUid);

            if (!pending.Discoveries.Contains(
                discoveryKey))
            {
                pending.Discoveries.Add(discoveryKey);
            }
        }

        private static void OnPlayerReady(
            IServerPlayer player)
        {
            if (player == null
                || !pendingAwards.TryGetValue(
                    player.PlayerUID,
                    out PendingPlayerAwards pending))
            {
                return;
            }

            if (pending.Experience > 0f)
            {
                if (!XSkillsBridge
                    .TryAddFermentationExperience(
                        player,
                        pending.Experience))
                {
                    return;
                }

                pending.Experience = 0f;
            }

            for (int i =
                    pending.Discoveries.Count - 1;
                i >= 0;
                i--)
            {
                if (TryAwardDiscovery(
                    player,
                    pending.Discoveries[i]))
                {
                    pending.Discoveries.RemoveAt(i);
                }
            }

            if (pending.Experience <= 0f
                && pending.Discoveries.Count == 0)
            {
                pendingAwards.Remove(
                    player.PlayerUID);
            }
        }

        private static void LoadPendingAwards()
        {
            pendingAwards =
                new Dictionary<
                    string,
                    PendingPlayerAwards>();

            try
            {
                byte[] data =
                    sapi.WorldManager.SaveGame.GetData(
                        PendingAwardsSaveKey);

                if (data == null || data.Length == 0)
                {
                    return;
                }

                Dictionary<
                    string,
                    PendingPlayerAwards> loaded =
                    JsonSerializer.Deserialize<
                        Dictionary<
                            string,
                            PendingPlayerAwards>>(data);

                if (loaded != null)
                {
                    pendingAwards = loaded;
                }
            }
            catch (Exception ex)
            {
                sapi.Logger.Warning(
                    "[Ghaelen's Tweaks] Could not load pending "
                    + "Fermentation XP awards: {0}",
                    ex.Message);
            }
        }

        private static void SavePendingAwards()
        {
            if (sapi == null)
            {
                return;
            }

            try
            {
                byte[] data =
                    JsonSerializer.SerializeToUtf8Bytes(
                        pendingAwards);

                sapi.WorldManager.SaveGame.StoreData(
                    PendingAwardsSaveKey,
                    data);
            }
            catch (Exception ex)
            {
                sapi.Logger.Warning(
                    "[Ghaelen's Tweaks] Could not save pending "
                    + "Fermentation XP awards: {0}",
                    ex.Message);
            }
        }

        private static void ClearBarrelState(
            BarrelState state)
        {
            state.Active = false;
            state.BrewerUid = null;
            state.RecipeCode = null;
            state.StartLitres = 0f;
        }

        private sealed class BarrelState
        {
            public bool Active;
            public string BrewerUid;
            public string RecipeCode;
            public float StartLitres;
        }

        private sealed class PressState
        {
            public string PlayerUid;
            public float PendingExperience;
        }

        private sealed class BoilerState
        {
            public string PlayerUid;
        }

        private struct BarrelTickState
        {
            public bool WasSealed;
            public string RecipeCode;
        }

        public sealed class PendingPlayerAwards
        {
            public float Experience { get; set; }

            public List<string> Discoveries { get; set; } =
                new List<string>();
        }
    }
}
