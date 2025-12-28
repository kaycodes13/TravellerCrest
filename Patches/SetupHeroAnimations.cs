using HarmonyLib;
using TravellerCrest.Data;

namespace TravellerCrest.Patches;

[HarmonyPatch(typeof(HeroController), nameof(HeroController.Awake))]
internal static class SetupHeroAnimations {
	private static void Postfix(HeroController __instance)
		=> AnimationManager.SetupHeroAnims(__instance);
}
