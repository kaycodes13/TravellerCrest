using BepInEx;
using BepInEx.Configuration;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TravellerCrest.Mechanics;
using UnityEngine;

namespace TravellerCrest;

public partial class TravellerCrestPlugin : BaseUnityPlugin, IModMenuCustomMenu {

	internal static TravellerCrestPlugin Inst = null!;

	internal class ConfigRange {
		public ConfigEntry<float> entry;
		public float min, max;
		public Action? onChange;

		public float Value => entry.Value;
		public void Reset() => entry.Value = (float)entry.DefaultValue;

		public static implicit operator float(ConfigRange me) => me.entry.Value;
	}

	internal ConfigRange
		toolDamageMultiplier,
		dropRateNormal,
		dropRateSnitch,
		dropRateDiceBonus,
		percentToolsRefilled,
		dashSX, dashSY, dashPX, dashPY, dashR,
		pogoSX, pogoSY, pogoPX, pogoPY, pogoR,
		chargeSX, chargeSY, chargePX, chargePY, chargeR;

	internal IEnumerable<ConfigRange> Settings =>
		typeof(TravellerCrestPlugin)
			.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(x => x.FieldType == typeof(ConfigRange))
			.Select(x => (ConfigRange)x.GetValue(this));

	private ConfigRange BindFloat(string section, string key, float defaultValue, float min, float max, Action? onChange = null)
		=> new() {
			entry = Config.Bind(section, key, defaultValue),
			min = min,
			max = max,
			onChange = onChange,
		};

	public TravellerCrestPlugin() {
		Inst = this;
		Config.SaveOnConfigSet = true;

		toolDamageMultiplier = BindFloat("Tool Damage", "Bonus Damage Multiplier", 0.53f, 0, 10);

		dropRateNormal = BindFloat("Tool Refills", "Base Drop Rate", 0.10f, 0, 1);
		dropRateSnitch = BindFloat("Tool Refills", "Snitch Pick Drop Rate", 0.10f, 0, 1);
		dropRateDiceBonus = BindFloat("Tool Refills", "Dice Bonus Multiplier", 1.10f, 1, 10);
		percentToolsRefilled = BindFloat("Tool Refills", "Refill Percentage", 0.10f, 0, 1);

		var f = Moveset.RefreshSettings;

		dashSX = BindFloat("Dash Attack", "X Scale", -1, -5, 5, f);
		dashSY = BindFloat("Dash Attack", "Y Scale", 0.8f, -5, 5, f);
		dashPX = BindFloat("Dash Attack", "X Position", -5, -10, 10, f);
		dashPY = BindFloat("Dash Attack", "Y Position", 0, -10, 10, f);
		dashR = BindFloat("Dash Attack", "Rotation", 0, 0, 360, f);

		pogoSX = BindFloat("Pogo", "X Scale", 1, -5, 5, f);
		pogoSY = BindFloat("Pogo", "Y Scale", 1, -5, 5, f);
		pogoPX = BindFloat("Pogo", "X Position", 0.05f, -10, 10, f);
		pogoPY = BindFloat("Pogo", "Y Position", -0.15f, -10, 10, f);
		pogoR = BindFloat("Pogo", "Rotation", 40, 0, 360, f);

		chargeSX = BindFloat("Needle Strike", "X Scale", 1, -5, 5, f);
		chargeSY = BindFloat("Needle Strike", "Y Scale", 1, -5, 5, f);
		chargePX = BindFloat("Needle Strike", "X Position", 0, -10, 10, f);
		chargePY = BindFloat("Needle Strike", "Y Position", 0, -10, 10, f);
		chargeR = BindFloat("Needle Strike", "Rotation", 0, 0, 360, f);
	}

	public string ModMenuName() => Info.Metadata.Name.Replace('_', ' ');

	public AbstractMenuScreen BuildCustomMenu() {
		List<string> headers = [];
		List<MenuElement> elts = [];
		List<VerticalGroup> pages = [];

		VerticalGroup curPage = null!;

		static bool Unparse(float value, out string text) {
			text = $"{value:0.##}";
			return true;
		}

		foreach(ConfigRange def in Settings) {
			string section = def.entry.Definition.Section;
			if (!headers.Contains(section)) {
				headers.Add(section);
				curPage = new() { VerticalSpacing = SpacingConstants.VSPACE_MEDIUM };
				pages.Add(curPage);

				curPage.Add(new TextButton("Reset All to Default") {
					OnSubmit = () => {
						foreach (ConfigRange def in Settings)
							def.Reset();
					}
				});
				curPage.Add(new TextLabel("-----"));
				curPage.Add(new TextLabel(section));
			}

			TextInput<float> elt = new(
				def.entry.LabelName(),
				// default needs to be not 0 so that 0 shows up in the ui
				new ParserTextModel<float>(float.TryParse, Unparse, float.NaN), 
				$"Range: {def.min} - {def.max}"
			);
			elt.TextModel.SetValue(def.entry.Value);
			elt.OnValueChanged += value => {
				// model's constraint function did not actually constrain it?
				// perhaps i am simply a fool
				elt.Value = Mathf.Clamp(elt.Value, def.min, def.max);
				if (def.entry.Value != value) {
					def.entry.Value = value;
					def.onChange?.Invoke();
				}
			};
			def.entry.SettingChanged += (_, _) => {
				def.entry.Value = Mathf.Clamp(def.entry.Value, def.min, def.max);
				if (elt.Value != def.entry.Value) {
					elt.Value = def.entry.Value;
					def.onChange?.Invoke();
				}
			};
			curPage.Add(elt);
		}

		PaginatedMenuScreen screen = new(ModMenuName());
		screen.AddPages(pages);
		return screen;
	}

}
