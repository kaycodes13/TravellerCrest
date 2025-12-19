using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Needleforge.Data;
using TeamCherry.Localization;

namespace TravellerCrest;

// TODO - adjust the plugin guid as needed
[BepInAutoPlugin(id: "io.github.kaycodes13.travellercrest")]
[BepInDependency("org.silksong-modding.i18n")]
[BepInDependency("org.silksong-modding.fsmutil")]
[BepInDependency("org.silksong-modding.unityhelper")]
[BepInDependency("io.github.needleforge")]
public partial class TravellerCrestPlugin : BaseUnityPlugin {

	private Harmony Harmony { get; } = new(Id);
	internal static ManualLogSource Log { get; private set; }

	internal static CrestData SifCrest { get; private set; }

	internal const string
		SifId = "Traveller";
	internal static readonly LocalisedString
		SifName = new($"Mods.{Id}", "CREST_NAME"),
		SifDesc = new($"Mods.{Id}", "CREST_DESC");

	private void Awake() {
		Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}
}
