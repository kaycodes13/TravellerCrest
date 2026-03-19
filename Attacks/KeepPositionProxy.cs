using UnityEngine;

namespace TravellerCrest.Attacks;

/// <summary>
/// Makes an attack hold in place when used, rather than following Hornet.
/// </summary>
internal class KeepPositionProxy : ComponentProxy<KeepWorldPosition> {

	/// <summary>
	/// Whether or not the attack will remain stationary.
	/// </summary>
	public bool Value {
		get => _keepPos;
		set {
			_keepPos = value;
			if (component) component.enabled = value;
		}
	}
	private bool _keepPos = false;

	protected override void Init() {
		component!.getPositionOnEnable
			= component.resetOnDisable
			= component.keepX
			= component.keepY
			= component.keepScaleX
			= component.keepScaleY
			= true;
		component.enabled = false;

		component.GetComponent<NailAttackBase>().AttackStarting += ResetKeptPos;

		void ResetKeptPos() {
			Debug.LogWarning($"WAGH {component.gameObject.name}");
			component.enabled = false;
			component.enabled = Value;
		}
	}

	public static implicit operator KeepPositionProxy(bool b) => new() { Value = b };
	public static implicit operator bool(KeepPositionProxy k) => k.Value;
}
