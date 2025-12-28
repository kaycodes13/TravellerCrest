using GlobalSettings;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TravellerCrest.Data;
using TravellerCrest.Utils;
using UnityEngine;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class EnemiesDropToolRefills {

	private const float DROP_RATE_NORMAL = 0.10f;
	private const float DROP_RATE_SNITCH_PICK = 0.10f;

	private const float DROP_RATE_DICE_BONUS = 1.10f;

	private const float REFILL_PERCENT = 0.10f;


	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Awake))]
	[HarmonyPostfix]
	private static void EnemyDeathDrop(HealthManager __instance) {
		if (__instance.GetComponent<PersistentBoolItem>())
			__instance.OnDeath += () => SpawnRefillItems(DROP_RATE_NORMAL, __instance);
	}

	[HarmonyPatch(typeof(HealthManager.StealLagHit), nameof(HealthManager.StealLagHit.OnEnd))]
	[HarmonyPostfix]
	private static void SnitchPickDrop(HealthManager.StealLagHit __instance) {
		if (__instance.healthManager.GetComponent<PersistentBoolItem>())
			SpawnRefillItems(DROP_RATE_SNITCH_PICK, __instance.healthManager);
	}

	private static int GetAmountRefilled(int toolCapacity) {
		float rawAmount = toolCapacity * REFILL_PERCENT;
		float chanceToCeil = rawAmount - System.MathF.Truncate(rawAmount);

		return ProbabilityUtils.GetRandomBool(chanceToCeil)
			? Mathf.CeilToInt(rawAmount)
			: Mathf.FloorToInt(rawAmount);
	}

	private static void SpawnRefillItems(float dropRate, HealthManager origin) {
		if (!SifCrest.IsEquipped || !origin.lastHitInstance.IsHeroDamage)
			return;

		if (Gameplay.LuckyDiceTool.IsEquipped)
			dropRate *= DROP_RATE_DICE_BONUS;

		IEnumerable<ToolItem> eligibleTools =
			ToolItemManager.GetCurrentEquippedTools()
			.Where(x => x && x.IsAttackType() && x.HasLimitedUses());

		foreach (ToolItem tool in eligibleTools) {
			int capacity = ToolItemManager.GetToolStorageAmount(tool),
				remaining = PlayerData.instance.Tools.GetData(tool.name).AmountLeft;
			float missingPercent = (float)(capacity - remaining) / capacity,
				scaledDropRate = dropRate * missingPercent;

			if (!ProbabilityUtils.GetRandomBool(scaledDropRate))
				continue;

			int amount = GetAmountRefilled(capacity);
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
