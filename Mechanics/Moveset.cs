using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Needleforge.Attacks;
using Needleforge.Data;
using Silksong.FsmUtil;
using System;
using System.Linq;
using TravellerCrest.Attacks;
using TravellerCrest.Data;
using TravellerCrest.Utils;
using UnityEngine;
using Camera = GlobalSettings.Camera;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class Moveset {

	private const float STUN_DAMAGE = 0.8f;

	internal static void RefreshSettings() {
		var dash = (DashStepPositionable)DashSlashMain;
		dash.Scale = new(Inst.dashSX, Inst.dashSY);
		dash.Transform!.Position = new(Inst.dashPX, Inst.dashPY);
		dash.Transform.Rotation = Quaternion.Euler(0, 0, Inst.dashR);

		var pogo = (DownAttackPositionable)Moves.DownSlash!;
		pogo.Scale = new(Inst.pogoSX, Inst.pogoSY);
		pogo.Transform!.Position = new(Inst.pogoPX, Inst.pogoPY);
		pogo.Transform.Rotation = Quaternion.Euler(0, 0, Inst.pogoR);

		if (!Hc) return;

		var charge = Moves.ChargedSlash!.GameObject!.transform;
		charge.SetScale2D(new(Inst.chargeSX, Inst.chargeSY));
		charge.SetLocalPositionAndRotation(
			new(Inst.chargePX, Inst.chargePY, 0),
			Quaternion.Euler(0, 0, Inst.chargeR)
		);
	}

	const string ALT_WALL_SLASH_EVENT = "TRAVELLER LUNGE";

	static readonly Vector2[] JUST_ATTACK_HITBOX = [
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
	static HeroController.ConfigGroup GetCrest(string name)
		=> Hc.configs.First(x => x.Config.name == name);

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

		Moves.OnInitialized += SimpleAttackInit;
		static void SimpleAttackInit() {
			var wanderer = GetCrest("Wanderer");
			Moves.Slash!.Sound = GetSound(wanderer.NormalSlashObject);
			Moves.AltSlash!.Sound = GetSound(wanderer.AlternateSlashObject);
			Moves.UpSlash!.Sound = GetSound(wanderer.UpSlashObject);
			Moves.WallSlash!.Sound = GetSound(wanderer.WallSlashObject);
		}
	}

	#region Alt Wall Slash

	[HarmonyPatch(typeof(HeroController), nameof(HeroController.Attack))]
	[HarmonyPostfix]
	private static void AltWallSlash(HeroController __instance, AttackDirection attackDir) {
		if (!SifCrest.IsEquipped)
			return;

		var hc = __instance;
		HeroActions input = hc.inputHandler.inputActions;

		bool
			wallSliding = hc.cState.wallSliding || hc.cState.wallScrambling,
			pressingTowardWall =
				(hc.touchingWallL && input.Left.IsPressed)
				|| (hc.touchingWallR && input.Right.IsPressed);

		if (wallSliding && !pressingTowardWall && attackDir == AttackDirection.normal) {
			Moves.WallSlash!.CancelAtk();
			Moves.WallSlash!.Get<AudioSource>().Stop(true);
			hc.wallSlashing = false;
			hc.SlashComponent = null;
			hc.currentSlashDamager = null;
			hc.sprintFSM.Fsm.Event(ALT_WALL_SLASH_EVENT);
		}
	}
	
	#endregion

	#region Down Slash

	private static void DownSlash() {
		Config.SetDownspikeFields(recoveryTime: 0.24f);
		Config.SetCustomDownslash("TRAVELLER DOWNSLASH", DownslashEdit);

		Moves.DownSlash = new DownAttackPositionable {
			Name = "Down",
			AnimLibrary = AnimationManager.MainLib,
			AnimName = "Slash_Charged Effect",
			Hitbox = JUST_ATTACK_HITBOX,
			Transform = new() {
				Position = new(0.05f, -0.15f),
				Rotation = Quaternion.Euler(0, 0, 40),
			},
			KeepWorldPosition = true,
		};

		Moves.OnInitialized += DownAttackInit;
		static void DownAttackInit() {
			Moves.DownSlash!.Sound = GetSound(GetCrest("Shaman").DownSlashObject);
		}
	}

	private static void DownslashEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault ownerHornet = new();

		FsmState
			slashState = fsm.AddState($"{SifId} Downslash"),
			missState = fsm.AddState($"{SifId} End"),
			bounceState = fsm.AddState($"{SifId} Bounce"),
			dashCancelState = fsm.AddState($"{SifId} Dash Cancel");

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
		slashState.AddTransitions(
			(bounceState, ["ATTACK LANDED", "BOUNCE TINKED", "BOUNCE CANCEL"]),
			(missState, ["LEAVING SCENE"]),
			(missState, [FsmEvent.Finished.name])
		);
		AddDashCancel(slashState);

		missState.AddMethod(() => {
			Hc.cState.downAttacking = false;
			Hc.FinishDownspike();
		});

		bounceState.AddMethod(() => {
			Hc.rb2d.linearVelocity = Vector2.zero;
			Hc.SetStartWithDownSpikeBounce();
		});

		dashCancelState.AddMethod(() => {
			Moves.DownSlash!.GameObject!.SendMessage(nameof(NailSlash.CancelAttack));
			Hc.SetStartWithDash();
		});

		endStates = [missState, bounceState, dashCancelState];

		void AddDashCancel(FsmState state) {
			FsmEvent dashEvent = FsmEvent.GetFsmEvent("SPRINT");
			state.AddAction(new ListenForDash {
				wasPressed = dashEvent,
				delayBeforeActive = 0f,
				BlocksFinish = false,
			});
			state.AddTransition(dashEvent.name, dashCancelState.name);
		}
	}

	#endregion

	#region Dash Slash

	static DashAttack.Step DashSlashMain => Moves.DashSlash!.Steps[0];
	static DashAttack.Step DashSlashLunge => Moves.DashSlash!.Steps[1];

	private static void DashSlash() {
		Config.DashSlashFsmEdit = DashSlashFsmEdit;

		Moves.DashSlash = new DashAttack {
			Name = "Dash",
			Steps = [
				new DashStepPositionable {
					AnimName = "Slash_Charged Effect",
					Hitbox = JUST_ATTACK_HITBOX,
					Transform = new() {
						Position = new(-5, 0, 0),
					},
					Scale = new(-1, 0.8f),
					KeepWorldPosition = true,
					StunDamage = STUN_DAMAGE,
				},
				new DashStepPositionable {
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

		Moves.OnInitialized += DashAttackInit;
		static void DashAttackInit() {
			DashSlashMain.Sound = GetSound(GetCrest("Shaman").NormalSlashObject);
			DashSlashLunge.Sound = GetSound(GetCrest("Default").DownSlashObject);

			foreach (var step in Moves.DashSlash!.Steps) {
				var de = step.Get<DamageEnemies>();
				de.dealtDamageFSM = Hc.sprintFSM;
				de.dealtDamageFSMEvent = "DASH HIT";
			}
		}
	}

	private static void DashSlashFsmEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault ownerHornet = new();

		FsmState
			continueSprintState = fsm.GetState("Continue Sprint?")!,
			bonkState = fsm.GetState("Bonk")!,
			slashState = fsm.AddState($"{SifId} Slash"),
			dashCancelState = fsm.AddState($"{SifId} Dash Cancel"),
			jumpCancelState = fsm.AddState($"{SifId} Jump Cancel"),
			recoveryState = fsm.AddState($"{SifId} End"),
			altWallSlashState = fsm.AddState($"{SifId} Alt Wall Slash"),
			lungeAnticState = fsm.AddState($"{SifId} Followup Antic"),
			lungeSlashState = fsm.AddState($"{SifId} Followup Slash"),
			lungeMissState = fsm.AddState($"{SifId} Followup Miss"),
			lungeBounceState = fsm.AddState($"{SifId} Followup Bounce");

		float lungeInputDelay = 0.04f;

		#region Craft attack + leap back

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
			}
		);
		startState.AddTransition(FsmEvent.Finished.name, slashState.name);

		slashState.AddMethod(() => {
			Hc.cState.onGround = false;
			Hc.AffectedByGravity(false);
			DashSlashMain.StartAtk();
		});
		slashState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Dash Attack",
				animationCompleteEvent = FsmEvent.Finished,
			},
			new SetVelocityByScale {
				gameObject = ownerHornet,
				speed = 12,
				ySpeed = 18,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.9f,
			},
			new ListenForAttackV2 {
				IsActive = true, queueBool = false,
				WasPressed = FsmEvent.GetFsmEvent("ATTACK"),
				DelayBeforeActive = lungeInputDelay,
			},
			new ListenForDashV2 {
				IsActive = true,
				WasPressed = FsmEvent.GetFsmEvent("DASH"),
				DelayBeforeActive = lungeInputDelay,
			},
			new ListenForJumpV2 {
				activeBool = true, queueBool = false, isPressedBool = false,
				wasPressed = FsmEvent.GetFsmEvent("JUMP"),
				delayBeforeActive = lungeInputDelay,
			}
		);
		slashState.AddTransitions(
			(recoveryState, FsmEvent.Finished.name),
			(bonkState, "DAMAGER TINKED"),
			(lungeAnticState, "ATTACK"),
			(dashCancelState, "DASH"),
			(jumpCancelState, "JUMP")
		);

		dashCancelState.AddMethod(Hc.SetStartWithDash);

		jumpCancelState.AddMethod(() => {
			if (Hc.playerData.hasDoubleJump)
				Hc.SetStartWithDoubleJump();
			else if (Hc.playerData.hasBrolly)
				Hc.SetStartWithBrolly();
		});

		recoveryState.AddMethod(() => {
			Hc.SetStartFromReaperUpperslash();
			Hc.CrestAttackRecovery();
			Hc.AffectedByGravity(true);
			Hc.SetAllowRecoilWhileRelinquished(false);
		});
		recoveryState.AddTransition(FsmEvent.Finished.name, continueSprintState.name);

		#endregion

		#region Alt Wall Slash

		altWallSlashState.AddMethod(() => {
			Hc.SetAllowNailChargingWhileRelinquished(false);
			Hc.SetAllowRecoilWhileRelinquished(true);
			Hc.RelinquishControlNotVelocity();
			Hc.CancelWallsliding();

			if (Hc.cState.facingRight)
				Hc.FaceLeft();
			else
				Hc.FaceRight();

			DashSlashLunge.Get<DamageEnemies>().SetDirectionByHeroFacing();
		});
		altWallSlashState.AddAction(new NextFrameEvent());
		altWallSlashState.AddTransition(FsmEvent.Finished.name, lungeAnticState.name);

		fsm.AddGlobalTransition(ALT_WALL_SLASH_EVENT, altWallSlashState.name);

		#endregion

		#region Lunging followup attack

		lungeAnticState.AddMethod(() => {
			Hc.rb2d.linearVelocity = Vector2.zero;
			Hc.attackAudioTable.SpawnAndPlayOneShot(Hc.transform.position);
		});
		lungeAnticState.AddActions(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Wanderer RecoilStab",
				animationTriggerEvent = FsmEvent.Finished,
			}
		);
		lungeAnticState.AddTransition(FsmEvent.Finished.name, lungeSlashState.name);

		lungeSlashState.AddMethod(() => {
			DashSlashLunge.StartAtk();
			Hc.audioCtrl.PlaySound(HeroSounds.DASH);
			Hc.StartDownspikeInvulnerability();
		});
		lungeSlashState.AddActions(
			new SetVelocityByScale {
				gameObject = ownerHornet,
				speed = -48,
				ySpeed = -16,
			},
			new DecelerateV2 {
				gameObject = ownerHornet,
				deceleration = 0.96f,
			},
			new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationCompleteEvent = FsmEvent.Finished,
			}
		);
		lungeSlashState.AddTransitions(
			(lungeMissState, FsmEvent.Finished.name),
			(bonkState, "DAMAGER TINKED"),
			(lungeBounceState, "DASH HIT")
		);

		lungeMissState.AddMethod(() => {
			Hc.CrestAttackRecovery();
			Hc.AffectedByGravity(true);
			Hc.SetAllowRecoilWhileRelinquished(true);
		});
		lungeMissState.AddTransition(FsmEvent.Finished.name, continueSprintState.name);

		lungeBounceState.AddMethod(() => {
			Hc.rb2d.linearVelocity = Vector2.zero;
			Hc.SetStartWithDownSpikeBounce();
			Hc.CrestAttackRecovery();
			Hc.AffectedByGravity(true);
			Hc.SetAllowRecoilWhileRelinquished(false);
			DashSlashLunge.CancelAtk();
		});

		#endregion

		endStates = [jumpCancelState, dashCancelState, lungeBounceState];
	}

	#endregion

	#region Charged Slash

	static ChargedAttack.Step[] ChargeSteps => Moves.ChargedSlash!.Steps;

	private static void ChargedSlash() {
		Config.ChargedSlashFsmEdit = ChargedSlashFsmEdit;

		float knockback = 0.1f, damage = 0.6667f;

		Moves.ChargedSlash = new ChargedAttack {
			Name = "Charged",
			PlayOnActivation = false,
			PlayStepsInSequence = false,
			ScreenFlashColors = [new(1, 1, 1, 0.4f)],
			Steps = [
				new ChargeStepPositionable {
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
				new ChargeStepPositionable {
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
				new ChargeStepPositionable {
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

		Moves.OnInitialized += ChargedAttackInit;
		static void ChargedAttackInit() {
			Moves.ChargedSlash!.CameraShakeProfiles = [Camera.TinyShake, Camera.EnemyKillShake];

			var sound = GetCrest("Shaman").ChargeSlash.GetComponent<PlayRandomAudioEvent>()
				.audioEvent.Clips.FirstOrDefault(x => x.name == "hornet_shaman_needle_art");
			foreach (var step in ChargeSteps)
				step.Sound = sound;
			RefreshSettings();
		}
	}

	private static void ChargedSlashFsmEdit(PlayMakerFSM fsm, FsmState startState, out FsmState[] endStates) {
		FsmOwnerDefault ownerHornet = new();

		FsmState
			slashState = fsm.AddState($"{SifId} Attack Starting"),
			recoveryState = fsm.AddState($"{SifId} Recovery");

		FsmState[] chain = [
			.. ChargeSteps.Select((x, i) => fsm.AddState($"{SifId} Step {i}")),
			recoveryState
		];

		fsm.GetState("Cancel All")!.AddMethod(Hc.AllowRecoil);

		startState.AddMethod(() => {
			Hc.SpriteFlash.flashFocusHeal();
			Moves.ChargedSlash!.GameObject!.SetActive(true);
			foreach (var step in ChargeSteps)
				step.CancelAtk();
		});
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
			}
		);
		startState.AddTransition(FsmEvent.Finished.name, slashState.name);

		slashState.AddMethod(() => {
			Hc.PreventRecoil(
				AnimationManager.MainLib.GetClipByName("Slash_Charged").Duration
			);
			fsm.FindBoolVariable("Is Anim Finished")!.Value = false;
		});
		slashState.AddAction(
			new Tk2dPlayAnimationWithEvents {
				gameObject = ownerHornet,
				clipName = "Slash_Charged",
				animationTriggerEvent = FsmEvent.Finished,
			}
		);
		slashState.AddTransition(FsmEvent.Finished.name, chain[0].name);

		for (int i = 0; i < chain.Length - 1; i++) {
			var step = ChargeSteps[i];
			chain[i].AddMethod(step.StartAtk);
			chain[i].AddAction(new Tk2dWatchAnimationEvents {
				gameObject = ownerHornet,
				animationTriggerEvent = FsmEvent.Finished,
				animationCompleteEvent = FsmEvent.Finished,
			});
			chain[i].AddTransition(FsmEvent.Finished.name, chain[i + 1].name);
		}

		recoveryState.AddMethod(Hc.SetStartWithDownSpikeEnd);

		endStates = [recoveryState];
	}

	#endregion

}
