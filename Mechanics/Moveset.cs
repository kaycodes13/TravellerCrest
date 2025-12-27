using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Needleforge.Attacks;
using Needleforge.Data;
using Silksong.FsmUtil;
using TravellerCrest.Data;
using UnityEngine;
using Camera = GlobalSettings.Camera;

namespace TravellerCrest.Mechanics;

internal static class Moveset {
	private static HeroConfigNeedleforge Cfg => SifCrest.Moveset.HeroConfig!;

	private const float STUN_DAMAGE = 0.8f;

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
			recoveryTime: 0.24f
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

	#region Charged Slash

	private static void ChargedSlash() {
		Cfg.ChargedSlashFsmEdit = ChargedSlashFsmEdit;

		const float endLagMult = 3f;

		Vector2 distance = new(-4, 0f);
		float duration = 0.4f * endLagMult;
		AnimationCurve easeOut = new(
			new Keyframe(time: 0, value: 0, inTangent: 0, outTangent: 3),
			new Keyframe(time: 1f / endLagMult, value: 1),
			new Keyframe(time: 1, value: 1)
		);

		SifCrest.Moveset.ChargedSlash = new ChargedAttack {
			Name = "Charged",
			PlayOnActivation = false,
			PlayStepsInSequence = false,
			CameraShakeProfiles = [Camera.TinyShake],
			ScreenFlashColors = [new(1, 1, 1, 0.4f)],
			Steps = [
				new TravellingChargeAttackStep {
					AnimName = "Slash_Charged Effect TEST",
					Hitbox = [new(0, -1), new(0, 1), new(-2, 0)],
					CameraShakeIndex = 0,
					ScreenFlashIndex = 0,
					Travel = new() {
						Distance = distance, Duration = duration, Curve = easeOut,
					},
					Scale = new(0.5f, 2),
				},
				new TravellingChargeAttackStep {
					AnimName = "Slash_Charged Effect TEST",
					Hitbox = [new(0, -1), new(0, 1), new(-2, 0)],
					CameraShakeIndex = 0,
					ScreenFlashIndex = 0,
					Travel = new() {
						Distance = distance, Duration = duration, Curve = easeOut,
					},
				}
			]
		};
		SifCrest.Moveset.ChargedSlash.SetAnimLibrary(AnimationManager.library);
	}

	private static void ChargedSlashFsmEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault
			ownerHornet = new() { OwnerOption = OwnerDefaultOption.UseOwner },
			ownerAttack = new() {
				OwnerOption = OwnerDefaultOption.SpecifyGameObject,
				GameObject = SifCrest.Moveset.ChargedSlash!.GameObject!
			},
			ownerStepOne = new() {
				OwnerOption = OwnerDefaultOption.SpecifyGameObject,
				GameObject = SifCrest.Moveset.ChargedSlash!.Steps[0].GameObject!
			},
			ownerStepTwo = new() {
				OwnerOption = OwnerDefaultOption.SpecifyGameObject,
				GameObject = SifCrest.Moveset.ChargedSlash!.Steps[1].GameObject!
			};

		FsmState
			beginAttackState = fsm.AddState($"{SifId} Attack Starting"),
			stepOneState = fsm.AddState($"{SifId} Attack Step 1"),
			stepTwoState = fsm.AddState($"{SifId} Attack Step 2"),
			recoveryState = fsm.AddState($"{SifId} Recovery");

		// Play hornet antic anim, decelerate
		startState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Slash_Charged Antic",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.85f,
				brakeOnExit = true,
			},
			new SendMessageV2 {
				gameObject = ownerHornet,
				delivery = SendMessageV2.MessageType.SendMessage,
				options = SendMessageOptions.RequireReceiver,
				functionCall = new() { FunctionName = nameof(SpriteFlash.flashFocusHeal) }
			}
		);
		startState.AddTransition(FsmEvent.Finished.name, beginAttackState.name);

		// Play hornet attack anim, start first step on the first anim trigger
		beginAttackState.AddActions(
			new SetBoolValue {
				boolVariable = fsm.GetBoolVariable("Is Anim Finished"),
				boolValue = false,
			},
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Slash_Charged",
				animationTriggerEvent = FsmEvent.Finished,
			}
		);
		beginAttackState.AddTransition(FsmEvent.Finished.name, stepOneState.name);

		// Fire first attack, start second step on second anim trigger
		stepOneState.AddActions(
			new ActivateGameObject {
				gameObject = ownerAttack,
				activate = true,
				recursive = false,
			},
			new SendMessageV2 {
				gameObject = ownerStepOne,
				delivery = SendMessageV2.MessageType.SendMessage,
				options = SendMessageOptions.DontRequireReceiver,
				functionCall = new() { FunctionName = nameof(NailSlash.StartSlash) }
			},
			new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationTriggerEvent = FsmEvent.Finished
			}
		);
		stepOneState.AddTransition(FsmEvent.Finished.name, stepTwoState.name);

		// Fire second attack, start recovering when anim finished
		stepTwoState.AddActions(
			new SendMessageV2 {
				gameObject = ownerStepTwo,
				delivery = SendMessageV2.MessageType.SendMessage,
				options = SendMessageOptions.DontRequireReceiver,
				functionCall = new() { FunctionName = nameof(NailSlash.StartSlash) }
			},
			new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationCompleteEvent = FsmEvent.Finished,
			}
		);
		stepTwoState.AddTransition(FsmEvent.Finished.name, recoveryState.name);

		recoveryState.AddActions(
			new SendMessageV2 {
				gameObject = ownerHornet,
				delivery = SendMessageV2.MessageType.SendMessage,
				options = SendMessageOptions.DontRequireReceiver,
				functionCall = new() {
					FunctionName = nameof(HeroController.SetStartWithDownSpikeEnd)
				}
			}
		);

		endStates = [recoveryState];
	}

	#endregion
}
