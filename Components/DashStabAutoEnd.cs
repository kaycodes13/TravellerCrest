using Needleforge.Components;
using UnityEngine;

namespace TravellerCrest.Components;

internal class DashStabAutoEnd : DashStabWithOwnAnim {

	private int triggers = 0;

	public override void Awake() {
		base.Awake();
		AttackStarting += OnAnimStart;
		animator.AnimationEventTriggeredEvent += OnAnimTrigger;
		animator.AnimationCompletedEvent += OnAnimEnd;
	}

	private void OnAnimStart() => triggers = 0;

	private void OnAnimTrigger(tk2dSpriteAnimator a, tk2dSpriteAnimationClip c, int f) {
		triggers++;
		if (triggers > 1 && IsDamagerActive) {
			IsDamagerActive = false;
			if (ExtraDamager)
				ExtraDamager.SetActive(false);
			if (TryGetComponent<Collider2D>(out var poly))
				poly.enabled = false;
			EnemyDamager.EndDamage();
		}
	}

	private void OnAnimEnd(tk2dSpriteAnimator a, tk2dSpriteAnimationClip c)
		=> OnCancelAttack();
}
