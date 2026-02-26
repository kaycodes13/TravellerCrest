using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Needleforge;
using Needleforge.Data;
using TeamCherry.Localization;
using TravellerCrest.Mechanics;
using TravellerCrest.Utils;
using UnityEngine;

namespace TravellerCrest;

/*

TODO:

- possibly a complex wall attack - designer still deciding
- config menu (possibly with debug options to tweak heroconfig, etc)
- playtesting, tweaking math

TO RESEARCH:

- how to make a silk skill

*/

[BepInAutoPlugin(id: "io.github.kaycodes13.travellercrest")]
[BepInDependency("org.silksong-modding.i18n", "1.0.2")]
[BepInDependency("org.silksong-modding.fsmutil", "0.3.12")]
[BepInDependency("org.silksong-modding.unityhelper", "1.1.1")]
[BepInDependency("io.github.needleforge", "0.8.1")]
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

		Harmony.PatchAll();

		#region Custom Tool Colour

		Purple.AddValidTypes(ToolItemType.Red, ToolItemType.Blue);
		// TODO: slot sprite, possibly header sprite

		#endregion

		#region Tool Slots

		const string path = $"{nameof(TravellerCrest)}.Assets.Sprites";

		SifCrest.RealSprite = AssetUtil.LoadSprite($"{path}.crest_lines.png", ppu: 100);
		SifCrest.Silhouette = AssetUtil.LoadSprite($"{path}.crest_silhouette.png", ppu: 200);
		SifCrest.CrestGlow = AssetUtil.LoadSprite($"{path}.crest_glow.png", ppu: 284);

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

		HUD.Setup();
		Moveset.Setup();

		Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}

}
