using GlobalSettings;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TravellerCrest.Data;
using UnityEngine;
using static TravellerCrest.TravellerCrestPlugin;
using static TravellerCrest.Utils.ILUtils;
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

	[HarmonyPatch(typeof(DamageEnemies), nameof(DamageEnemies.DoDamage), [typeof(GameObject), typeof(bool)])]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> MultiplyToolMultipliers(
		IEnumerable<CodeInstruction> instructions
	) {
		return new CodeMatcher(instructions)
			#region Flintslate and Pollip Pouch
			.Start()
			.MatchEndForward([
				new(x => CallRelaxed(x, $"get_{nameof(DamageEnemies.NailImbuement)}")),
				new(x => Ldfld(x, nameof(NailImbuementConfig.NailDamageMultiplier))),
				new(x => CallRelaxed(x, nameof(DamageStack.AddMultiplier))),
			])
			.Insert([
				new(OpCodes.Ldarg_0),
				Transpilers.EmitDelegate(ApplyBonus),
			])
			#endregion
			#region Barbed Bracelet
			.Start()
			.MatchEndForward([
				new(x => CallRelaxed(x, $"get_{nameof(Gameplay.BarbedWireDamageDealtMultiplier)}")),
				new(x => CallRelaxed(x, nameof(DamageStack.AddMultiplier))),
			])
			.Insert([
				new(OpCodes.Ldarg_0),
				Transpilers.EmitDelegate(ApplyBonus),
			])
			#endregion
			#region Volt Filament
			.Start()
			.MatchEndForward([
				new(x => CallRelaxed(x, $"get_{nameof(Gameplay.ZapDamageMult)}")),
				new(x => CallRelaxed(x, nameof(DamageStack.AddMultiplier))),
			])
			.Insert([
				new(OpCodes.Ldarg_0),
				Transpilers.EmitDelegate(ApplyBonus),
			])
			#endregion
			.InstructionEnumeration();

		static float ApplyBonus(float multiplier, DamageEnemies instance) {
			if (SifCrest.IsEquipped && (instance.isHeroDamage || instance.sourceIsHero)) 
				return multiplier * ToolDamageBonus();
			return multiplier;
		}
	}

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
		__instance.OnDeath += () => SpawnRefundItems(REFUND_NORMAL_DROP_RATE, __instance);
	}

	[HarmonyPatch(typeof(HealthManager.StealLagHit), nameof(HealthManager.StealLagHit.OnEnd))]
	[HarmonyPostfix]
	private static void SnitchPickToolRefund(HealthManager.StealLagHit __instance) {
		SpawnRefundItems(REFUND_SNITCH_DROP_RATE, __instance.healthManager);
	}

	private static void SpawnRefundItems(float baseDropRate, HealthManager origin) {
		if (!SifCrest.IsEquipped || !origin.lastHitInstance.IsHeroDamage)
			return;

		IEnumerable<ToolItem> eligibleTools = 
			ToolItemManager.GetCurrentEquippedTools()
			.Where(x => x && x.IsAttackType() && x.HasLimitedUses());

		float bonus = 1;
		if (Gameplay.LuckyDiceTool.IsEquipped)
			bonus += REFUND_DICE_MULT;

		foreach (ToolItem tool in eligibleTools) {
			int max = ToolItemManager.GetToolStorageAmount(tool),
				remaining = PlayerData.instance.Tools.GetData(tool.name).AmountLeft;
			float missingPercent = (float)(max - remaining) / max;

			Log.LogInfo($"yonder tool is {tool.name} with {missingPercent:#0%} missing uses resulting in a base drop rate of {baseDropRate * missingPercent}, then the bonus of {bonus} makes it {baseDropRate * bonus * missingPercent}");

			int amount = GetRefundAmount(baseDropRate * bonus * missingPercent);
			if (amount <= 0)
				continue;

			RefundItem refund = ScriptableObject.CreateInstance<RefundItem>();
			refund.tool = tool;
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
	}

	private static int GetRefundAmount(float dropRate) {
		return Probability.GetRandomItemByProbability<ProbabilityInt, int>([
			new() {
				Value = 0,
				Probability = 1 - dropRate
			},
			new() {
				Value = REFUND_AMOUNT,
				Probability = dropRate
			},
		]);
	}

	#endregion

}
