using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Needleforge.Attacks;
using Needleforge.Data;
using Silksong.FsmUtil;
using System.Linq;
using TravellerCrest.Attacks;
using TravellerCrest.Data;
using UnityEngine;
using Camera = GlobalSettings.Camera;

namespace TravellerCrest.Mechanics;

internal static class Moveset {

	private const float STUN_DAMAGE = 0.8f;
	private const float DOWN_ATTACK_GAP = 0.01f;

	internal static void Setup() {
		Moves.HeroConfig = ScriptableObject.CreateInstance<HeroConfigNeedleforge>();

		Config.heroAnimOverrideLib = AnimationManager.library;
		Config.canBind = true;
		Config.forceBareInventory = false;
		Config.SetCanUseAbilities(true);

		SimpleAttacks();
		DownSlash();
		DashSlash();
		ChargedSlash();
	}

	private static void SimpleAttacks() {
		Config.wallSlashSlowdown = false;
		Config.SetAttackFields(
			time: 0.25f, recovery: 0.1f, cooldown: 0.3f,
			quickSpeedMult: 1.5f, quickCooldown: 0.15f
		);

		Moves.Slash = new Attack {
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

		Moves.AltSlash = new AttackPositionable {
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
			StunDamage = STUN_DAMAGE,
			Scale = new(0.8f, 1.3f),
			Transform = new() {
				Position = new(-0.18f, 0.38f)
			},
		};

		Moves.UpSlash = new AttackPositionable {
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
			StunDamage = STUN_DAMAGE,
			Scale = new(0.7f, -1.15f),
			Transform = new() {
				Position = new(-0.03f, 0.22f),
				Rotation = Quaternion.Euler(0, 0, -90)
			},
		};

		Moves.WallSlash = new AttackPositionable {
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
			StunDamage = STUN_DAMAGE,
			Scale = new(0.9f, 1.3f),
			Transform = new() {
				Position = new(0.24f, 0.43f)
			},
		};

		Moves.OnInitialized += SetSimpleAttackSounds;

		static void SetSimpleAttackSounds() {
			var wanderer = Hc.configs.First(x => x.Config.name == "Wanderer");
			Moves.Slash!.Sound = GetSound(wanderer.NormalSlashObject);
			Moves.AltSlash!.Sound = GetSound(wanderer.AlternateSlashObject);
			Moves.UpSlash!.Sound = GetSound(wanderer.UpSlashObject);
			Moves.WallSlash!.Sound = GetSound(wanderer.WallSlashObject);
		}
	}

	#region Down Slash

	private static void DownSlash() {
		Config.SetDownspikeFields(
			anticTime: 0.09f,
			time: 0.16f,
			recoveryTime: 0.24f
		);
		Config.SetCustomDownslash("TRAVELLER DOWNSLASH", DownslashEdit);

		Moves.DownSlash = new DownAttackNonBouncing {
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
			Scale = new(-0.8f, 1f),
			Transform = new() {
				Position = new(0.05f, -0.15f)
			},
			StunDamage = STUN_DAMAGE / 2f,
			KnockbackMult = 0.5f,
			DamageMult = 0.55f,
		};
		Moves.AltDownSlash = new DownAttackNonBouncing {
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
			Scale = new(0.8f, 1f),
			Transform = new() {
				Position = new(-0.03f, -0.15f)
			},
			StunDamage = STUN_DAMAGE / 2f,
			DamageMult = 0.55f,
		};

		Moves.OnInitialized += SetDownAttackSounds;

		static void SetDownAttackSounds() {
			var wanderer = Hc.configs.First(x => x.Config.name == "Wanderer");
			Moves.DownSlash!.Sound = GetSound(wanderer.DownSlashObject);
			Moves.AltDownSlash!.Sound = GetSound(wanderer.AltDownSlashObject);
		}
	}

	private static void DownslashEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		#region Defining states/variables/events/etc
		FsmOwnerDefault
			ownerHornet = new() { OwnerOption = OwnerDefaultOption.UseOwner };
		FsmFloat
			delay = fsm.GetFloatVariable($"{SifId} Delay");
		FsmBool
			bounceAtEnd = fsm.GetBoolVariable($"{SifId} Do Bounce");
		FsmEvent
			dashEvent = FsmEvent.GetFsmEvent("SPRINT"),
			bounceAnywayEvent = FsmEvent.GetFsmEvent("BOUNCE ANYWAY"),
			attackLandedEvent = FsmEvent.FindEvent("ATTACK LANDED"),
			bounceTinkedEvent = FsmEvent.FindEvent("BOUNCE TINKED"),
			bounceCancelEvent = FsmEvent.FindEvent("BOUNCE CANCEL"),
			leavingSceneEvent = FsmEvent.FindEvent("LEAVING SCENE");
		FsmState
			firstStepState = fsm.AddState($"{SifId} Step 1"),
			delayState = fsm.AddState($"{SifId} Step Gap"),
			firstHitState = fsm.AddState($"{SifId} Step 1 Hit"),
			secondStepState = fsm.AddState($"{SifId} Step 2"),
			missState = fsm.AddState($"{SifId} End"),
			bounceState = fsm.AddState($"{SifId} Bounce"),
			dashCancelState = fsm.AddState($"{SifId} Dash Cancel");
		#endregion

		// relinquish control, allow clawline cancels, etc
		startState.AddLambdaMethod(finished => {
			fsm.GetBoolVariable("In Crest Attack").Value = true;
			fsm.GetBoolVariable("Disabled Animation").Value = true;
			bounceAtEnd.Value = false;
			Hc.StopAnimationControl();
			Hc.RelinquishControlNotVelocity();
			Hc.QueueCancelDownAttack();
			Hc.cState.isInCancelableFSMMove = true;
			finished();
		});
		startState.AddTransition(FsmEvent.Finished.name, firstStepState.name);

		// play first slash, reduce gravity
		firstStepState.AddLambdaMethod(finished => {
			Hc.cState.downAttacking = true;
			Moves.DownSlash!.GameObject!.SendMessage(nameof(NailSlash.StartSlash));
			finished();
		});
		firstStepState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "DownSlash",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.78f,
				brakeOnExit = false,
			}
		);
		firstStepState.AddTransition(FsmEvent.Finished.name, delayState.name);
		firstStepState.AddTransition(attackLandedEvent.name, firstHitState.name);
		firstStepState.AddTransition(bounceTinkedEvent.name, firstHitState.name);
		firstStepState.AddTransition(bounceCancelEvent.name, firstHitState.name);
		firstStepState.AddTransition(leavingSceneEvent.name, missState.name);
		AddDashCancel(firstStepState);

		// if first step hits; stop downward movement and queue a bounce at the end
		firstHitState.AddLambdaMethod(finished => {
			Hc.rb2d.linearVelocity = new(Hc.rb2d.linearVelocity.x, 20f);
			Hc.StartDownspikeInvulnerabilityLong();
			bounceAtEnd.Value = true;
			finished();
		});
		firstHitState.AddAction(new Tk2dWatchAnimationEvents {
			gameObject = ownerHornet,
			animationCompleteEvent = FsmEvent.Finished,
		});
		firstHitState.AddTransition(FsmEvent.Finished.name, delayState.name);
		AddDashCancel(firstHitState);

		// wait a configurable amount of time between steps
		delayState.AddActions(
			new SetFloatValue {
				floatVariable = delay,
				floatValue = DOWN_ATTACK_GAP,
			},
			new Wait { time = delay, }
		);
		delayState.AddTransition(FsmEvent.Finished.name, secondStepState.name);
		delayState.AddTransition(leavingSceneEvent.name, missState.name);
		AddDashCancel(delayState);

		// perform the second slash, reduce gravity again
		secondStepState.AddLambdaMethod(finished => {
			Moves.AltDownSlash!.GameObject!.SendMessage(nameof(NailSlash.StartSlash));
			finished();
		});
		secondStepState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "DownSlashAlt",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.78f,
				brakeOnExit = false,
			}
		);
		secondStepState.AddTransition(FsmEvent.Finished.name, missState.name);
		secondStepState.AddTransition(attackLandedEvent.name, bounceState.name);
		secondStepState.AddTransition(bounceTinkedEvent.name, bounceState.name);
		secondStepState.AddTransition(bounceCancelEvent.name, bounceState.name);
		secondStepState.AddTransition(leavingSceneEvent.name, missState.name);
		AddDashCancel(secondStepState);

		// if queued bounce, redirect to bounce; if not, end normally
		missState.AddAction(new BoolTest {
			boolVariable = bounceAtEnd,
			isTrue = bounceAnywayEvent
		});
		missState.AddLambdaMethod(finished => {
			Hc.cState.downAttacking = false;
			Hc.rb2d.linearVelocity = Vector2.zero;
			Hc.FinishDownspike(true);
			finished();
		});
		missState.AddTransition(bounceAnywayEvent.name, bounceState.name);

		// perform a bounce because something was pogo'd upon
		bounceState.AddLambdaMethod(finished => {
			Hc.cState.downAttacking = false;
			Hc.SetStartWithDownSpikeBounce();
			finished();
		});

		// if a dash input happens at any point, cancel the entire attack
		dashCancelState.AddLambdaMethod(finished => {
			Moves.DownSlash!.GameObject!.SendMessage(nameof(NailSlash.CancelAttack));
			Moves.AltDownSlash!.GameObject!.SendMessage(nameof(NailSlash.CancelAttack));
			Hc.SetStartWithDash();
			finished();
		});

		endStates = [missState, bounceState, dashCancelState];

		void AddDashCancel(FsmState state) {
			state.AddAction(new ListenForDash {
				wasPressed = dashEvent,
				delayBeforeActive = 0f,
				BlocksFinish = false
			});
			state.AddTransition(dashEvent.name, dashCancelState.name);
		}
	}

	#endregion

	#region Dash Slash

	private static void DashSlash() {
		Config.SetDashStabFields(
			time: 0.12f,
			speed: -50,
			bounceJumpSpeed: 18.6f,
			forceShortBounce: false
		);
		Config.DashSlashFsmEdit = DashSlashFsmEdit;

		const float endLagMult = 3f;

		Vector2 distance = new(-4, 0f);
		float duration = 0.4f * endLagMult;
		AnimationCurve easeOut = new(
			new Keyframe(time: 0, value: 0, inTangent: 0, outTangent: 3),
			new Keyframe(time: 1f / endLagMult, value: 1),
			new Keyframe(time: 1, value: 1)
		);

		Moves.DashSlash = new DashAttack {
			Name = "Dash",
			Steps = [
				new DashAttackStepTravelling {
					AnimName = "Dash Attack Effect TEST",
					Hitbox = [new(0, -1), new(0, 1), new(-2, 1), new(-2, -1)],
					Travel = new() {
						Distance = distance, Duration = duration, Curve = easeOut,
					}
				}
			]
		};
		Moves.DashSlash.SetAnimLibrary(AnimationManager.library);

		Moves.OnInitialized += SetDashAttackSound;

		static void SetDashAttackSound() {
			var shaman = Hc.configs.First(x => x.Config.name == "Shaman");
			Moves.DashSlash!.Steps[0].Sound = GetSound(shaman.NormalSlashObject);
		}
	}

	private static void DashSlashFsmEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault
			ownerHornet = new() { OwnerOption = OwnerDefaultOption.UseOwner };

		FsmState
			slashState = fsm.AddState($"{SifId} Slash"),
			endState = fsm.AddState($"{SifId} End");

		// Play antic, slow down, relinquishing control stuff
		startState.AddLambdaMethod(finished => {
			Hc.attackAudioTable.SpawnAndPlayOneShot(Hc.transform.position);
			Hc.SetAllowRecoilWhileRelinquished(true);
			finished();
		});
		startState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Dash Attack Antic",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.65f,
				brakeOnExit = false,
			}
		);
		startState.AddTransition(FsmEvent.Finished.name, slashState.name);

		// Play anim attack and audio, start attack, disable gravity, leap back
		slashState.AddLambdaMethod(finished => {
			Hc.cState.onGround = false;
			Hc.AffectedByGravity(false);
			Moves.DashSlash!.Steps[0].GameObject!.SendMessage(nameof(NailSlash.StartSlash));
			finished();
		});
		slashState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Dash Attack",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new SetVelocityByScale {
				gameObject = ownerHornet,
				speed = 25f,
				ySpeed = 14.5f,
				everyFrame = false,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.95f,
				brakeOnExit = false,
			}
		);
		slashState.AddTransition(FsmEvent.Finished.name, endState.name);

		// re-enable gravity, set attack cooldown, etc
		endState.AddLambdaMethod(finished => {
			Hc.SetStartFromReaperUpperslash();
			Hc.CrestAttackRecovery();
			Hc.AffectedByGravity(true);
			Hc.SetAllowRecoilWhileRelinquished(false);
			finished();
		});

		endStates = [endState];
	}

	#endregion

	#region Charged Slash

	private static void ChargedSlash() {
		Config.ChargedSlashFsmEdit = ChargedSlashFsmEdit;

		const float endLagMult = 3f;

		Vector2 distance = new(-4, 0f);
		float duration = 0.4f * endLagMult;
		AnimationCurve easeOut = new(
			new Keyframe(time: 0, value: 0, inTangent: 0, outTangent: 3),
			new Keyframe(time: 1f / endLagMult, value: 1),
			new Keyframe(time: 1, value: 1)
		);

		Moves.ChargedSlash = new ChargedAttack {
			Name = "Charged",
			PlayOnActivation = false,
			PlayStepsInSequence = false,
			CameraShakeProfiles = [Camera.TinyShake],
			ScreenFlashColors = [new(1, 1, 1, 0.4f)],
			Steps = [
				new ChargeAttackStepTravelling {
					AnimName = "Slash_Charged Effect TEST",
					Hitbox = [new(0, -1), new(0, 1), new(-2, 0)],
					CameraShakeIndex = 0,
					ScreenFlashIndex = 0,
					Travel = new() {
						Distance = distance, Duration = duration, Curve = easeOut,
					},
					Scale = new(0.5f, 2),
				},
				new ChargeAttackStepTravelling {
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
		Moves.ChargedSlash.SetAnimLibrary(AnimationManager.library);

		Moves.OnInitialized += SetChargedAttackSounds;

		static void SetChargedAttackSounds() {
			var shaman = Hc.configs.First(x => x.Config.name == "Shaman");
			foreach (var step in Moves.ChargedSlash!.Steps)
				step.Sound = GetSound(shaman.ChargeSlash);
		}
	}

	private static void ChargedSlashFsmEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault
			ownerHornet = new() { OwnerOption = OwnerDefaultOption.UseOwner },
			ownerAttack = new() {
				OwnerOption = OwnerDefaultOption.SpecifyGameObject,
				GameObject = Moves.ChargedSlash!.GameObject!
			},
			ownerStepOne = new() {
				OwnerOption = OwnerDefaultOption.SpecifyGameObject,
				GameObject = Moves.ChargedSlash!.Steps[0].GameObject!
			},
			ownerStepTwo = new() {
				OwnerOption = OwnerDefaultOption.SpecifyGameObject,
				GameObject = Moves.ChargedSlash!.Steps[1].GameObject!
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

	#region Local Utilities

	static HeroController Hc => HeroController.instance;
	static MovesetData Moves => SifCrest.Moveset;
	static HeroConfigNeedleforge Config => SifCrest.Moveset.HeroConfig!;
	static AudioClip GetSound(GameObject go) => go.GetComponent<AudioSource>().clip;

	#endregion

}
