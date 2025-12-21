using GlobalSettings;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TravellerCrest.Data;
using UnityEngine;
using ProbabilityInt = Probability.ProbabilityInt;
using static TravellerCrest.TravellerCrestPlugin;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class EnemiesDropToolRefills {

	private const float DROP_RATE_NORMAL = 0.10f;
	private const float DROP_RATE_SNITCH_PICK = 0.10f;
	private const float DROP_RATE_DICE_BONUS = 0.10f;

	private const int REFILL_AMOUNT = 1;

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Awake))]
	[HarmonyPostfix]
	private static void EnemyDeathDrop(HealthManager __instance) {
		__instance.OnDeath += () => SpawnRefillItems(DROP_RATE_NORMAL, __instance);
	}

	[HarmonyPatch(typeof(HealthManager.StealLagHit), nameof(HealthManager.StealLagHit.OnEnd))]
	[HarmonyPostfix]
	private static void SnitchPickDrop(HealthManager.StealLagHit __instance) {
		SpawnRefillItems(DROP_RATE_SNITCH_PICK, __instance.healthManager);
	}

	private static int GetAmountRefilled(float dropRate) {
		return Probability.GetRandomItemByProbability<ProbabilityInt, int>([
			new() {
				Value = 0,
				Probability = 1 - dropRate
			},
			new() {
				Value = REFILL_AMOUNT,
				Probability = dropRate
			},
		]);
	}

	private static void SpawnRefillItems(float baseDropRate, HealthManager origin) {
		if (!SifCrest.IsEquipped || !origin.lastHitInstance.IsHeroDamage)
			return;

		IEnumerable<ToolItem> eligibleTools =
			ToolItemManager.GetCurrentEquippedTools()
			.Where(x => x && x.IsAttackType() && x.HasLimitedUses());

		float bonus = 1;
		if (Gameplay.LuckyDiceTool.IsEquipped)
			bonus += DROP_RATE_DICE_BONUS;

		foreach (ToolItem tool in eligibleTools) {
			int max = ToolItemManager.GetToolStorageAmount(tool),
				remaining = PlayerData.instance.Tools.GetData(tool.name).AmountLeft;
			float missingPercent = (float)(max - remaining) / max;

			Log.LogInfo(
				$"tool: {tool.name} | missing: {missingPercent:#0%} | " +
				$"drop rate: {baseDropRate * missingPercent} | " +
				$"with bonus ({bonus}): {baseDropRate * missingPercent * bonus}"
			);

			int amount = GetAmountRefilled(baseDropRate * missingPercent * bonus);
			if (amount <= 0)
				continue;

			RefillItem refill = ScriptableObject.CreateInstance<RefillItem>();
			refill.tool = tool;
			refill.amountRefunded = amount;

			Vector3 spawnPoint = origin.transform.TransformPoint(origin.effectOrigin);

			GameObject item = ObjectPool.Spawn(
				prefab: Gameplay.CollectableItemPickupInstantPrefab.gameObject,
				parent: null,
				position: spawnPoint,
				rotation: Quaternion.identity
			);
			item.GetComponent<CollectableItemPickup>().SetItem(refill);

			FlingUtils.FlingObject(new FlingUtils.SelfConfig {
				Object = item,
				SpeedMin = 15f,
				SpeedMax = 30f,
				AngleMin = 80f,
				AngleMax = 100f,
			}, origin.transform, origin.effectOrigin);
		}
	}

}
