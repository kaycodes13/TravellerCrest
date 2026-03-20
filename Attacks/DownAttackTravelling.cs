namespace TravellerCrest.Attacks;

internal class DownAttackTravelling : DownAttackPositionable {
	public NailTravelProxy? Travel {
		get => _travel;
		set { _travel = value; value?.TryInitialize(GameObject); }
	}
	private NailTravelProxy? _travel;

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		Travel?.Initialize(GameObject!);
	}
}
