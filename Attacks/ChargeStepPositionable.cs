using Needleforge.Attacks;

namespace TravellerCrest.Attacks;

internal class ChargeStepPositionable : ChargedAttack.Step {
	public TransformProxy? Transform {
		get => _transform;
		set { _transform = value; value?.TryInitialize(GameObject); }
	}
	private TransformProxy? _transform;

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Transform?.Initialize(GameObject!);
	}
}
