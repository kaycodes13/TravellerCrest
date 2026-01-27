using Coffee.UISoftMask;
using GlobalEnums;
using Silksong.UnityHelper.Extensions;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TravellerCrest.Components;

/// <summary>
/// Attaching this to a <see cref="GameObject"/> builds out a <see cref="Canvas"/>
/// hierarchy representing a meter that fills via a sprite in its center growing outward.
/// </summary>
internal class MeterCenterFill : MonoBehaviour {
	/// <summary>
	/// Current value of the meter. Change at any point to change how full the meter is.
	/// </summary>
	public float Value {
		get => _val;
		set {
			_val = value;
			UpdateMeter();
		}
	}
	private float _val = 0;

	/// <summary>
	/// Minimum value of the meter. Default 0.
	/// </summary>
	public float Min {
		get => _mn;
		set {
			_mn = value;
			UpdateMeter();
		}
	}
	private float _mn = 0;

	/// <summary>
	/// Maximum value of the meter. Default 1.
	/// </summary>
	public float Max {
		get => _mx;
		set {
			_mx = value;
			UpdateMeter();
		}
	}
	private float _mx = 1;

	/// <summary>
	/// Optional function determining how the scale of the meter fill sprite is affected
	/// by the <see cref="Value"/>.
	/// By default, Value's position between <see cref="Min"/> and <see cref="Max"/>
	/// is mapped linearly to the range [0, 1].
	/// </summary>
	public ValueToScale? ValueToScaleFn { get; set; }

	public delegate float ValueToScale(float val, float minVal, float maxVal);

	/// <summary>
	/// Sprite to use for the fully-filled portion of the meter.
	/// This will also hard-mask the backboard to ensure everything fits together.
	/// </summary>
	public Sprite? Fill {
		get => _fill;
		set {
			_fill = value;
			if (fillGo) fillGo.GetComponent<Image>().sprite = value;
			if (backMaskGo) backMaskGo.GetComponent<Image>().sprite = value;
		}
	}
	private Sprite? _fill;

	/// <summary>
	/// Sprite used to soft-mask <see cref="Fill"/> when the meter is partially full.
	/// </summary>
	public Sprite? FillMask {
		get => _fillMask;
		set {
			_fillMask = value;
			if (fillMaskGo) fillMaskGo.GetComponent<Image>().sprite = value;
		}
	}
	private Sprite? _fillMask;

	/// <summary>
	/// Sprite to use for the back of the unfilled meter.
	/// </summary>
	public Sprite? Backboard {
		get => _backboard;
		set {
			_backboard = value;
			if (backGo) backGo.GetComponent<Image>().sprite = value;
		}
	}
	private Sprite? _backboard;

	/// <summary>
	/// Lineart sprite on top of the meter to hide its edge.
	/// </summary>
	public Sprite? Line {
		get => _line;
		set {
			_line = value;
			if (lineGo) lineGo.GetComponent<Image>().sprite = value;
		}
	}
	private Sprite? _line;

	private Coroutine? valueCoro;

	private GameObject
		lineGo,
		fillMaskGo,
		fillGo,
		backMaskGo,
		backGo;

	private void Awake() {
		var canvas = gameObject.GetOrAddComponent<Canvas>();
		canvas.worldCamera = GameManager.instance.gameCams.hudCamera;

		var canvScaler = gameObject.GetOrAddComponent<CanvasScaler>();
		canvScaler.referencePixelsPerUnit = 1;

		backMaskGo = NewGO("Backboard Mask", transform);
		AddImage(backMaskGo, Fill, maskable: false);
		backMaskGo.AddComponent<Mask>();

			backGo = NewGO("Backboard", backMaskGo.transform);
			AddImage(backGo, Backboard, maskable: true);

		fillMaskGo = NewGO("Fill Mask", transform);
		AddImage(fillMaskGo, FillMask, maskable: false);
		var fillmask = fillMaskGo.AddComponent<SoftMask>();
		fillmask.showMaskGraphic = false;
		fillMaskGo.AddComponent<SoftMaskFixer>();

			fillGo = NewGO("Fill", fillMaskGo.transform);
			AddImage(fillGo, Fill, maskable: true);
			fillGo.AddComponent<SoftMaskable>();

		lineGo = NewGO("Lineart", transform);
		AddImage(lineGo, Line, maskable: false);
	}

	private void Start() {
		gameObject.GetComponent<Canvas>().sortingLayerName = "Over";
	}

	private void UpdateMeter() {
		if (!fillMaskGo)
			return;

		float valueScale = (ValueToScaleFn ?? ValueToScaleLinear).Invoke(Value, Min, Max);

		var sizer = fillMaskGo.GetComponent<LockToPreferredSize>();

		if (Mathf.Approximately(valueScale, sizer.Scale))
			return;

		if (valueCoro != null)
			StopCoroutine(valueCoro);
		valueCoro = StartCoroutine(ScaleFill(0.1f, valueScale));
		
		IEnumerator ScaleFill(float duration, float finalScale) {
			float initialScale = sizer.Scale;
			for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime) {
				sizer.Scale = Mathf.Lerp(initialScale, finalScale, elapsed / duration);
				yield return null;
			}
			sizer.Scale = finalScale;
		}
	}

	private static float ValueToScaleLinear(float val, float minVal, float maxVal)
		=> (val - minVal) / (maxVal - minVal);

	private static GameObject NewGO(string name, Transform parent) {
		GameObject go = new(name) { layer = (int)PhysLayers.UI };
		go.transform.parent = parent;
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = Vector3.zero;
		return go;
	}

	private static void AddImage(GameObject go, Sprite? sprite, bool maskable) {
		var img = go.AddComponent<Image>();
		img.preserveAspect = true;
		img.maskable = maskable;
		img.sprite = sprite;
		go.AddComponent<LockToPreferredSize>();
	}

}
