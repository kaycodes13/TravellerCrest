using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Needleforge;
using Needleforge.Data;
using System.Reflection;
using TeamCherry.Localization;
using TravellerCrest.Mechanics;
using UnityEngine;
using static Silksong.UnityHelper.Util.SpriteUtil;

namespace TravellerCrest;

/*

TODO:

- possibly a complex wall attack - designer still deciding
- config menu (possibly with debug options to tweak heroconfig, etc)
- bind anim should be flea brew drinking anim, disable silk effects if possible
- playtesting, tweaking math

TO RESEARCH:

- how to make a silk skill

*/

[BepInAutoPlugin(id: "io.github.kaycodes13.travellercrest")]
[BepInDependency("org.silksong-modding.i18n")]
[BepInDependency("org.silksong-modding.fsmutil")]
[BepInDependency("org.silksong-modding.unityhelper")]
[BepInDependency("io.github.needleforge")]
[BepInIncompatibility("com.cometcake575.architect")]
public partial class TravellerCrestPlugin : BaseUnityPlugin {

	private Harmony Harmony { get; } = new(Id);
	internal static ManualLogSource Log { get; private set; }

	internal const string
		SifId = "Traveller";
	internal static readonly LocalisedString
		SifName = new($"Mods.{Id}", "CREST_NAME"),
		SifDesc = new($"Mods.{Id}", "CREST_DESC");

	internal static readonly CrestData
		SifCrest = NeedleforgePlugin.AddCrest(SifId, SifName, SifDesc);

	private static readonly Color
		PurpleColour = new Color32(186, 131, 241, 255);
	internal static readonly ColorData
		Purple = NeedleforgePlugin.AddToolColor("Attack/Defend", PurpleColour, isAttackType: true);

	private void Awake() {
		Log = Logger;
		Logger.LogInfo($"Plugin {Name} ({Id}) is loading...");

		Harmony.PatchAll();

		#region Custom Tool Colour

		Purple.AddValidTypes(ToolItemType.Red, ToolItemType.Blue);
		// TODO: slot sprite, possibly header sprite

		#endregion

		#region Tool Slots

		Assembly asm = Assembly.GetExecutingAssembly();
		const string path = $"{nameof(TravellerCrest)}.Assets.Sprites";

		SifCrest.RealSprite = LoadEmbeddedSprite(asm, $"{path}.crest_lines.png", pixelsPerUnit: 100);
		SifCrest.Silhouette = LoadEmbeddedSprite(asm, $"{path}.crest_silhouette.png", pixelsPerUnit: 200);
		SifCrest.CrestGlow = LoadEmbeddedSprite(asm, $"{path}.crest_glow.png", pixelsPerUnit: 284);

		SifCrest.AddRedSlot(AttackToolBinding.Up, new(-0.13f, 2.59f), false);
		SifCrest.AddSkillSlot(AttackToolBinding.Neutral, new(0.15f, -0.32f), false);
		SifCrest.AddToolSlot(Purple.Type, AttackToolBinding.Down, new(-0.15f, -2.18f), false);

		SifCrest.AddBlueSlot(new(-2.25f, -1.59f), false);
		SifCrest.AddBlueSlot(new(1.71f, -2.5f), false);
		SifCrest.AddYellowSlot(new(-1.41f, 0.55f), false);
		SifCrest.AddYellowSlot(new(2.2f, -0.12f), false);

		SifCrest.ApplyAutoSlotNavigation();

		#endregion

		SifCrest.BindEvent = Bind.OnBindStart;

		Moveset.Setup();

		Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}

}
