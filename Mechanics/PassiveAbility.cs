using GlobalSettings;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using ProbabilityInt = Probability.ProbabilityInt;
using static TravellerCrest.TravellerCrestPlugin;
using TravellerCrest.Data;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class PassiveAbility {

	#region Bonus tool damage at low health

	private const float DAMAGE_SCALING = 1.06f;

	private static float ToolDamageMultiplier() {
		int masksMissing = PlayerData.instance.maxHealth - PlayerData.instance.health;
		return 1 + DAMAGE_SCALING * Mathf.Sqrt(masksMissing / 10f);
	}

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.ApplyDamageScaling))]
	[HarmonyPostfix]
	private static void ApplyBonusToolDamage(ref HitInstance __result) {
		if (!SifCrest.IsEquipped || !__result.RepresentingTool)
			return;

		__result.DamageDealt = Mathf.FloorToInt(__result.DamageDealt * ToolDamageMultiplier());
	}

	#endregion

	#region Enemies sometimes drop tool refunds

	private const float REFUND_NORMAL_DROP_RATE = 0.10f;
	private const float REFUND_SNITCH_DROP_RATE = 0.10f;
	private const float REFUND_DICE_MULT = 0.10f;
	private const int REFUND_AMOUNT = 1;

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Awake))]
	[HarmonyPostfix]
	private static void EnemyDeathToolRefund(HealthManager __instance) {
		__instance.OnDeath += () => SpawnRefundItem(REFUND_NORMAL_DROP_RATE, __instance);
	}

	[HarmonyPatch(typeof(HealthManager.StealLagHit), nameof(HealthManager.StealLagHit.OnEnd))]
	[HarmonyPostfix]
	private static void SnitchPickToolRefund(HealthManager.StealLagHit __instance) {
		SpawnRefundItem(REFUND_SNITCH_DROP_RATE, __instance.healthManager);
	}

	private static void SpawnRefundItem(float baseDropRate, HealthManager origin) {
		if (!SifCrest.IsEquipped)
			return;

		int amount = GetRefundAmount(baseDropRate);
		if (amount <= 0)
			return;

		ToolItem[] attackTools = [
			.. ToolItemManager.GetCurrentEquippedTools().Where(x => x && x.IsAttackType())
		];
		if (attackTools.Length <= 0)
			return;

		RefundItem refund = ScriptableObject.CreateInstance<RefundItem>();
		refund.tool = attackTools.GetRandomElement();
		refund.amountRefunded = amount;

		Vector3 spawnPoint = origin.transform.TransformPoint(origin.effectOrigin);

		GameObject item = ObjectPool.Spawn(
			prefab: Gameplay.CollectableItemPickupInstantPrefab.gameObject,
			parent: null,
			position: spawnPoint,
			rotation: Quaternion.identity,
			stealActiveSpawned: false
		);
		var itemOptions = item.GetComponent<CollectableItemPickup>();
		itemOptions.SetItem(refund);

		FlingUtils.FlingObject(new FlingUtils.SelfConfig {
			Object = item,
			SpeedMin = 15f,
			SpeedMax = 30f,
			AngleMin = 80f,
			AngleMax = 100f,
		}, origin.transform, origin.effectOrigin);
	}

	private static int GetRefundAmount(float baseDropRate) {
		float bonus = 1;
		if (Gameplay.LuckyDiceTool.IsEquipped)
			bonus += REFUND_DICE_MULT;

		float dropRate = baseDropRate * bonus;
		Log.LogInfo($"BASE {baseDropRate} | BONUS {bonus} | FINAL {dropRate}");

		ProbabilityInt[] dropAmounts = [
			new() {
				Value = 0,
				Probability = 1 - dropRate
			},
			new() {
				Value = REFUND_AMOUNT,
				Probability = dropRate
			},
		];

		return Probability.GetRandomItemByProbability<ProbabilityInt, int>(dropAmounts);
	}

	#endregion

}
