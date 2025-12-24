using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Needleforge;
using Needleforge.Data;
using TeamCherry.Localization;
using TravellerCrest.Mechanics;
using UnityEngine;

namespace TravellerCrest;

/*

TODO:

- down attack (likely FSM)
- dash attack (likely FSM)
- charged attack (likely FSM)
- possibly a complex wall attack - designer still deciding
- config menu (possibly with debug options to tweak heroconfig, etc)
- playtesting, tweaking math

TO RESEARCH:

- how to make a silk skill

*/

[BepInAutoPlugin(id: "io.github.kaycodes13.travellercrest")]
[BepInDependency("org.silksong-modding.i18n")]
[BepInDependency("org.silksong-modding.fsmutil")]
[BepInDependency("org.silksong-modding.unityhelper")]
[BepInDependency("io.github.needleforge")]
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

		SifCrest.AddRedSlot(AttackToolBinding.Up, new(0, 2), false);
		SifCrest.AddSkillSlot(AttackToolBinding.Neutral, new(0, 0), false);
		SifCrest.AddToolSlot(Purple.Type, AttackToolBinding.Down, new(0, -2), false);

		SifCrest.AddBlueSlot(new(-2, 1.5f), false);
		SifCrest.AddBlueSlot(new(-2, -1.5f), false);
		SifCrest.AddYellowSlot(new(2, 1.5f), false);
		SifCrest.AddYellowSlot(new(2, -1.5f), false);

		SifCrest.ApplyAutoSlotNavigation();

		#endregion

		SifCrest.BindEvent = Bind.OnBindStart;

		Moveset.Setup();

		Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}
}
