using Needleforge.Attacks;
using TravellerCrest.Components;

namespace TravellerCrest.Attacks;

internal class AttackPositionable : Attack {
	public TransformProxy? Transform { get; set; }
	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Transform?.Initialize(GameObject!);
	}
}
