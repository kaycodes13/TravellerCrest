using System.Reflection;
using UnityEngine;
using static Silksong.UnityHelper.Util.SpriteUtil;

namespace TravellerCrest.Utils;

internal static class AssetUtils {

	private static readonly Assembly asm = Assembly.GetExecutingAssembly();

	public static Sprite LoadSprite(string path, bool premultiply = true, Vector2? pivot = null, float ppu = 64) {

		Texture2D tex = LoadEmbeddedTexture(asm, path);

		if (premultiply)
			tex = tex.PremultiplyAlpha();

		Rect r = new(0f, 0f, tex.width, tex.height);
		Vector2 p = pivot ?? new(0.5f, 0.5f);

		return Sprite.Create(tex, r, p, ppu);
	}

	public static Texture2D PremultiplyAlpha(this Texture2D source) {
		Color32[] sourcePixels = source.GetPixels32();
		Color32[] destPixels = new Color32[sourcePixels.Length];

		for (int i = 0; i < sourcePixels.Length; i++) {
			Color px = sourcePixels[i];
			destPixels[i] = new Color(px.r * px.a, px.g * px.a, px.b * px.a, px.a);
		}

		Texture2D dest = new(source.width, source.height, TextureFormat.ARGB32, false);
		dest.SetPixels32(destPixels);
		dest.Apply();
		return dest;
	}

}
