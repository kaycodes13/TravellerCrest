using Needleforge.Attacks;
using Silksong.UnityHelper.Extensions;
using System.Linq;
using TeamCherry.SharedUtils;
using TravellerCrest.Components;

namespace TravellerCrest.Attacks;

internal class DownAttackNonBouncing : DownAttack {
	public TransformProxy? Transform { get; set; }

	private NailSlash? ns;
	private PlayMakerFSM? reactionFsm;
	protected override NailAttackBase? NailAttack => ns;
	protected override void AddComponents(HeroController hc) {
		ns = GameObject!.GetOrAddComponent<NailSlash>();
		reactionFsm = GameObject!.AddComponent<PlayMakerFSM>();
	}
	protected override void LateInitializeComponents(HeroController hc) {
		Transform?.Initialize(GameObject!);
		ns!.animName = AnimName;
		Damager!.corpseDirection = new OverrideFloat {
			IsEnabled = true,
			Value = DirectionUtils.GetAngle(DirectionUtils.Down)
		};
		hc.InvokeNextFrame(() => {
			var scythe = hc.transform.Find("Attacks/Scythe/DownSlash New").gameObject;
			var scythefsm = scythe.GetComponent<PlayMakerFSM>();
			var template = scythefsm.fsmTemplate;
			reactionFsm!.SetFsmTemplate(template);
		});
	}
}
