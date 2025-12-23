using GlobalSettings;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using static TravellerCrest.Utils.ILUtils;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class LowHPToolDamageBonus {

	// 0.53 gives a maximum bonus of 1.5x at 9 masks missing - similar to hunter crest bonus
	private const float DAMAGE_SCALING = 0.53f;

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
	private static void AffectNormalDamage(ref HitInstance hitInstance) {
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
	private static void AffectLagHits(ref LagHitOptions options, ref HitInstance hitInstance) {
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
	private static void AffectTagDamage(ref DamageTag.DamageTagInstance damageTagInstance) {
		// DamageTagInstance.isHeroDamage is based on whether or not the source is a tool
		if (!SifCrest.IsEquipped || !damageTagInstance.isHeroDamage)
			return;

		damageTagInstance.amount = ApplyBonusToDamage(damageTagInstance.amount);
	}

}
