using Needleforge.Attacks;
using UnityEngine;

namespace TravellerCrest.Attacks;

internal class DashAttackStepTravelling : DashAttack.Step {

	#region API

	public NailSlashTravelProxy? Travel { get; set; }

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
		nailSlash = GameObject!.AddComponent<NailSlash>();
	}

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Travel?.Initialize(GameObject!);

		Object.Destroy(dashStab);

		nailSlash!.animName = AnimName;
		Collider!.enabled = false;
	}
}
