using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Needleforge.Attacks;
using Needleforge.Data;
using Silksong.FsmUtil;
using System;
using System.Linq;
using TravellerCrest.Attacks;
using TravellerCrest.Data;
using UnityEngine;
using Camera = GlobalSettings.Camera;

namespace TravellerCrest.Mechanics;

internal static class Moveset {

	private const float STUN_DAMAGE = 0.8f;
	private const float DOWN_ATTACK_GAP = 0.01f;

	private static readonly Vector2[] JUST_ATTACK_HITBOX = [
		new(-2f, 0.1f),
		new(-0.8f, 1.9f),
		new(-1.6f, 2f),
		new(-2.6f, 1.7f),
		new(-3.3f, 1f),
		new(-4f, 0f),
		new(-3.2f, -1f),
		new(-2.5f, -1.5f),
		new(-1.6f, -1.8f),
		new(-0.8f, -1.7f),
	];

	static HeroController Hc => HeroController.instance;
	static MovesetData Moves => SifCrest.Moveset;
	static HeroConfigNeedleforge Config => SifCrest.Moveset.HeroConfig!;
	static AudioClip GetSound(GameObject go) => go.GetComponent<AudioSource>().clip;

	internal static void Setup() {
		Moves.HeroConfig = ScriptableObject.CreateInstance<HeroConfigNeedleforge>();

		Config.heroAnimOverrideLib = AnimationManager.MainLib;
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
			AnimLibrary = AnimationManager.MainLib,
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
			AnimLibrary = AnimationManager.MainLib,
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
			AnimLibrary = AnimationManager.MainLib,
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
			AnimLibrary = AnimationManager.MainLib,
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
		Config.SetDownspikeFields(recoveryTime: 0.24f);
		Config.SetCustomDownslash("TRAVELLER DOWNSLASH", DownslashEdit);

		Moves.DownSlash = new DownAttackStationary {
			Name = "Down",
			AnimLibrary = AnimationManager.MainLib,
			AnimName = "Slash_Charged Effect",
			Hitbox = JUST_ATTACK_HITBOX,
			Transform = new() {
				Position = new(0.05f, -0.15f),
				Rotation = Quaternion.Euler(0, 0, 40),
			},
		};

		Moves.OnInitialized += SetDownAttackSounds;

		static void SetDownAttackSounds() {
			var shaman = Hc.configs.First(x => x.Config.name == "Shaman");
			Moves.DownSlash!.Sound = GetSound(shaman.DownSlashObject);
		}
	}

	private static void DownslashEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault
			ownerHornet = new();
		FsmEvent
			dashEvent = FsmEvent.GetFsmEvent("SPRINT");
		FsmState
			slashState = fsm.AddState($"{SifId} Downslash"),
			missState = fsm.AddState($"{SifId} End"),
			bounceState = fsm.AddState($"{SifId} Bounce"),
			dashCancelState = fsm.AddState($"{SifId} Dash Cancel");

		// ANTIC
		startState.AddMethod(() => {
			fsm.GetBoolVariable("Disabled Animation").Value = true;
			fsm.GetBoolVariable("In Crest Attack").Value = true;
			Hc.StopAnimationControl();
			Hc.RelinquishControlNotVelocity();
			Hc.AffectedByGravity(false);
			Hc.QueueCancelDownAttack();
			Hc.cState.isInCancelableFSMMove = true;

			// Clamp out "downward" velocity in a way compatible with Glissando
			Func<float, float, float>
				clampFn = Hc.transform.localScale.y > 0 ? Mathf.Max : Mathf.Min;
			Hc.rb2d.linearVelocityY = clampFn(0, Hc.rb2d.linearVelocityY);
		});
		startState.AddActions([
			new DecelerateXY {
				gameObject = ownerHornet,
				decelerationX = 0.9f,
				decelerationY = 0.7f,
			},
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "DownSlash",
				animationTriggerEvent = FsmEvent.Finished,
			},
		]);
		startState.AddTransition(FsmEvent.Finished.name, slashState.name);
		AddDashCancel(startState);

		// DOWNSLASH
		// fire the attack, move hornet up and back
		slashState.AddMethod(() => {
			Hc.AffectedByGravity(true);
			Hc.cState.downAttacking = true;
			Moves.DownSlash!.GameObject!.SendMessage(nameof(NailSlash.StartSlash));
		});
		slashState.AddActions(
			new SetVelocityByScale {
				gameObject = ownerHornet,
				speed = 8,
				ySpeed = 14,
			},
			new DecelerateXY {
				gameObject = ownerHornet,
				decelerationX = 0.9f,
				decelerationY = 1,
			},
			new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationCompleteEvent = FsmEvent.Finished,
			}
		);
		slashState.AddTransition("ATTACK LANDED", bounceState.name);
		slashState.AddTransition("BOUNCE TINKED", bounceState.name);
		slashState.AddTransition("BOUNCE CANCEL", bounceState.name);
		slashState.AddTransition("LEAVING SCENE", missState.name);
		slashState.AddTransition(FsmEvent.Finished.name, missState.name);
		AddDashCancel(slashState);

		// DOWNSLASH END
		missState.AddMethod(() => {
			Hc.cState.downAttacking = false;
			Hc.FinishDownspike();
		});

		// DOWNSLASH BOUNCE
		bounceState.AddMethod(() => {
			Hc.rb2d.linearVelocity = Vector2.zero;
			Hc.SetStartWithDownSpikeBounce();
		});

		// if a dash input happens at any point, cancel the entire attack
		dashCancelState.AddMethod(() => {
			Moves.DownSlash!.GameObject!.SendMessage(nameof(NailSlash.CancelAttack));
			Hc.SetStartWithDash();
		});

		endStates = [missState, bounceState, dashCancelState];
		return;

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

		Moves.DashSlash = new DashAttack {
			Name = "Dash",
			Steps = [
				new DashAttackStepPositionable {
					AnimName = "Slash_Charged Effect",
					Hitbox = JUST_ATTACK_HITBOX,
					Transform = new() {
						Position = new(-5, 0, 0),
					},
					Scale = new(-1, 0.8f),
					KeepWorldPosition = true,
					StunDamage = STUN_DAMAGE,
				},
				new DashAttackStepPositionable {
					AnimName = "Wanderer RecoilStab Efct",
					Hitbox = [
						new(-3.09f, -0.22f),
						new(-4.33f, -0.68f),
						new(-2.99f, -1.22f),
						new(-0.31f, -1.19f),
						new(-0.35f, -0.09f),
					],
					Transform = new() {
						Position = new(0.74f, 0.81f),
						Rotation = Quaternion.Euler(0, 0, 24.84f)
					},
					Scale = new(0.99f, 1.16f),
					StunDamage = STUN_DAMAGE,
				},
			]
		};
		Moves.DashSlash.SetAnimLibrary(AnimationManager.MainLib);

		Moves.OnInitialized += SetDashAttackSoundAndEvents;

		static void SetDashAttackSoundAndEvents() {
			var shaman = Hc.configs.First(x => x.Config.name == "Shaman");
			Moves.DashSlash!.Steps[0].Sound = GetSound(shaman.NormalSlashObject);

			foreach (var step in Moves.DashSlash!.Steps) {
				var de = step.GameObject!.GetComponent<DamageEnemies>();
				de.dealtDamageFSM = Hc.sprintFSM;
				de.dealtDamageFSMEvent = "DASH HIT";
		}
	}
	}

	private static void DashSlashFsmEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault
			ownerHornet = new() { OwnerOption = OwnerDefaultOption.UseOwner };

		FsmState
			continueSprintState = fsm.GetState("Continue Sprint?")!,
			bonkState = fsm.GetState("Bonk")!,
			slashState = fsm.AddState($"{SifId} Slash"),
			dashCancelState = fsm.AddState($"{SifId} Dash Cancel"),
			jumpCancelState = fsm.AddState($"{SifId} Jump Cancel"),
			recoveryState = fsm.AddState($"{SifId} End"),
			recoilAnticState = fsm.AddState($"{SifId} Followup Antic"),
			recoilSlashState = fsm.AddState($"{SifId} Followup Slash"),
			recoilEndState = fsm.AddState($"{SifId} Followup End"),
			recoilBounceState = fsm.AddState($"{SifId} Followup Bounce");

		#region Craft attack + leap back

		// Play antic, slow down, relinquishing control stuff
		startState.AddMethod(() => {
			Hc.attackAudioTable.SpawnAndPlayOneShot(Hc.transform.position);
			Hc.SetAllowRecoilWhileRelinquished(true);
		});
		startState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Dash Attack Antic",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.85f,
				brakeOnExit = false,
			}
		);
		startState.AddTransition(FsmEvent.Finished.name, slashState.name);

		// Play anim attack and audio, start attack, disable gravity, leap back
		slashState.AddMethod(() => {
			Hc.cState.onGround = false;
			Hc.AffectedByGravity(false);
			Moves.DashSlash!.Steps[0].GameObject!.SendMessage(nameof(NailSlash.StartSlash));
		});
		slashState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Dash Attack",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new SetVelocityByScale {
				gameObject = ownerHornet,
				speed = 14f,
				ySpeed = 16f,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.9f,
			},
			new ListenForAttackV2 {
				IsActive = true,
				queueBool = false,
				WasPressed = FsmEvent.GetFsmEvent("ATTACK"),
				DelayBeforeActive = 0.05f,
			},
			new ListenForDashV2 {
				IsActive = true,
				WasPressed = FsmEvent.GetFsmEvent("DASH"),
				DelayBeforeActive = 0.05f,
			},
			new ListenForJumpV2 {
				activeBool = true,
				queueBool = false,
				isPressedBool = false,
				wasPressed = FsmEvent.GetFsmEvent("JUMP"),
				delayBeforeActive = 0.05f,
			}
		);
		slashState.AddTransition(FsmEvent.Finished.name, recoveryState.name);
		slashState.AddTransition("DAMAGER TINKED", bonkState.name);
		slashState.AddTransition("ATTACK", recoilAnticState.name);
		slashState.AddTransition("DASH", dashCancelState.name);
		slashState.AddTransition("JUMP", jumpCancelState.name);

		// allow attack to be canceled via re-dashing
		dashCancelState.AddMethod(Hc.SetStartWithDash);

		// allow attack to be canceled via jumping/floating
		jumpCancelState.AddMethod(() => {
			if (Hc.playerData.hasDoubleJump)
				Hc.SetStartWithDoubleJump();
			else if (Hc.playerData.hasBrolly)
				Hc.SetStartWithBrolly();
		});

		// re-enable gravity, set attack cooldown, etc
		recoveryState.AddMethod(() => {
			Hc.SetStartFromReaperUpperslash();
			Hc.CrestAttackRecovery();
			Hc.AffectedByGravity(true);
			Hc.SetAllowRecoilWhileRelinquished(false);
		});
		recoveryState.AddTransition(FsmEvent.Finished.name, continueSprintState.name);

		#endregion

		#region Lunging followup attack

		// halt velocity, play recoil stab anim+sound
		recoilAnticState.AddMethod(() => {
			Hc.rb2d.linearVelocity = Vector2.zero;
			Hc.audioCtrl.PlaySound(GlobalEnums.HeroSounds.DASH);
			Hc.attackAudioTable.SpawnAndPlayOneShot(Hc.transform.position);
		});
		recoilAnticState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Wanderer RecoilStab",
				animationTriggerEvent = FsmEvent.Finished,
			}
		);
		recoilAnticState.AddTransition(FsmEvent.Finished.name, recoilSlashState.name);

		// do recoil stab movement + start the attack
		recoilSlashState.AddMethod(() => {
			Moves.DashSlash!.Steps[1].GameObject!.SendMessage(nameof(NailSlash.StartSlash));
			Hc.StartDownspikeInvulnerability();
		});
		recoilSlashState.AddActions(
			new SetVelocityByScale {
				gameObject = ownerHornet,
				speed = -45f,
				ySpeed = -15f,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.95f,
			},
			new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationCompleteEvent = FsmEvent.Finished,
			}
		);
		recoilSlashState.AddTransition("FINISHED", continueSprintState.name);
		recoilSlashState.AddTransition("DAMAGER TINKED", bonkState.name);
		recoilSlashState.AddTransition("DASH HIT", recoilBounceState.name);

		// recoil stab bounces off enemies' heads
		recoilBounceState.AddMethod(() => {
			Hc.rb2d.linearVelocity = Vector2.zero;
			Hc.SetStartWithDownSpikeBounce();
			Hc.CrestAttackRecovery();
			Hc.AffectedByGravity(true);
			Hc.SetAllowRecoilWhileRelinquished(false);
			Moves.DashSlash!.Steps[1].GameObject!.SendMessage(nameof(NailSlash.CancelAttack));
		});

		#endregion

		endStates = [jumpCancelState, dashCancelState, recoilBounceState];
	}

	#endregion

	#region Charged Slash

	private static void ChargedSlash() {
		Config.ChargedSlashFsmEdit = ChargedSlashFsmEdit;

		float knockback = 0.1f, damage = 0.6667f;

		Moves.ChargedSlash = new ChargedAttack {
			Name = "Charged",
			PlayOnActivation = false,
			PlayStepsInSequence = false,
			ScreenFlashColors = [new(1, 1, 1, 0.4f)],
			Steps = [
				new ChargeAttackStepPositionable {
					AnimName = "Slash_Charged Effect",
					Hitbox = JUST_ATTACK_HITBOX,
					CameraShakeIndex = 0,
					ScreenFlashIndex = 0,
					Transform = new() {
						Position = new(-5.5f, 0, 0),
					},
					Scale = new(-1, 1),
					KnockbackMult = knockback,
					DamageMult = damage,
				},
				new ChargeAttackStepPositionable {
					AnimName = "Slash_Charged Effect",
					Hitbox = JUST_ATTACK_HITBOX,
					CameraShakeIndex = 0,
					ScreenFlashIndex = 0,
					Transform = new() {
						Position = new(0.5f, 0, 0),
					},
					KnockbackMult = knockback,
					DamageMult = damage,
				},
				new ChargeAttackStepPositionable {
					AnimName = "Slash_Charged Effect",
					Hitbox = JUST_ATTACK_HITBOX,
					CameraShakeIndex = 1,
					ScreenFlashIndex = 0,
					Transform = new() {
						Position = new(-5.5f, 0, 0),
					},
					Scale = new(-1, 1),
					KnockbackMult = knockback,
					DamageMult = damage,
				},
			]
		};
		Moves.ChargedSlash.SetAnimLibrary(AnimationManager.MainLib);

		Moves.OnInitialized += SetChargedAttackSoundsAndShakes;

		static void SetChargedAttackSoundsAndShakes() {
			Moves.ChargedSlash!.CameraShakeProfiles = [Camera.TinyShake, Camera.EnemyKillShake];

			var shaman = Hc.configs.First(x => x.Config.name == "Shaman");
			var sound = shaman.ChargeSlash.GetComponent<PlayRandomAudioEvent>().audioEvent
				.Clips.FirstOrDefault(x => x.name == "hornet_shaman_needle_art");
			foreach (var step in Moves.ChargedSlash!.Steps)
				step.Sound = sound;
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
			},
			ownerStepThree = new() {
				OwnerOption = OwnerDefaultOption.SpecifyGameObject,
				GameObject = Moves.ChargedSlash!.Steps[2].GameObject!
			};

		FsmState
			beginAttackState = fsm.AddState($"{SifId} Attack Starting"),
			stepOneState = fsm.AddState($"{SifId} Attack Step 1"),
			stepTwoState = fsm.AddState($"{SifId} Attack Step 2"),
			stepThreeState = fsm.AddState($"{SifId} Attack Step 3"),
			recoveryState = fsm.AddState($"{SifId} Recovery");

		fsm.GetState("Cancel All")!.AddMethod(() =>
			HeroController.instance.AllowRecoil()
		);

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
		beginAttackState.AddMethod(() =>
			HeroController.instance.PreventRecoil(
				AnimationManager.MainLib.GetClipByName("Slash_Charged").Duration
			)
		);
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

		// Fire second attack, start third step on third anim trigger
		stepTwoState.AddActions(
			new SendMessageV2 {
				gameObject = ownerStepTwo,
				delivery = SendMessageV2.MessageType.SendMessage,
				options = SendMessageOptions.DontRequireReceiver,
				functionCall = new() { FunctionName = nameof(NailSlash.StartSlash) }
			},
			new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationTriggerEvent = FsmEvent.Finished,
			}
		);
		stepTwoState.AddTransition(FsmEvent.Finished.name, stepThreeState.name);

		// Fire third attack, start recovering when anim finished
		stepThreeState.AddActions(
			new SendMessageV2 {
				gameObject = ownerStepThree,
				delivery = SendMessageV2.MessageType.SendMessage,
				options = SendMessageOptions.DontRequireReceiver,
				functionCall = new() { FunctionName = nameof(NailSlash.StartSlash) }
			},
			new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationTriggerEvent = FsmEvent.Finished,
				animationCompleteEvent = FsmEvent.Finished,
			}
		);
		stepThreeState.AddTransition(FsmEvent.Finished.name, recoveryState.name);

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
