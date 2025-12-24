using Needleforge.Attacks;

namespace TravellerCrest.Data;

internal class TravellingChargeAttackStep : ChargedAttack.Step {
	public AttackTravel? Travel { get; set; }
	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Travel?.Initialize(GameObject!);
	}
}
