using GlobalEnums;
using Silksong.UnityHelper.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TravellerCrest.Components;

internal class MeterCenterFill : MonoBehaviour {
	public float Value {
		get => _val;
		set {
			_val = value;
			UpdateMeter();
		}
	}
	private float _val = 0;

	public float Min {
		get => _mn;
		set {
			_mn = value;
			UpdateMeter();
		}
	}
	private float _mn = 0;

	public float Max {
		get => _mx;
		set {
			_mx = value;
			UpdateMeter();
		}
	}
	private float _mx = 1;

	public ValueToScale? valueToScale;

	public delegate Vector3 ValueToScale(float val, float minVal, float maxVal);

	private static Vector3 DefaultScaler(float val, float minVal, float maxVal)
		=> Vector3.one * ((val - minVal) / (maxVal - minVal));


	public Sprite Fill {
		get => _fill;
		set {
			_fill = value;
			if (fillGo) fillGo.SetSprite(value, SpriteMaskInteraction.VisibleInsideMask);
			if (backMaskGo) backMaskGo.SetSprite(value, SpriteMaskInteraction.VisibleInsideMask);
		}
	}
	private Sprite _fill;

	public Sprite FillMask {
		get => _fillMask;
		set {
			_fillMask = value;
			if (fillMaskGo) fillMaskGo.SetSprite(value);
		}
	}
	private Sprite _fillMask;

	public Sprite Backboard {
		get => _backboard;
		set {
			_backboard = value;
			if (backGo) backGo.SetSprite(value, SpriteMaskInteraction.VisibleInsideMask);
		}
	}
	private Sprite _backboard;

	public Sprite Line {
		get => _line;
		set {
			_line = value;
			if (lineGo) lineGo.SetSprite(value, SpriteMaskInteraction.None);
		}
	}
	private Sprite _line;

	private void UpdateMeter() {
		if (!fillMaskGo)
			return;

		Vector3 valueScale = (valueToScale ?? DefaultScaler).Invoke(Value, Min, Max);
		StopAllCoroutines();
		fillMaskGo.transform.ScaleTo(this, valueScale, 0.1f);
	}

	public PhysLayers Layer {
		get => _layr;
		set {
			_layr = value;
			if (didAwake) {
				gameObject.layer = (int)value;
				foreach (Transform t in transform.GetAllDescendants())
					t.gameObject.layer = (int)value;
			}
		}
	}
	private PhysLayers _layr = PhysLayers.UI;


	public string SortingLayer {
		get => _sortLayr;
		set {
			_sortLayr = value;
			if (didAwake) {
				gameObject.GetComponent<SortingGroup>().sortingLayerName = value;

				foreach (var r in transform.GetComponentsInDescendants<Renderer>())
					r.sortingLayerName = value;
				foreach (var sg in transform.GetComponentsInDescendants<SortingGroup>())
					sg.sortingLayerName = value;
			}
		}
	}
	private string _sortLayr = "Over";

	public int SortingOrder {
		get => _sortOrder;
		set {
			_sortOrder = value;
			if (TryGetComponent<SortingGroup>(out var sg))
				sg.sortingOrder = value;
		}
	}
	private int _sortOrder = default;


	private GameObject
		lineGo,
		fillMaskGo,
		fillGo,
		backMaskGo,
		backGo;

	private void Awake() {
		gameObject.AddSortingGroup(SortingLayer, SortingOrder);
		gameObject.layer = (int)Layer;

		lineGo =
			NewGO("Lineart", order: 3, components: typeof(SpriteRenderer))
			.SetSprite(Line, SpriteMaskInteraction.None);

		var fillGroup = NewGO("Fill Group").AddSortingGroup(SortingLayer, order: 1);

		var backGroup = NewGO("Backboard Group").AddSortingGroup(SortingLayer, order: 0);


		fillMaskGo =
			NewGO("Fill Mask", fillGroup, order: 1, components: typeof(SpriteMask))
			.SetSprite(FillMask);

		fillGo =
			NewGO("Fill", fillGroup, order: 0, components: typeof(SpriteRenderer))
			.SetSprite(Fill, SpriteMaskInteraction.VisibleInsideMask);


		backMaskGo =
			NewGO("Backboard Mask", backGroup, order: 1, components: typeof(SpriteMask))
			.SetSprite(Fill);

		backGo =
			NewGO("Backboard", backGroup, order: 0, components: typeof(SpriteRenderer))
			.SetSprite(Backboard, SpriteMaskInteraction.VisibleInsideMask);

	}

	private void Start() {
		gameObject.transform.localScale = Vector3.zero;
	}

	private GameObject NewGO(string name, GameObject? parent = null, int order = 0, params Type[] components) {
		GameObject go = new(name) { layer = (int)PhysLayers.UI };

		go.transform.parent = parent ? parent.transform : transform;
		go.ResetTransform();

		foreach (Type cType in components) {
			var c = go.AddComponent(cType);
			if (c is Renderer)
				go.SetRendererSort(SortingLayer, order);
		}

		return go;
	}

}

file static class Ext {

	public static GameObject AddSortingGroup(this GameObject go, string layer, int order) {
		var group = go.GetOrAddComponent<SortingGroup>();
		group.sortingLayerName = layer;
		group.sortingOrder = order;
		return go;
	}

	public static GameObject SetRendererSort(this GameObject go, string layer, int order) {
		if (go.TryGetComponent<Renderer>(out var r)) {
			r.sortingLayerName = layer;
			r.sortingOrder = order;
		}
		return go;
	}

	public static GameObject SetSprite(this GameObject go, Sprite sprite, SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None) {
		if (go.TryGetComponent<SpriteMask>(out var sm))
			sm.sprite = sprite;
		else if (go.TryGetComponent<SpriteRenderer>(out var sr)) {
			sr.sprite = sprite;
			sr.maskInteraction = maskInteraction;
		}
		return go;
	}

	public static GameObject ResetAllTransforms(this GameObject go) {
		ResetTransform(go);
		foreach (Transform child in go.transform)
			ResetAllTransforms(child.gameObject);
		return go;
	}
	public static GameObject ResetTransform(this GameObject go) {
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = Vector3.zero;
		return go;
	}

	public static IEnumerable<Transform> GetAllDescendants(this Transform transform) {
		List<Transform> descendants = [transform];
		foreach (Transform t in transform)
			descendants.AddRange(t.GetAllDescendants());
		return descendants;
	}

	public static IEnumerable<T> GetComponentsInDescendants<T>(this Transform transform) where T : Component {
		List<T> comps = [transform.GetComponent<T>()];
		foreach (Transform t in transform)
			comps.AddRange(t.GetComponentsInDescendants<T>());
		return comps;
	}

}

