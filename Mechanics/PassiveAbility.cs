using GlobalSettings;
using HarmonyLib;
using System.Linq;
using TravellerCrest.Data;
using UnityEngine;
using static TravellerCrest.TravellerCrestPlugin;
using ProbabilityInt = Probability.ProbabilityInt;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class PassiveAbility {

	#region Bonus tool damage at low health

	private const float DAMAGE_SCALING = 1.06f;

	private static float ToolDamageBonus() {
		int masksMissing = PlayerData.instance.maxHealth - PlayerData.instance.health;
		return 1 + DAMAGE_SCALING * Mathf.Sqrt(masksMissing / 10f);
	}

	private static int ApplyBonusToDamage(int damage)
		=> Mathf.FloorToInt(damage * ToolDamageBonus());


	#region Tools with damage multipliers

	[HarmonyPatch(typeof(Gameplay), nameof(Gameplay.BarbedWireDamageDealtMultiplier), MethodType.Getter)]
	[HarmonyPostfix]
	private static void MultiplyBarbedWireMult(ref float __result) {
		if (SifCrest.IsEquipped)
			__result *= ToolDamageMultiplier();
	}

	[HarmonyPatch(typeof(Gameplay), nameof(Gameplay.ZapDamageMult), MethodType.Getter)]
	[HarmonyPostfix]
	private static void MultiplyVoltFilamentMult(ref float __result) {
		if (SifCrest.IsEquipped)
			__result *= ToolDamageMultiplier();
	}

	[HarmonyPatch(typeof(DamageEnemies), nameof(DamageEnemies.NailImbuement), MethodType.Getter)]
	[HarmonyPostfix]
	private static void MultiplyFlintslateMult(ref NailImbuementConfig __result) {
		if (SifCrest.IsEquipped && __result) {
			var newres = Object.Instantiate(__result);
			newres.NailDamageMultiplier *= ToolDamageMultiplier();
			__result = newres;
		}
	}

	#endregion

	#region Statuses & Tools which deal damage

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.TakeDamage))]
	[HarmonyPrefix]
	private static void BonusToolDamage(ref HitInstance hitInstance) {
		if (!SifCrest.IsEquipped || !hitInstance.IsHeroDamage)
			return;

		if ( // is a non-skill tool
			hitInstance.RepresentingTool
			&& hitInstance.RepresentingTool.Type != ToolItemType.Skill
		) {
			hitInstance.DamageDealt = ApplyBonusToDamage(hitInstance.DamageDealt);
		}
	}

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.LagHits))]
	[HarmonyPrefix]
	private static void BonusToolDamageLagHits(ref LagHitOptions options, ref HitInstance hitInstance) {
		if (!SifCrest.IsEquipped || !hitInstance.IsHeroDamage)
			return;

		if (
			(// is a non-skill tool
				hitInstance.RepresentingTool
				&& hitInstance.RepresentingTool.Type != ToolItemType.Skill
			)
			||
			(// is status damage (which we assume is from a tool)
				options.DamageType != LagHitDamageType.None
				|| hitInstance.ToolDamageFlags.HasFlag(ToolDamageFlags.Searing) // just in case, for flintslate
			)
		) {
			var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			var type = options.GetType();
			var newOps = (LagHitOptions)RuntimeHelpers.GetUninitializedObject(type);

			foreach(FieldInfo fi in type.GetAllFields(flags))
				fi.SetValue(newOps, fi.GetValue(options));

			newOps.HitDamage = ApplyBonusToDamage(options.HitDamage);

			options = newOps;
		}
	}

	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.ApplyTagDamage))]
	[HarmonyPrefix]
	private static void BonusToolDamageTag(ref DamageTag.DamageTagInstance damageTagInstance) {
		// DamageTagInstance.isHeroDamage is based on whether or not the source is a tool
		if (!SifCrest.IsEquipped || !damageTagInstance.isHeroDamage)
			return;

		damageTagInstance.amount = ApplyBonusToDamage(damageTagInstance.amount);
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
		if (!SifCrest.IsEquipped || !origin.lastHitInstance.IsHeroDamage)
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
