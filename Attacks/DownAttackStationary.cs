using Silksong.UnityHelper.Extensions;

namespace TravellerCrest.Attacks;

internal class DownAttackStationary : DownAttackPositionable {

	protected KeepWorldPosition? keepPos;

	protected override void AddComponents(HeroController hc) {
		base.AddComponents(hc);
		keepPos = GameObject!.GetOrAddComponent<KeepWorldPosition>();
	}

	protected override void LateInitializeComponents(HeroController hc) {
		base.LateInitializeComponents(hc);
		keepPos!.getPositionOnEnable
			= keepPos.resetOnDisable
			= keepPos.keepX
			= keepPos.keepY
			= true;
		NailAttack!.AttackStarting += ResetKeptPos;

		void ResetKeptPos() {
			keepPos.OnDisable();
			keepPos.Initialise();
		}
	}

}
