using HutongGames.PlayMaker;
using Needleforge.Attacks;
using Needleforge.Data;
using TravellerCrest.Data;
using UnityEngine;

namespace TravellerCrest.Mechanics;

internal static class Moveset {
	private static HeroConfigNeedleforge Cfg => SifCrest.Moveset.HeroConfig!;

	private const float STUN_DAMAGE = 0.8f;

	private static readonly tk2dSpriteAnimation BorrowedAnims;

	static Moveset() {
		GameObject libobj = new($"{SifId} Anims Borrowed") {
			hideFlags = HideFlags.HideAndDontSave
		};
		Object.DontDestroyOnLoad(libobj);

		BorrowedAnims = libobj.AddComponent<tk2dSpriteAnimation>();
	}

	internal static void Setup() {
		SifCrest.Moveset.HeroConfig = ScriptableObject.CreateInstance<HeroConfigNeedleforge>();

		Cfg.heroAnimOverrideLib = AnimationManager.library;
		Cfg.canBind = true;
		Cfg.forceBareInventory = false;
		Cfg.SetCanUseAbilities(true);

		SimpleAttacks();
		DownSlash();
		DashSlash();
		ChargedSlash();
	}

	private static void SimpleAttacks() {
		Cfg.wallSlashSlowdown = false;
		Cfg.SetAttackFields(
			time: 0.25f, recovery: 0.1f, cooldown: 0.3f,
			quickSpeedMult: 1.5f, quickCooldown: 0.15f
		);

		SifCrest.Moveset.Slash = new Attack {
			Name = "Neutral",
			AnimLibrary = AnimationManager.library,
			AnimName = "SlashEffect",
			Hitbox = [
				new(-3.34f, 0.17f),
				new(-3.92f, -0.16f),
				new(-3.06f, -0.57f),
				new(-0.16f, -0.79f),
				new(-0.22f, 0.60f),
			],
			Scale = new(0.8f, 1.3f),
			StunDamage = STUN_DAMAGE,
		};

		SifCrest.Moveset.AltSlash = new Attack {
			Name = "NeutralAlt",
			AnimLibrary = AnimationManager.library,
			AnimName = "SlashEffectAlt",
			Hitbox = [
				new(-3.16f, -0.1f),
				new(-3.84f, -0.37f),
				new(-3.30f, -0.7f),
				new(0.07f, -1.16f),
				new(-0.07f, 0.21f),
			],
			Scale = new(0.8f, 1.3f),
			StunDamage = STUN_DAMAGE,
		};

		SifCrest.Moveset.UpSlash = new Attack {
			Name = "Up",
			AnimLibrary = AnimationManager.library,
			AnimName = "SlashEffect",
			Hitbox = [
				new(0.98f, -0.58f),
				new(-1.72f, -0.63f),
				new(-3.84f, -0.25f),
				new(-3.83f, 0f),
				new(-1.86f, 0.37f),
				new(0.95f, 0.35f),
			],
			Scale = new(0.7f, -1.15f),
			StunDamage = STUN_DAMAGE,
		};
		SifCrest.Moveset.OnInitialized += RotateUpslash;

		SifCrest.Moveset.WallSlash = new Attack {
			Name = "Wall",
			AnimLibrary = AnimationManager.library,
			AnimName = "SlashEffectAlt",
			Hitbox = [
				new(-3.09f, -0.1f),
				new(-3.97f, -0.37f),
				new(-3.12f, -0.66f),
				new(-0.03f, -1.02f),
				new(-0.30f, 0.13f),
			],
			Scale = new(0.9f, 1.3f),
			StunDamage = STUN_DAMAGE,
		};

		static void RotateUpslash() {
			SifCrest.Moveset.UpSlash!.GameObject!.transform
				.localRotation = Quaternion.Euler(0, 0, -90);
		}
	}

	// TODO fsm multihitter
	private static void DownSlash() {
		Cfg.SetDownspikeFields(
			anticTime: 0.09f,
			time: 0.16f,
			recoveryTime: 0.09f
		);
		Cfg.downSlashType = HeroControllerConfig.DownSlashTypes.Slash;
		//Cfg.SetCustomDownslash("TRAVELLER DOWNSLASH", DownslashEdit);

		SifCrest.Moveset.DownSlash = new DownAttack {
			Name = "Down",
			AnimLibrary = AnimationManager.library,
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
			StunDamage = STUN_DAMAGE / 2,
			DamageMult = 0.55f,
		};
		SifCrest.Moveset.AltDownSlash = new DownAttack {
			Name = "DownAlt",
			AnimLibrary = AnimationManager.library,
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
			Scale = new(-0.8f, 1f),
			StunDamage = STUN_DAMAGE / 2,
			DamageMult = 0.55f,
		};

		static void DownslashEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
			endStates = [];
		}
	}

	private static void DashSlash() {
		Cfg.SetDashStabFields(
			time: 0.12f,
			speed: -50,
			bounceJumpSpeed: 18.6f,
			forceShortBounce: false
		);
	}

	private static void ChargedSlash() {
	
	}
}
