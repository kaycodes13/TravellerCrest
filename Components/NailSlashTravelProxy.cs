using Needleforge.Components;
using UnityEngine;

namespace TravellerCrest.Components;

/// <summary>
/// Makes an attack travel over time.
/// </summary>
internal class NailSlashTravelProxy : ComponentProxy<NailSlashTravel> {

	#region API

	public float GroundedYOffset {
		get => _yOffset;
		set {
			_yOffset = value;
			if (component) component!.groundedYOffset = value;
		}
	}
	private float _yOffset = 0;

	/// <summary>
	/// How far the attack will travel over its <see cref="Duration"/>.
	/// </summary>
	public Vector2 Distance {
		get => _travelDist;
		set {
			_travelDist = value;
			if (component) component!.travelDistance = value;
		}
	}
	private Vector2 _travelDist = Vector2.zero;

	public float RecoilDistance {
		get => _recoilDist;
		set {
			_recoilDist = value;
			if (component) component!.recoilDistance = value;
		}
	}
	private float _recoilDist = 0;

	/// <summary>
	/// How long the attack will travel for.
	/// </summary>
	public float Duration {
		get => _travelDuration;
		set {
			_travelDuration = value;
			if (component) component!.travelDuration = value;
		}
	}
	private float _travelDuration = 0;

	/// <summary>
	/// Curve modifying the attack's position over its <see cref="Duration"/>.
	/// Used to make it speed up and slow down at will.
	/// Min value should be 0 and max should be 1.
	/// </summary>
	public AnimationCurve Curve {
		get => _travelCurve;
		set {
			_travelCurve = value;
			if (component) component!.travelCurve = value;
		}
	}
	private AnimationCurve _travelCurve = AnimationCurve.Linear(0, 0, 1, 1);

	#endregion

	protected override void Init() {
		var nsWithEndEvent = GetOwnerComponent<NailSlashWithEndEvent>();
		var nsRegular = GetOwnerComponent<NailSlash>();

		component!.slash = nsWithEndEvent ? nsWithEndEvent : nsRegular;
		component!.damager = GetOwnerComponent<DamageEnemies>();

		component!.maxXOffset = new TeamCherry.SharedUtils.OverrideFloat();
		component!.maxYOffset = new TeamCherry.SharedUtils.OverrideFloat();

		component!.impactPrefab =
			component!.slash.hc.transform.Find("Attacks/Shaman/Slash")
			.GetComponent<NailSlashTravel>().impactPrefab;

		component!.groundedYOffset = GroundedYOffset;
		component!.travelDistance = Distance;
		component!.recoilDistance = RecoilDistance;
		component!.travelDuration = Duration;
		component!.travelCurve = Curve;
	}

}
