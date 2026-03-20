using Needleforge.Attacks;
using TravellerCrest.Utils;
using UnityEngine;

namespace TravellerCrest.Attacks;

internal class DownAttackPositionable : DownAttack {

	#region API

	public TransformProxy? Transform {
		get => _transform;
		set { _transform = value; value?.TryInitialize(GameObject); }
	}
	private TransformProxy? _transform;

	public KeepPositionProxy? KeepWorldPosition {
		get => _keepPos;
		set { _keepPos = value; value?.TryInitialize(GameObject); }
	}
	private KeepPositionProxy? _keepPos;

	#endregion

	protected KeepWorldPosition? keepWorldPos;

	// Needed because we're destroying the HDA component this prop normally pulls from
	protected override NailAttackBase? NailAttack => this.Get<NailAttackBase>();

	protected override void AddComponents(HeroController hc) {
		base.AddComponents(hc);
		// responsible for causing auto-bounces when we don't want them
		if (this.TryGet<HeroDownAttack>(out var hda))
			Object.DestroyImmediate(hda);
	}

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Transform?.Initialize(GameObject!);
		KeepWorldPosition?.Initialize(GameObject!);
	}
}
