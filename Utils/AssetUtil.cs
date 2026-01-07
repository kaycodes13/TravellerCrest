using Newtonsoft.Json;
using Silksong.UnityHelper.Util;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TravellerCrest.Utils;

internal static class AssetUtil {

	public static readonly Assembly Asm = Assembly.GetExecutingAssembly();

	public static T ReadJson<T>(string path) {
		T value;
		using (StreamReader reader = new(Asm.GetManifestResourceStream(path))) {
			value = JsonConvert.DeserializeObject<T>(reader.ReadToEnd())!;
		}
		return value;
	}

	public static Sprite LoadSprite(string path, float ppu = 64, Vector2? pivot = null, bool unreadable = true) {
		var sprite = SpriteUtil.LoadEmbeddedSprite(Asm, path, ppu, pivot).PremultiplyAlpha();
		if (unreadable)
			sprite.Unreadable();
		return sprite;
	}

	public static Texture2D LoadTexture(string path, bool unreadable = false) {
		var tex = SpriteUtil.LoadEmbeddedTexture(Asm, path).PremultiplyAlpha();
		if (unreadable)
			tex.MakeUnreadable();
		return tex;
	}

	public static Sprite Unreadable(this Sprite source) {
		source.texture.MakeUnreadable();
		return source;
	}

	public static Texture2D MakeUnreadable(this Texture2D source) {
		source.Apply(false, makeNoLongerReadable: true);
		return source;
	}

}
