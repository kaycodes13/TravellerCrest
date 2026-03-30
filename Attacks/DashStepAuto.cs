using Needleforge.Attacks;
using TravellerCrest.Components;
using UnityEngine;

namespace TravellerCrest.Attacks;

internal class DashStepAuto : DashAttack.Step {
	protected override void AddComponents(HeroController hc) {
		base.AddComponents(hc);
		Object.DestroyImmediate(GameObject!.GetComponent<NailAttackBase>());
		dashStab = GameObject.AddComponent<DashStabAutoEnd>();
	}
}
