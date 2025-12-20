using HarmonyLib;
using System.Linq;
using UnityEngine;
using static Probability;
using static TravellerCrest.TravellerCrestPlugin;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class PassiveAbility {

	private const float TOOL_DAMAGE_SCALING = 1.06f;

	private static float ToolDamageMultiplier() {
		int masksMissing = PlayerData.instance.maxHealth - PlayerData.instance.health;
		return 1 + TOOL_DAMAGE_SCALING * Mathf.Sqrt(masksMissing / 10f);
	}

	private static readonly ProbabilityInt[] ToolRefundAmounts = [
		new() {
			Value = 0,
			Probability = 0.75f
		},
		new() {
			Value = 1,
			Probability = 0.25f
		},
	];

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.ApplyDamageScaling))]
	[HarmonyPostfix]
	private static void ApplyBonusToolDamage(ref HitInstance __result) {
		if (!SifCrest.IsEquipped || !__result.RepresentingTool)
			return;

		__result.DamageDealt = Mathf.FloorToInt(__result.DamageDealt * ToolDamageMultiplier());
	}

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Awake))]
	[HarmonyPostfix]
	private static void AddRandomToolRefundChance(HealthManager __instance) {
		__instance.OnDeath += RefundToolSometimes;
	}

	private static void RefundToolSometimes() {
		if (!SifCrest.IsEquipped)
			return;

		var amount = GetRandomItemByProbability<ProbabilityInt, int>(ToolRefundAmounts);
		if (amount <= 0)
			return;

		ToolItem[] equippedAttackTools = [..
			PlayerData.instance.ToolEquips.GetData(SifName).Slots
			.Select(x => x.EquippedTool)
			.Where(x => !string.IsNullOrEmpty(x))
			.Select(x => ToolItemManager.GetToolByName(x))
			.Where(x => x.IsAttackType())
		];

		equippedAttackTools.GetRandomElement().CollectFree(amount);
	}
}
