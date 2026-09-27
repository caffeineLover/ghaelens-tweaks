using System;
using System.Collections.Concurrent;
using System.Reflection;
using Vintagestory.API.Common;

namespace GhaelenTweaks.Compatibility.XSkills
{
    /// <summary>
    /// Reflection-only adapter for xSkills/xLib.
    ///
    /// Ghaelen's Tweaks must not reference xSkills or xLib at compile time.  The bridge
    /// discovers the player's xLib SkillSet behavior at runtime, finds the "brewing"
    /// skill used by xSkills for Fermentation, and invokes PlayerSkill.AddExperience.
    /// </summary>
    internal static class XSkillsBridge
    {
        private const string SkillSetBehaviorName = "SkillSet";
        private const string FermentationSkillName = "brewing";

        private static readonly ConcurrentDictionary<Type, MethodInfo> FindSkillMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        private static readonly ConcurrentDictionary<Type, MethodInfo> AddExperienceMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        private static ICoreAPI api;
        private static bool reflectionFailureLogged;

        /// <summary>
        /// Initializes logging and runtime detection.  No xSkills types are loaded here.
        /// </summary>
        internal static void Initialize(ICoreAPI coreApi)
        {
            api = coreApi;
        }

        /// <summary>
        /// Supports both the current xSkills Fork identifier and the older upstream name.
        /// </summary>
        internal static bool IsXSkillsLoaded(ICoreAPI coreApi)
        {
            if (coreApi == null)
            {
                return false;
            }

            return coreApi.ModLoader.IsModEnabled("xskillsfork")
                || coreApi.ModLoader.IsModEnabled("xskills");
        }

        /// <summary>
        /// Adds Fermentation XP through xSkills' own PlayerSkill.AddExperience method.
        /// Passing true for invokeModifiers preserves xSkills' normal global, class, and
        /// Master Brewmaster XP modifiers instead of bypassing its progression rules.
        /// </summary>
        internal static bool TryAddFermentationExperience(IPlayer player, float experience)
        {
            if (player?.Entity == null || experience <= 0f)
            {
                return false;
            }

            try
            {
                object skillSet = player.Entity.GetBehavior(SkillSetBehaviorName);
                if (skillSet == null)
                {
                    return false;
                }

                MethodInfo findSkill = GetFindSkillMethod(skillSet.GetType());

                if (findSkill == null)
                {
                    LogReflectionFailure(
                        "Could not find xLib PlayerSkillSet.FindSkill(string, bool).");
                    return false;
                }

                object playerSkill = findSkill.Invoke(
                    skillSet,
                    new object[] { FermentationSkillName, false });

                if (playerSkill == null)
                {
                    return false;
                }

                MethodInfo addExperience =
                    GetAddExperienceMethod(playerSkill.GetType());

                if (addExperience == null)
                {
                    LogReflectionFailure(
                        "Could not find xLib PlayerSkill.AddExperience(float, bool).");
                    return false;
                }

                addExperience.Invoke(playerSkill, new object[] { experience, true });
                return true;
            }
            catch (TargetInvocationException ex)
            {
                LogReflectionFailure(
                    "xSkills rejected a Fermentation XP award.",
                    ex.InnerException ?? ex);
                return false;
            }
            catch (Exception ex)
            {
                LogReflectionFailure(
                    "Could not award Fermentation XP through xSkills.",
                    ex);
                return false;
            }
        }


        private static MethodInfo GetFindSkillMethod(Type type)
        {
            if (FindSkillMethods.TryGetValue(type, out MethodInfo method))
            {
                return method;
            }

            method = ResolveFindSkillMethod(type);
            if (method != null)
            {
                FindSkillMethods.TryAdd(type, method);
            }

            return method;
        }

        private static MethodInfo GetAddExperienceMethod(Type type)
        {
            if (AddExperienceMethods.TryGetValue(type, out MethodInfo method))
            {
                return method;
            }

            method = ResolveAddExperienceMethod(type);
            if (method != null)
            {
                AddExperienceMethods.TryAdd(type, method);
            }

            return method;
        }

        private static MethodInfo ResolveFindSkillMethod(Type type)
        {
            return type.GetMethod(
                "FindSkill",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(string), typeof(bool) },
                modifiers: null);
        }

        private static MethodInfo ResolveAddExperienceMethod(Type type)
        {
            return type.GetMethod(
                "AddExperience",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(float), typeof(bool) },
                modifiers: null);
        }

        private static void LogReflectionFailure(string message, Exception ex = null)
        {
            if (reflectionFailureLogged)
            {
                return;
            }

            reflectionFailureLogged = true;

            if (ex == null)
            {
                api?.Logger.Warning(
                    "[Ghaelen's Tweaks] xSkills Fermentation bridge: {0}",
                    message);
            }
            else
            {
                api?.Logger.Warning(
                    "[Ghaelen's Tweaks] xSkills Fermentation bridge: {0} {1}",
                    message,
                    ex.Message);
            }
        }
    }
}
