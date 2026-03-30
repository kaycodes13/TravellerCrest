using GlobalSettings;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TeamCherry.SharedUtils;
using TravellerCrest.Data;
using TravellerCrest.Utils;
using UnityEngine;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class EnemiesDropToolRefills {

	private static float DROP_RATE_NORMAL => Inst.dropRateNormal.Value;
	private static float DROP_RATE_SNITCH_PICK => Inst.dropRateSnitch.Value;

	private static float DROP_RATE_DICE_BONUS => Inst.dropRateDiceBonus.Value;

	private static float REFILL_PERCENT => Inst.percentToolsRefilled.Value;


	[HarmonyPatch(typeof(HealthManager), "Awake")]
	[HarmonyPostfix]
	private static void EnemyDeathDrop(HealthManager __instance) {
		if (__instance.GetComponent<PersistentBoolItem>())
			__instance.OnDeath += () => SpawnRefillItems(DROP_RATE_NORMAL, __instance);
	}

	[HarmonyPatch(typeof(HealthManager.StealLagHit), "OnEnd")]
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
			int capacity = ToolItemManager.GetToolStorageAmount(tool);
			float remaining = PlayerData.instance.Tools.GetData(tool.name).AmountLeft,
				missingPercent = 1 - remaining/capacity,
				scaledDropRate = dropRate * missingPercent;

			if (!ProbabilityUtils.GetRandomBool(scaledDropRate))
				continue;

			int amount = GetAmountRefilled(capacity);
			if (amount <= 0)
				continue;

			var refill = ScriptableObject.CreateInstance<RefillItem>();
			refill.tool = tool;
			refill.amountRefunded = amount;

			GameObject item = ObjectPool.Spawn(
				prefab: Gameplay.CollectableItemPickupInstantPrefab.gameObject,
				parent: null,
				position: origin.transform.TransformPoint(origin.effectOrigin),
				rotation: Quaternion.identity
			);
			var pickup = item.GetComponent<CollectableItemPickup>();
			pickup.SetItem(refill);
			pickup.FlingSelf(speed: new(15, 30), angle: new(75, 105));
		}
	}

}
