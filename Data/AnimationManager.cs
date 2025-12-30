using Newtonsoft.Json;
using Silksong.UnityHelper.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TravellerCrest.Components;
using TravellerCrest.Utils;
using UnityEngine;
using UObject = UnityEngine.Object;
using WrapMode = tk2dSpriteAnimationClip.WrapMode;

namespace TravellerCrest.Data;

internal static class AnimationManager {
	internal static readonly tk2dSpriteCollectionData spriteCollection;
	internal static readonly tk2dSpriteAnimation library;

	private const string path = $"{nameof(TravellerCrest)}.Assets.Animations";

	private static readonly HeroAnimDef[] heroDefs;

	static AnimationManager() {
		Assembly asm = Assembly.GetExecutingAssembly();

		GameObject libobj = new($"{SifId} Anim");
		UObject.DontDestroyOnLoad(libobj);
		libobj.hideFlags = HideFlags.HideAndDontSave;

		library = libobj.AddComponent<tk2dSpriteAnimation>();
		library.clips = [];

		#if DEBUG
		libobj.AddComponent<RedoTriggers>();
		#endif

		#region Custom Animations

		using (StreamReader reader = new(asm.GetManifestResourceStream($"{path}.CustomSpriteDefs.json"))) {
			CustomSpriteDef[] frameDatas = JsonConvert.DeserializeObject<CustomSpriteDef[]>(reader.ReadToEnd())!;

			IEnumerable<Texture2D> frames = frameDatas.Select(x =>
				SpriteUtil.LoadEmbeddedTexture(asm, $"{path}.{x.Path}").PremultiplyAlpha()
			);

			spriteCollection = Tk2dUtil.CreateTk2dSpriteCollection(
				frames,
				spriteNames: frameDatas.Select(x => x.Path),
				spriteCenters: frameDatas.Select(x => x.Pivot)
			);
		}
		UObject.DontDestroyOnLoad(spriteCollection);
		spriteCollection.hideFlags = HideFlags.HideAndDontSave;
		spriteCollection.gameObject.name = $"{SifId} Cln";

		CustomAnimDef[] animDatas;
		using (StreamReader reader = new(asm.GetManifestResourceStream($"{path}.CustomAnimDefs.json"))) {
			animDatas = JsonConvert.DeserializeObject<CustomAnimDef[]>(reader.ReadToEnd())!;
		}

		List<tk2dSpriteAnimationClip> clips = [];

		foreach (CustomAnimDef animData in animDatas) {
			tk2dSpriteAnimationClip anim = new() {
				name = animData.Name,
				fps = animData.Fps,
				wrapMode = animData.WrapMode,
				frames = spriteCollection.CreateFrames(animData.Frames),
			};
			foreach (int index in animData.Triggers) {
				anim.frames[index].triggerEvent = true;
			}
			clips.Add(anim);
		}

		library.clips = [.. clips];

		#endregion

		using (StreamReader reader = new(asm.GetManifestResourceStream($"{path}.HeroAnimDefs.json"))) {
			heroDefs = JsonConvert.DeserializeObject<HeroAnimDef[]>(reader.ReadToEnd())!;
		}
	}

	#region Hero Animations

	private static bool hasSetupHero = false;

	public static void SetupHeroAnims(HeroController hc) {
		if (hasSetupHero)
			return;
		hasSetupHero = true;

		List<tk2dSpriteAnimationClip> heroClips = [];

		foreach(HeroAnimDef def in heroDefs) {
			List<tk2dSpriteAnimationFrame> frames = [];

			if (def.CopyEntire is HeroFrameDef source) {
				var sourceClip = GetSourceClip(source, hc);
				frames = [.. sourceClip.frames.Select(CopyFrame)];
				for (int i = 0; i < sourceClip.frames.Length; i++) {
					frames[i].triggerEvent = sourceClip.frames[i].triggerEvent;
				}
				heroClips.Add(new() {
					name = string.IsNullOrEmpty(def.Name) ? sourceClip.name : def.Name,
					fps = sourceClip.fps,
					wrapMode = sourceClip.wrapMode,
					frames = [.. frames]
				});
			}
			else if (def.Composite is HeroFrameDef[] frameDefs) {
				foreach (HeroFrameDef frame in frameDefs) {
					tk2dSpriteAnimationClip sourceClip = GetSourceClip(frame, hc);

					int start = frame.Frame ?? frame.Start ?? 0,
						end = frame.Frame ?? frame.End ?? sourceClip.frames.Length - 1,
						repeats = frame.Repeat ?? 1;

					end++; // so end of the range is inclusive in the json

					for (int i = 0; i < repeats; i++)
						frames.AddRange(sourceClip.frames[start..end].Select(CopyFrame));
				}
				foreach (int index in def.Triggers) {
					frames[index].triggerEvent = true;
				}
				heroClips.Add(new() {
					name = def.Name,
					fps = def.Fps,
					wrapMode = def.WrapMode,
					frames = [.. frames]
				});
			}
		}

		library.clips = [..library.clips, ..heroClips];
		library.isValid = false;
		library.ValidateLookup();
	}

	private static tk2dSpriteAnimationClip GetSourceClip(HeroFrameDef frameDef, HeroController hc)
		=> (frameDef.Crest switch {
			"Cloakless" or "Cursed" or "Reaper" or "Spell" or "Toolmaster" or
			"Wanderer" or "Warrior" or "Witch"
				=> ToolItemManager.GetCrestByName(frameDef.Crest).HeroConfig
					.heroAnimOverrideLib,
			"Hunter" or _
				=> hc.animCtrl.animator.Library
		}).GetClipByName(frameDef.Name);

	private static tk2dSpriteAnimationFrame CopyFrame(tk2dSpriteAnimationFrame source)
		=> new() {
			spriteCollection = source.spriteCollection,
			spriteId = source.spriteId
		};

	/// <summary>
	/// Deserialization struct describing an animation either copied or composited from a
	/// vanilla Hornet animation.
	/// </summary>
	/// <param name="Name">A name for the new animation.</param>
	/// <param name="Triggers">
	///		A list of frame indexes which should trigger an event.
	/// </param>
	/// <param name="CopyEntire">
	///		An animation that will be copied directly, sans triggers.
	///	</param>
	/// <param name="Composite">
	///		A series of single frames, or ranges of frames, from one or more animations
	///		which will be chained together to form the final animation.
	///	</param>
	[Serializable]
	private record struct HeroAnimDef(
		string Name, int Fps, WrapMode WrapMode, int[] Triggers,
		HeroFrameDef? CopyEntire, HeroFrameDef[] Composite);

	/// <summary>
	/// Deserialization struct describing one of, or a section of one of, Hornet's animations.
	/// </summary>
	/// <param name="Name">Name of an animation in one of Hornet's libraries.</param>
	/// <param name="Crest">
	///		Name of the vanilla <see cref="ToolCrest"/> the animation belongs to.
	///		Null = the default/Hunter library.
	///	</param>
	/// <param name="Frame">Index of a single frame to copy.</param>
	/// <param name="Start">Start (inclusive) of a range of frames to copy.</param>
	/// <param name="End">End (inclusive) of a range of frames to copy.</param>
	/// <param name="Repeat">
	///		How many times the frame/sequence of frames being copied should be repeated.
	///		Defaults to 1.
	///	</param>
	[Serializable]
	private record struct HeroFrameDef(
		string Name, string Crest, int? Frame, int? Start, int? End, int? Repeat);

	#endregion

	#region Custom Animations

	/// <summary>
	/// Deserialization struct for an entry in a <see cref="tk2dSpriteCollectionData"/>.
	/// </summary>
	/// <param name="Path">
	///		Embedded resource path (relative to <see cref="path"/> of the sprite.
	///	</param>
	/// <param name="Pivot">Pivot/center point of the sprite, in pixels.</param>
	[Serializable]
	private record struct CustomSpriteDef(string Path, Vector2 Pivot);

	/// <summary>
	/// Deserialization struct for a custom animation.
	/// </summary>
	/// <param name="Frames">
	///		List of <see cref="CustomSpriteDef.Path"/>s of the frames of this animation.
	///	</param>
	/// <param name="Triggers">
	///		A list of frame indexes which should trigger an event.
	///	</param>
	[Serializable]
	private record struct CustomAnimDef(
		string Name, int Fps, WrapMode WrapMode, string[] Frames, int[] Triggers);

	#endregion

}
