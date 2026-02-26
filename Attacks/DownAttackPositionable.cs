using Needleforge.Attacks;
using UnityEngine;

namespace TravellerCrest.Attacks;

internal class DownAttackPositionable : DownAttack {
	public TransformProxy? Transform { get; set; }

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Transform?.Initialize(GameObject!);
		// responsible for causing auto-bounces when we don't want them
		if (GameObject!.TryGetComponent<HeroDownAttack>(out var hda))
			Object.DestroyImmediate(hda);
	}
}
