/*
 * Applies the narrow client and server Harmony hooks required by the
 * parental-controls respawn delay.
 *
 * The server hook rejects respawn requests before vanilla consumes a return
 * point or begins teleporting.  The client hook updates the existing death
 * dialog rather than replacing it, preserving vanilla death, revival, and
 * limited-life behavior outside the temporary countdown override.
 */

using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GhaelenTweaks;

internal static class ParentalControlDeathDelayPatches
{
	private const string ClientTargetTypeName = "Vintagestory.Client.NoObf.GuiDialogDead";
	private const string ClientTargetMethodName = "OnGameTick";
	private const string ServerTargetTypeName = "Vintagestory.Server.ServerSystemEntitySimulation";
	private const string ServerTargetMethodName = "OnPlayerRespawn";
	private static FieldInfo? clientRespawningField;
	private static WeakReference<GuiElementDynamicText>? lastCountdownElement;
	private static string originalCountdownText = string.Empty;
	private static bool countdownOverridden;



	//// Applies the server prefix that authoritatively gates respawn requests.
	////
	//// Target lookup is reflective because the vanilla server implementation
	//// is not part of the public API assembly referenced by the mod project.
	//// A missing target logs a warning instead of preventing mod startup.
	////
	internal static void ApplyServer(Harmony harmony, ILogger logger)
	{
		ApplyPatch(
			harmony,
			logger,
			ServerTargetTypeName,
			ServerTargetMethodName,
			new[] { typeof(IServerPlayer) },
			nameof(PrefixServerPlayerRespawn),
			isPrefix: true);
	}



	//// Applies the client postfix that displays and disables the countdown.
	////
	//// The patch reuses the vanilla death dialog's dynamic countdown line and
	//// restores its original revival text when the parental delay expires.
	////
	internal static void ApplyClient(Harmony harmony, ILogger logger)
	{
		ApplyPatch(
			harmony,
			logger,
			ClientTargetTypeName,
			ClientTargetMethodName,
			new[] { typeof(float) },
			nameof(PostfixClientDeathDialogTick),
			isPrefix: false);
	}



	//// Clears client GUI references retained by the countdown presentation.
	////
	internal static void Dispose()
	{
		lastCountdownElement = null;
		originalCountdownText = string.Empty;
		countdownOverridden = false;
		clientRespawningField = null;
	}



	//// Blocks an early server respawn while allowing vanilla when time is up.
	////
	private static bool PrefixServerPlayerRespawn(IServerPlayer player)
	{
		return ParentalControlDeathDelaySystem.IsServerRespawnAllowed(player);
	}



	//// Updates the existing death dialog after each vanilla quarter-second tick.
	////
	//// Vanilla still owns opening, composing, revival timing, limited lives,
	//// respawn dispatch, and closing.  This postfix changes only the countdown
	//// line and button enabled state while the synced delay remains positive.
	////
	private static void PostfixClientDeathDialogTick(object __instance)
	{
		if (__instance is not GuiDialog dialog)
		{
			return;
		}

		if (!ParentalControlDeathDelaySystem.IsClientPlayerDead())
		{
			ParentalControlDeathDelaySystem.ClearClientDelay();
			RestoreCountdownText();
			return;
		}

		GuiComposer? composer = dialog.Composers["menu"];
		if (composer == null)
		{
			return;
		}

		GuiElementTextButton? respawnButton = composer.GetButton("respawnbtn");
		GuiElementDynamicText? countdownElement = composer.GetDynamicText("reviveCountdown");
		long remainingMilliseconds = ParentalControlDeathDelaySystem.GetClientRemainingMilliseconds();

		if (remainingMilliseconds > 0)
		{
			CaptureCountdownText(countdownElement);
			long remainingSeconds = 1 + ((remainingMilliseconds - 1) / 1000);
			countdownElement?.SetNewText(
				Lang.Get("ghaelentweaks:pc-death-delay-countdown", remainingSeconds));

			if (respawnButton != null)
			{
				respawnButton.Enabled = false;
			}

			return;
		}

		RestoreCountdownText();

		if (respawnButton != null)
		{
			clientRespawningField ??= AccessTools.Field(__instance.GetType(), "respawning");
			bool respawning = clientRespawningField?.GetValue(__instance) is true;
			respawnButton.Enabled = !respawning;
		}
	}



	//// Captures the vanilla countdown element and its text before overriding it.
	////
	//// The dialog can recompose and replace its GUI elements, so a changed
	//// element reference starts a fresh capture rather than restoring text
	//// into an element that is no longer displayed.
	////
	private static void CaptureCountdownText(GuiElementDynamicText? countdownElement)
	{
		if (countdownElement == null)
		{
			return;
		}

		if (lastCountdownElement == null
			|| !lastCountdownElement.TryGetTarget(out GuiElementDynamicText? previousElement)
			|| !ReferenceEquals(previousElement, countdownElement))
		{
			lastCountdownElement = new WeakReference<GuiElementDynamicText>(countdownElement);
			originalCountdownText = countdownElement.GetText();
		}

		countdownOverridden = true;
	}



	//// Restores the vanilla revival countdown after the custom delay expires.
	////
	private static void RestoreCountdownText()
	{
		if (countdownOverridden
			&& lastCountdownElement != null
			&& lastCountdownElement.TryGetTarget(out GuiElementDynamicText? countdownElement))
		{
			countdownElement.SetNewText(originalCountdownText);
		}

		lastCountdownElement = null;
		originalCountdownText = string.Empty;
		countdownOverridden = false;
	}



	//// Resolves and applies one reflective Harmony prefix or postfix.
	////
	//// Both sides use the same defensive lookup and warning behavior.  The
	//// supplied target signature prevents an unrelated overload from being
	//// patched after a future game update.
	////
	private static void ApplyPatch(
		Harmony harmony,
		ILogger logger,
		string targetTypeName,
		string targetMethodName,
		Type[] targetParameterTypes,
		string patchMethodName,
		bool isPrefix)
	{
		try
		{
			Type? targetType = AccessTools.TypeByName(targetTypeName);
			MethodInfo? targetMethod = targetType == null
				? null
				: AccessTools.Method(targetType, targetMethodName, targetParameterTypes);
			MethodInfo? patchMethod = AccessTools.Method(typeof(ParentalControlDeathDelayPatches), patchMethodName);

			if (targetMethod == null || patchMethod == null)
			{
				logger.Warning(
					$"Parental-controls death-delay patch target was not found: {targetTypeName}.{targetMethodName}.");
				return;
			}

			HarmonyMethod harmonyMethod = new(patchMethod);
			if (isPrefix)
			{
				harmony.Patch(targetMethod, prefix: harmonyMethod);
			}
			else
			{
				harmony.Patch(targetMethod, postfix: harmonyMethod);
			}

			logger.Notification(
				$"Parental-controls death-delay {targetMethodName} patch initialized.");
		}
		catch (Exception exception)
		{
			logger.Warning(
				$"Parental-controls death-delay patch could not patch {targetTypeName}.{targetMethodName}: "
				+ exception.Message);
		}
	}
}
