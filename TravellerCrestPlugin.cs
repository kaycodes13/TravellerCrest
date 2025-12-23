using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Needleforge;
using Needleforge.Data;
using Needleforge.Attacks;
using TeamCherry.Localization;
using TravellerCrest.Mechanics;
using UnityEngine;

namespace TravellerCrest;

/*

TODO:

- up/wall/side attacks
- down attack (likely FSM)
- dash attack (likely FSM)
- charged attack (likely FSM)
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

		#region Moveset

		var cfg = ScriptableObject.CreateInstance<HeroConfigNeedleforge>();
		cfg.canBind = true;
		cfg.forceBareInventory = false;
		cfg.SetCanUseAbilities(true);
		cfg.wallSlashSlowdown = false;
		cfg.SetAttackFields(
			time: 0.25f, recovery: 0.1f, cooldown: 0.3f,
			quickSpeedMult: 1.5f, quickCooldown: 0.15f
		);
		cfg.downSlashType = HeroControllerConfig.DownSlashTypes.Slash;
		cfg.SetDashStabFields(
			time: 0.12f, speed: -50, bounceJumpSpeed: 18.6f, forceShortBounce: false
		);

		SifCrest.Moveset.HeroConfig = cfg;

		tk2dSpriteAnimation? animlib = null;

		SifCrest.Moveset.OnInitialized += () => {
			if (animlib)
				return;

			GameObject libobj = new("SifAnimLib");
			DontDestroyOnLoad(libobj);
			animlib = libobj.AddComponent<tk2dSpriteAnimation>();

			var wanderer = ToolItemManager.GetCrestByName("Wanderer").HeroConfig;
			animlib.clips = [
				wanderer.GetAnimationClip("SlashEffect"),
				wanderer.GetAnimationClip("SlashEffectAlt"),
				wanderer.GetAnimationClip("DownSlashEffect"),
			];

			SifCrest.Moveset.Slash!.AnimLibrary = animlib;
			SifCrest.Moveset.AltSlash!.AnimLibrary = animlib;
			SifCrest.Moveset.WallSlash!.AnimLibrary = animlib;
			SifCrest.Moveset.UpSlash!.AnimLibrary = animlib;
			SifCrest.Moveset.DownSlash!.AnimLibrary = animlib;

			SifCrest.Moveset.UpSlash!.GameObject!.transform.localRotation = new(0, 0, -0.707f, 0.707f);
		};

		SifCrest.Moveset.Slash = new Attack {
			AnimName = "SlashEffect",
			Hitbox = [
				new(-3.34f, 0.17f),
				new(-3.92f, -0.16f),
				new(-3.06f, -0.57f),
				new(-0.16f, -0.79f),
				new(-0.22f, 0.60f),
			],
			Scale = new(0.8f, 1.3f),
			StunDamage = 0.8f,
		};
		SifCrest.Moveset.AltSlash = new Attack {
			AnimName = "SlashEffectAlt",
			Hitbox = [
				new(-3.16f, -0.1f),
				new(-3.84f, -0.37f),
				new(-3.30f, -0.7f),
				new(0.07f, -1.16f),
				new(-0.07f, 0.21f),
			],
			Scale = new(0.8f, 1.3f),
			StunDamage = 0.8f,
		};
		SifCrest.Moveset.UpSlash = new Attack {
			AnimName = "SlashEffectAlt",
			Hitbox = [
				new(0.98f, -0.58f),
				new(-1.72f, -0.63f),
				new(-3.84f, -0.25f),
				new(-3.83f, 0f),
				new(-1.86f, 0.37f),
				new(0.95f, 0.35f),
			],
			Scale = new(0.7f, -1.15f),
			StunDamage = 0.8f,
		};
		SifCrest.Moveset.WallSlash = new Attack {
			AnimName = "SlashEffectAlt",
			Hitbox = [
				new(-3.09f, -0.1f),
				new(-3.97f, -0.37f),
				new(-3.12f, -0.66f),
				new(-0.03f, -1.02f),
				new(-0.30f, 0.13f),
			],
			Scale = new(0.9f, 1.3f),
			StunDamage = 0.8f,
		};
		SifCrest.Moveset.DownSlash = new DownAttack {
			AnimName = "DownSlashEffect",
			Hitbox = [
				new(2.19f, -0.24f),
				new(2.07f, -1.49f),
				new(1.23f, -2.15f),
				new(0.16f, -2.44f),
				new(-1.23f, -2.23f),
				new(-1.82f, -1.59f),
				new(-1.84f, -0.25f),
			],
			Scale = new(0.8f, 1f),
			StunDamage = 0.8f,
			DamageMult = 0.55f,
			// TODO fsm multihitter
		};

		#endregion

		Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}
}
