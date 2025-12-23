using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace TravellerCrest.Patches;

[HarmonyPatch(typeof(CountCrestUnlockPoints), nameof(CountCrestUnlockPoints.OnEnter))]
internal static class EvaOptOut {
	private static void Prefix(CountCrestUnlockPoints __instance) {
		ToolCrestList list = ScriptableObject.CreateInstance<ToolCrestList>();

		foreach (var crest in (ToolCrestList)__instance.CrestList.Value) {
			if (crest.name != SifCrest.name)
				list.Add(crest);
		}

		__instance.CrestList.Value = list;
	}
}
