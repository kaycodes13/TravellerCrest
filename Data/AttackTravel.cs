using Needleforge.Components;
using UnityEngine;

namespace TravellerCrest.Data;

/// <summary>
/// Makes an attack travel over time.
/// </summary>
internal class AttackTravel {

	#region API

	public float GroundedYOffset {
		get => _yOffset;
		set {
			_yOffset = value;
			if (nsTravel) nsTravel!.groundedYOffset = value;
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
			if (nsTravel) nsTravel!.travelDistance = value;
		}
	}
	private Vector2 _travelDist = Vector2.zero;

	public float RecoilDistance {
		get => _recoilDist;
		set {
			_recoilDist = value;
			if (nsTravel) nsTravel!.recoilDistance = value;
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
			if (nsTravel) nsTravel!.travelDuration = value;
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
			if (nsTravel) nsTravel!.travelCurve = value;
		}
	}
	private AnimationCurve _travelCurve = AnimationCurve.Linear(0, 0, 1, 1);

	#endregion

	private static GameObject ImpactRegular {
		get {
			if (!_impactRegular)
				_impactRegular = GameObject.Find("shaman_blade_impact");
			return _impactRegular;
		}
	}
	private static GameObject? _impactRegular;

	protected NailSlashTravel? nsTravel;

	internal void Initialize(GameObject owner) {
		if (!owner)
			throw new System.InvalidOperationException($"Owner {nameof(GameObject)} must exist at the time of {nameof(AttackTravel)} initialization.");

		nsTravel = owner.AddComponent<NailSlashTravel>();

		var nsWithEndEvent = owner.GetComponent<NailSlashWithEndEvent>();
		var nsRegular = owner.GetComponent<NailSlash>();

		nsTravel!.slash = nsWithEndEvent ? nsWithEndEvent : nsRegular;
		nsTravel!.damager = owner.GetComponent<DamageEnemies>();

		nsTravel!.maxXOffset = new TeamCherry.SharedUtils.OverrideFloat();
		nsTravel!.maxYOffset = new TeamCherry.SharedUtils.OverrideFloat();

		nsTravel!.impactPrefab = ImpactRegular;

		nsTravel!.groundedYOffset = GroundedYOffset;
		nsTravel!.travelDistance = Distance;
		nsTravel!.recoilDistance = RecoilDistance;
		nsTravel!.travelDuration = Duration;
		nsTravel!.travelCurve = Curve;
	}

}
