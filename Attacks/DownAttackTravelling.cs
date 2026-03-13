namespace TravellerCrest.Attacks;

internal class DownAttackTravelling : DownAttackPositionable {
	public AttackTravelProxy? Travel { get; set; }

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Travel?.Initialize(GameObject!);
	}
}
