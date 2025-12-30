using System.Reflection;
using UnityEngine;
using Silksong.UnityHelper.Util;

namespace TravellerCrest.Utils;

internal static class AssetUtil {

	public static readonly Assembly Asm = Assembly.GetExecutingAssembly();

	public static Sprite LoadSprite(string path, bool premultiply = true, Vector2? pivot = null, float ppu = 64) {
		Sprite sprite = SpriteUtil.LoadEmbeddedSprite(Asm, path, ppu, pivot);
		if (premultiply)
			PremultiplyAlpha(sprite.texture);
		return sprite;
	}

	public static Texture2D LoadTexture(string path, bool premultiply = true) {
		Texture2D tex = SpriteUtil.LoadEmbeddedTexture(Asm, path);
		if (premultiply)
			PremultiplyAlpha(tex);
		return tex;
	}

	public static Texture2D PremultiplyAlpha(Texture2D source) {
		Color32[] pixels = source.GetPixels32();

		for (int i = 0; i < pixels.Length; i++) {
			Color px = pixels[i];
			pixels[i] = new Color(px.r * px.a, px.g * px.a, px.b * px.a, px.a);
		}

		source.SetPixels32(pixels);
		source.Apply();
		return source;
	}

}
