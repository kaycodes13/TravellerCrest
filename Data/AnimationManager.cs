using HarmonyLib;
using Silksong.UnityHelper.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using TravellerCrest.Components;
using TravellerCrest.Utils;
using UnityEngine;
using UObject = UnityEngine.Object;
using WrapMode = tk2dSpriteAnimationClip.WrapMode;

namespace TravellerCrest.Data;

[HarmonyPatch]
internal static class AnimationManager {

	internal static readonly tk2dSpriteCollectionData spriteCollection;

	internal static readonly Dictionary<string, tk2dSpriteAnimation> libraries = [];

	internal static tk2dSpriteAnimation MainLib => libraries["main"];

	private const string path = $"{nameof(TravellerCrest)}.Assets.Animations";

	private static bool hasSetupHero = false;
	private static readonly AnimLibrary<HeroAnimDef>[] heroLibs;

	static AnimationManager() {
		var frameDatas = AssetUtil.ReadJson<CustomSpriteDef[]>($"{path}.CustomSpriteDefs.json");

		IEnumerable<Texture2D>
			frames = frameDatas.Select(x => AssetUtil.LoadTexture($"{path}.{x.Path}"));

		spriteCollection = Tk2dUtil.CreateTk2dSpriteCollection(
			frames,
			spriteNames: frameDatas.Select(x => x.Path),
			spriteCenters: frameDatas.Select(x => x.Pivot)
		);
		UObject.DontDestroyOnLoad(spriteCollection);
		spriteCollection.hideFlags = HideFlags.HideAndDontSave;
		spriteCollection.gameObject.name = $"{SifId} Cln";

		//var mat = spriteCollection.material;
		//mat.shader = GlobalSettings.Effects.DefaultUnlitMaterial.shader;
		//mat.mainTexture = newtex;

		var animLibs = AssetUtil.ReadJson<AnimLibrary<CustomAnimDef>[]>($"{path}.CustomAnimDefs.json");

		foreach (var libData in animLibs)
			libData.Initialize();

		heroLibs = AssetUtil.ReadJson<AnimLibrary<HeroAnimDef>[]>($"{path}.HeroAnimDefs.json");
	}

	[HarmonyPatch(typeof(HeroController), nameof(HeroController.Awake))]
	[HarmonyPostfix]
	public static void SetupHeroAnims() {
		if (hasSetupHero)
			return;
		hasSetupHero = true;

		foreach (var libData in heroLibs)
			libData.Initialize();
	}


	/// <summary>
	/// Serializable group of animation definitions, which can be initialized to convert
	/// the definitions to <see cref="tk2dSpriteAnimationClip"/>s and add them to the
	/// appropriate <see cref="tk2dSpriteAnimation"/> library.
	/// </summary>
	[Serializable]
	private record struct AnimLibrary<T>(string Name, T[] Anims) where T : IAnimDef {
		internal readonly void Initialize() {
			if (!libraries.TryGetValue(Name, out var library)) {
				GameObject libobj = new($"{SifId} Anim {Name}");
				UObject.DontDestroyOnLoad(libobj);
				libobj.hideFlags = HideFlags.HideAndDontSave;

				libraries[Name] = library = libobj.AddComponent<tk2dSpriteAnimation>();
				library.clips = [];
				library.ValidateLookup();

				#if DEBUG
				libobj.AddComponent<RedoTriggers>();
				#endif
			}

			var newAnims = Anims
				.Where(x => !library.lookup.ContainsKey(x.Name))
				.Select(x => x.MakeClip());

			library.clips = [
				.. library.clips,
				.. newAnims
			];
			library.isValid = false;
			library.ValidateLookup();
		}
	};

	/// <summary>
	/// Common interface for serializable animation definitions.
	/// </summary>
	private interface IAnimDef {
		internal string Name { get; set; }

		/// <summary>
		/// Creates a <see cref="tk2dSpriteAnimationClip"/> from this definition.
		/// </summary>
		internal tk2dSpriteAnimationClip MakeClip();
	}

	#region Hero Animations

	/// <summary>
	/// Serializable definition for an animation either copied or composited from an
	/// existing Hornet animation, which can convert itself to a <see cref="tk2dSpriteAnimationClip"/>.
	/// </summary>
	/// <param name="Name">A name for the new animation.</param>
	/// <param name="Triggers">List of frame indexes which should trigger an event.</param>
	/// <param name="Copy">An animation that will be copied exactly, sans triggers.</param>
	/// <param name="Composite">
	///		A series of single frames, or ranges of frames, from one or more animations
	///		which will be chained together to form the final animation.
	///	</param>
	[Serializable]
	private record struct HeroAnimDef
		(string Name, int Fps, WrapMode WrapMode, int[] Triggers,
		HeroFrameDef? Copy, HeroFrameDef[] Composite)
		: IAnimDef {

		readonly string IAnimDef.Name {
			get => Name ?? Copy?.Name ?? "";
			set => _ = value;
		}

		readonly tk2dSpriteAnimationClip IAnimDef.MakeClip() {
			List<tk2dSpriteAnimationFrame> frames = [];

			if (Copy is HeroFrameDef source) {
				var sourceClip = GetSourceClip(source);

				frames = [.. sourceClip.frames.Select(CopyFrame)];
				for (int i = 0; i < sourceClip.frames.Length; i++)
					frames[i].triggerEvent = sourceClip.frames[i].triggerEvent;

				return new() {
					name = string.IsNullOrEmpty(Name) ? sourceClip.name : Name,
					fps = sourceClip.fps,
					wrapMode = sourceClip.wrapMode,
					frames = [.. frames]
				};
			}
			else if (Composite is HeroFrameDef[] frameDefs) {
				foreach (HeroFrameDef frame in frameDefs) {
					tk2dSpriteAnimationClip sourceClip = GetSourceClip(frame);

					int start = frame.Frame ?? frame.Start ?? 0,
						end = frame.Frame ?? frame.End ?? sourceClip.frames.Length - 1,
						repeats = frame.Repeat ?? 1;

					end++; // so end of the range is inclusive in the json

					for (int i = 0; i < repeats; i++)
						frames.AddRange(sourceClip.frames[start..end].Select(CopyFrame));
				}
				foreach (int index in Triggers)
					frames[index].triggerEvent = true;

				return new() {
					name = Name,
					fps = Fps,
					wrapMode = WrapMode,
					frames = [.. frames]
				};
			}
			throw new InvalidOperationException();
		}

		private static tk2dSpriteAnimationClip GetSourceClip(HeroFrameDef frameDef)
			=> (frameDef.Crest switch {
				"Cloakless" or "Cursed" or "Reaper" or "Spell" or "Toolmaster" or
				"Wanderer" or "Warrior" or "Witch"
					=> ToolItemManager.GetCrestByName(frameDef.Crest).HeroConfig
						.heroAnimOverrideLib,
				"Hunter" or _
					=> HeroController.instance.animCtrl.animator.Library
			}).GetClipByName(frameDef.Name);

		private static tk2dSpriteAnimationFrame CopyFrame(tk2dSpriteAnimationFrame source)
			=> new() {
				spriteCollection = source.spriteCollection,
				spriteId = source.spriteId
			};
	};

	/// <summary>
	/// Deserialization struct describing one of, or a section of one of, Hornet's animations.
	/// </summary>
	/// <param name="Name">Name of an animation in one of Hornet's libraries.</param>
	/// <param name="Crest">Name of the vanilla crest the animation belongs to.</param>
	/// <param name="Frame">Index of a single frame to copy.</param>
	/// <param name="Start">Start (inclusive) of a range of frames to copy.</param>
	/// <param name="End">End (inclusive) of a range of frames to copy.</param>
	/// <param name="Repeat">
	///		How many times the frame/sequence of frames being copied should be repeated.
	///		Defaults to 1.
	///	</param>
	[Serializable]
	private record struct HeroFrameDef
		(string Name, string Crest, int? Frame, int? Start, int? End, int? Repeat);

	#endregion

	#region Custom Animations

	/// <summary>
	/// Serializable definition for a custom animation, which can convert itself
	/// to a <see cref="tk2dSpriteAnimationClip"/>.
	/// </summary>
	/// <param name="Frames">
	///		List of <see cref="CustomSpriteDef.Path"/>s of the frames of this animation.
	///	</param>
	/// <param name="Triggers">List of frame indexes which should trigger an event.</param>
	[Serializable]
	private record struct CustomAnimDef
		(string Name, int Fps, WrapMode WrapMode, string[] Frames, int[] Triggers)
		: IAnimDef {

		readonly tk2dSpriteAnimationClip IAnimDef.MakeClip() {
			tk2dSpriteAnimationClip anim = new() {
				name = Name,
				fps = Fps,
				wrapMode = WrapMode,
				frames = spriteCollection.CreateFrames(Frames),
			};
			foreach (int index in Triggers) {
				anim.frames[index].triggerEvent = true;
			}
			return anim;
		}
	}

	/// <summary>
	/// Deserialization struct for an entry in a <see cref="tk2dSpriteCollectionData"/>.
	/// </summary>
	/// <param name="Path">
	///		Embedded resource path (relative to <see cref="path"/> of the sprite.
	///	</param>
	/// <param name="Pivot">Pivot/center point of the sprite, in pixels.</param>
	[Serializable]
	private record struct CustomSpriteDef(string Path, Vector2 Pivot);

	#endregion

}
