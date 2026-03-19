namespace TravellerCrest.Attacks;

internal class DownAttackTravelling : DownAttackPositionable {
	public NailTravelProxy? Travel { get; set; }

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Travel?.Initialize(GameObject!);
	}
}
