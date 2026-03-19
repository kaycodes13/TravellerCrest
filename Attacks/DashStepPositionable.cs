using Needleforge.Attacks;
using UnityEngine;

namespace TravellerCrest.Attacks;

internal class DashStepPositionable : DashAttack.Step {

	#region API

	public TransformProxy? Transform { get; set; }

	public KeepPositionProxy? KeepWorldPosition {
		get => _keepPos;
		set {
			_keepPos = value;
			if (GameObject) _keepPos?.Initialize(GameObject);
		}
	}
	private KeepPositionProxy? _keepPos;

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

	protected NailSlash? nailSlash;
	protected override NailAttackBase? NailAttack => nailSlash;

	protected override void AddComponents(HeroController hc) {
		base.AddComponents(hc);
		Object.DestroyImmediate(dashStab);
		nailSlash = GameObject!.AddComponent<NailSlash>();
	}

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);

		nailSlash!.animName = AnimName;
		Collider!.enabled = false;

		Transform?.Initialize(GameObject!);
		KeepWorldPosition?.Initialize(GameObject!);
	}
}
