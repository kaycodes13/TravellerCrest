using Needleforge.Attacks;

namespace TravellerCrest.Attacks;

internal class ChargeAttackStepPositionable : ChargedAttack.Step {
	public TransformProxy? Transform { get; set; }
	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Transform?.Initialize(GameObject!);
	}
}
