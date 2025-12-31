using BepInEx;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using Needleforge;
using Needleforge.Data;
using Silksong.UnityHelper.Extensions;
using System.Collections;
using System.Reflection;
using TeamCherry.Localization;
using TravellerCrest.Components;
using TravellerCrest.Data;
using TravellerCrest.Mechanics;
using UnityEngine;
using UnityEngine.Rendering;
using static TravellerCrest.Utils.AssetUtil;

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

		Harmony.PatchAll();

		Assembly asm = Assembly.GetExecutingAssembly();
		const string path = $"{nameof(TravellerCrest)}.Assets.Sprites";

		#region Custom Tool Colour

		Purple.AddValidTypes(ToolItemType.Red, ToolItemType.Blue);
		// TODO: slot sprite, possibly header sprite

		#endregion

		#region Tool Slots

		SifCrest.RealSprite = LoadSprite($"{path}.crest_lines.png", ppu: 100);
		SifCrest.Silhouette = LoadSprite($"{path}.crest_silhouette.png", ppu: 200);
		SifCrest.CrestGlow = LoadSprite($"{path}.crest_glow.png", ppu: 284);

		SifCrest.AddRedSlot(AttackToolBinding.Up, new(-0.13f, 2.59f), false);
		SifCrest.AddSkillSlot(AttackToolBinding.Neutral, new(0.15f, -0.32f), false);
		SifCrest.AddToolSlot(Purple.Type, AttackToolBinding.Down, new(-0.15f, -2.18f), false);

		SifCrest.AddBlueSlot(new(-2.25f, -1.59f), false);
		SifCrest.AddBlueSlot(new(1.71f, -2.5f), false);
		SifCrest.AddYellowSlot(new(-1.41f, 0.55f), false);
		SifCrest.AddYellowSlot(new(2.2f, -0.12f), false);

		SifCrest.ApplyAutoSlotNavigation();

		#endregion

		#region HUD

		tk2dSpriteAnimation
			mainLib = AnimationManager.MainLib,
			steelLib = AnimationManager.libraries["steel"];
		HudFrameData
			hud = SifCrest.HudFrame;

		hud.ProfileIcon = LoadSprite($"{path}.hud_profile.png", ppu: 100);
		hud.SteelProfileIcon = LoadSprite($"{path}.hud_ss_profile.png", ppu: 100);

		hud.Appear = mainLib.GetClipByName("Traveller HUD Appear");
		hud.Idle = mainLib.GetClipByName("Traveller HUD Idle");
		hud.Disappear = mainLib.GetClipByName("Traveller HUD Disappear");

		hud.SteelAppear = steelLib.GetClipByName("Traveller HUD Appear");
		hud.SteelIdle = steelLib.GetClipByName("Traveller HUD Idle");
		hud.SteelDisappear = steelLib.GetClipByName("Traveller HUD Disappear");

		hud.OnRootCreated += HudRootSetup;

		void HudRootSetup() {
			var root = hud.Root!;

			var sg = root.GetOrAddComponent<SortingGroup>();
			sg.sortingLayerName = "Over";
			sg.sortingOrder = 0;

			GameObject
				meter = new("meter"),
				mask = new("mask");

			var meterRenderer = meter.AddComponent<SpriteRenderer>();
			meterRenderer.sprite = LoadSprite($"{path}.hud_meter.png");
			meterRenderer.sortingOrder = 0;
			meterRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
			meter.transform.SetParent(root.transform);
			meter.layer = (int)PhysLayers.UI;

			var maskRenderer = mask.AddComponent<SpriteMask>();
			maskRenderer.sprite = LoadSprite($"{path}.hud_meter_mask.png");
			maskRenderer.sortingOrder = 1;
			mask.transform.SetParent(root.transform);
			mask.transform.localScale = Vector3.zero;
			mask.layer = (int)PhysLayers.UI;

			HeroController.instance.InvokeNextFrame(() => {
				root.transform.parent.localScale = Vector3.one;
				root.transform.parent.localPosition = Vector2.zero;

				var activator = root.transform.parent.gameObject.GetOrAddComponent<HudRootActivator>();
				activator.hudroot = root;
				activator.crest = SifCrest;

				// critical for layering the hud frame, meter, and full-silk-orb correctly
				float bindOrbZ = root.transform.parent.parent.position.z;
				root.transform.localPosition = new(-1.54f, 0.15f);
				Vector3 rootPos = root.transform.position;
				root.transform.position = new(rootPos.x, rootPos.y, bindOrbZ - 0.00001f);

				root.transform.localScale = Vector3.one;
				meter.transform.localPosition = Vector3.zero;
				mask.transform.localPosition = Vector3.zero;
			});
		}

		hud.Coroutine = HudCoro;

		IEnumerator HudCoro(BindOrbHudFrame hudInstance) {
			HeroController hc = HeroController.instance;
			PlayerData pd = PlayerData.instance;
			GameObject mask = hud.Root!.FindChild("mask")!, glow = hud.Root!.FindChild("glow")!;
			while(true) {
				if (hc.IsPaused()) {
					yield return null;
					continue;
				}
				int masksMissing = pd.maxHealth - pd.health;
				if (masksMissing == 9) {
					if (mask.transform.localScale != Vector3.one) {
						hudInstance.hunterV2ChargedAudio.SpawnAndPlayOneShot(GlobalSettings.Audio.DefaultUIAudioSourcePrefab, hudInstance.transform.position);
					}
					mask.transform.localScale = Vector3.one;
				}
				else {
					float scale = masksMissing / 10f;
					mask.transform.localScale = new(scale, scale);
				}
				yield return null;
			}
		}

		#endregion

		SifCrest.BindEvent = Bind.OnBindStart;

		Moveset.Setup();

		Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}

}
