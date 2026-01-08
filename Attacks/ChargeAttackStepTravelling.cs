using Needleforge.Attacks;

namespace TravellerCrest.Attacks;

internal class ChargeAttackStepTravelling : ChargedAttack.Step {
	public NailSlashTravelProxy? Travel { get; set; }
	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Travel?.Initialize(GameObject!);
	}
}
