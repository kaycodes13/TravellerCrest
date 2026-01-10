using Needleforge.Attacks;
using UnityEngine;

namespace TravellerCrest.Attacks;

internal class DashAttackStepPositionable : DashAttack.Step {

	#region API

	public bool KeepWorldPosition {
		get => _keepPos;
		set {
			_keepPos = value;
			if (GameObject) keepWorldPosition!.enabled = value;
		}
	}
	private bool _keepPos = false;

	public TransformProxy? Transform { get; set; }

	/// <inheritdoc cref="AttackBase.AnimName"/>
	/// <remarks>
	/// Effect animations for these attacks should not loop, and must have <b>two</b>
	/// frames which trigger animation events; these frames determine when the attack's
	/// hitbox is enabled and disabled.
	/// </remarks>
	public override string AnimName {
		get => _animName;
		set {
			_animName = value;
			if (GameObject) nailSlash!.animName = value;
		}
	}
	private string _animName = "";

	#endregion

	protected KeepWorldPosition? keepWorldPosition;
	protected NailSlash? nailSlash;
	protected override NailAttackBase? NailAttack => nailSlash;

	protected override void AddComponents(HeroController hc) {
		base.AddComponents(hc);
		nailSlash = GameObject!.AddComponent<NailSlash>();

		keepWorldPosition = GameObject!.AddComponent<KeepWorldPosition>();
	}

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Transform?.Initialize(GameObject!);

		Object.Destroy(dashStab);

		nailSlash!.animName = AnimName;
		Collider!.enabled = false;

		keepWorldPosition!.getPositionOnEnable
			= keepWorldPosition.resetOnDisable
			= keepWorldPosition!.keepScaleX
			= keepWorldPosition!.keepScaleY
			= keepWorldPosition!.keepX
			= keepWorldPosition!.keepY
			= true;
		keepWorldPosition!.enabled = false;

		nailSlash!.AttackStarting += () => {
			keepWorldPosition.enabled = false;
			keepWorldPosition.enabled = KeepWorldPosition;
		};
	}
}
