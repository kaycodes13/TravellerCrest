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
	static float DAMAGE_SCALING => Inst.toolDamageMultiplier.Value;

	static float CurrentBonus() {
		int missing = PlayerData.instance.maxHealth - PlayerData.instance.health;
		return 1 + DAMAGE_SCALING * Mathf.Sqrt(missing / 10f);
	}

	static int ApplyBonusToDamage(int damage)
		=> Mathf.FloorToInt(damage * CurrentBonus());

	static bool RepsNonSkillTool(HitInstance hit)
		=> hit.RepresentingTool && hit.RepresentingTool.Type != ToolItemType.Skill;


	[HarmonyPatch(typeof(DamageEnemies), "DoDamage", [typeof(GameObject), typeof(bool)])]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> MultiplyToolMultipliers(
		IEnumerable<CodeInstruction> instructions
	) {
		return new CodeMatcher(instructions)
			// Flintslate and Pollip Pouch
			.Start()
			.MatchEndForward([
				new(x => Call(x, $"get_{nameof(DamageEnemies.NailImbuement)}")),
				new(x => Ldfld(x, nameof(NailImbuementConfig.NailDamageMultiplier))),
				new(x => Callvirt(x, nameof(DamageStack.AddMultiplier))),
			])
			.Insert(BonusDamageInstructions())

			// Barbed Bracelet
			.Start()
			.MatchEndForward([
				new(x => Call(x, $"get_{nameof(Gameplay.BarbedWireDamageDealtMultiplier)}")),
				new(x => Callvirt(x, nameof(DamageStack.AddMultiplier))),
			])
			.Insert(BonusDamageInstructions())

			// Volt Filament
			.Start()
			.MatchEndForward([
				new(x => Call(x, $"get_{nameof(Gameplay.ZapDamageMult)}")),
				new(x => Callvirt(x, nameof(DamageStack.AddMultiplier))),
			])
			.Insert(BonusDamageInstructions())

			.InstructionEnumeration();

		static CodeInstruction[] BonusDamageInstructions()
			=> [new(OpCodes.Ldarg_0), Transpilers.EmitDelegate(ApplyBonus)];

		static float ApplyBonus(float multiplier, DamageEnemies self)
			=> (SifCrest.IsEquipped && (self.isHeroDamage || self.sourceIsHero)) 
				? multiplier * CurrentBonus()
				: multiplier;
	}

	[HarmonyPatch(typeof(HealthManager), "TakeDamage")]
	[HarmonyPrefix]
	static void AffectNormalDamage(ref HitInstance hitInstance) {
		if (SifCrest.IsEquipped && hitInstance.IsHeroDamage && RepsNonSkillTool(hitInstance))
			hitInstance.DamageDealt = ApplyBonusToDamage(hitInstance.DamageDealt);
	}

	[HarmonyPatch(typeof(HealthManager), "LagHits")]
	[HarmonyPrefix]
	static void AffectLagHits(ref LagHitOptions options, ref HitInstance hitInstance) {
		if (!SifCrest.IsEquipped || !hitInstance.IsHeroDamage)
			return;

		if (
			RepsNonSkillTool(hitInstance)
			|| ( // is status damage (which we assume is from a tool)
				options.DamageType != LagHitDamageType.None
				|| hitInstance.ToolDamageFlags.HasFlag(ToolDamageFlags.Searing) // flintslate
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

	[HarmonyPatch(typeof(HealthManager), "ApplyTagDamage")]
	[HarmonyPrefix]
	static void AffectTagDamage(ref DamageTag.DamageTagInstance damageTagInstance) {
		// DamageTagInstance.isHeroDamage is based on whether or not the source is a tool
		if (SifCrest.IsEquipped && damageTagInstance.isHeroDamage)
			damageTagInstance.amount = ApplyBonusToDamage(damageTagInstance.amount);
	}

}
