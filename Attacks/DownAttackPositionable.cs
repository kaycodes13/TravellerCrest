using Needleforge.Attacks;
using UnityEngine;

namespace TravellerCrest.Attacks;

internal class DownAttackPositionable : DownAttack {

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

	#endregion

	protected KeepWorldPosition? keepWorldPos;

	// Needed because we're destroying the HDA component this prop normally pulls from
	protected override NailAttackBase? NailAttack =>
		GameObject ? GameObject.GetComponent<NailAttackBase>() : null;

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);

		// responsible for causing auto-bounces when we don't want them
		if (GameObject!.TryGetComponent<HeroDownAttack>(out var hda))
			Object.DestroyImmediate(hda);

		Transform?.Initialize(GameObject!);
		KeepWorldPosition?.Initialize(GameObject!);
	}
}
