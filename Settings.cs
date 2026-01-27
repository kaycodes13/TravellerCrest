using BepInEx.Configuration;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static InventoryItemComboButtonPromptDisplay;

namespace TravellerCrest;

internal class Settings {

	private TravellerCrestPlugin plugin;

	internal ConfigEntry<float>
		toolDamageMultiplier,
		dropRateNormal,
		dropRateSnitch,
		dropRateDiceBonus,
		percentToolsRefilled,
		downAttackTimeGap,
		downAttackHitVel;

	internal Settings(TravellerCrestPlugin inst) {
		plugin = inst;

		plugin.Config.SaveOnConfigSet = true;

		toolDamageMultiplier = plugin.Config.Bind("Tool Damage", "Bonus Tool Damage Multiplier", defaultValue: 0.53f);

		dropRateNormal = plugin.Config.Bind("Tool Refills", "Tool Refill Base Drop Rate", defaultValue: 0.10f);
		dropRateSnitch = plugin.Config.Bind("Tool Refills", "Tool Refill Snitch Pick Drop Rate", defaultValue: 0.10f);
		dropRateDiceBonus = plugin.Config.Bind("Tool Refills", "Tool Refill Dice Bonus Multiplier", defaultValue: 1.10f);
		percentToolsRefilled = plugin.Config.Bind("Tool Refills", "Tool Refill Percentage", defaultValue: 0.10f);

		downAttackTimeGap = plugin.Config.Bind("Attacks", "Seconds Between Down Swipes", defaultValue: 0.01f);
		downAttackHitVel = plugin.Config.Bind("Attacks", "Velocity if First Down Swipe Hits", defaultValue: 20f);
	}

	private record struct MenuEltDef(ConfigEntry<float> setting, float min, float max, float step);

	internal AbstractMenuScreen BuildMenu() {
		List<ChoiceElement<float>> elts = [];
		MenuEltDef[] configs = [
			new(toolDamageMultiplier, 0, 10, 0.01f),
			new(dropRateNormal, 0, 1, 0.05f),
			new(dropRateSnitch, 0, 1, 0.05f),
			new(dropRateDiceBonus, 1, 10, 0.05f),
			new(percentToolsRefilled, 0, 1, 0.05f),
			new(downAttackTimeGap, 0, 0.5f, 0.01f),
			new(downAttackHitVel, 0, 80, 0.5f),
		];

		foreach(MenuEltDef def in configs) {
			ChoiceElement<float> elt = new(
				def.setting.LabelName(),
				new FloatRangeChoiceModel(def.min, def.max, def.step, def.setting.Value)
			);
			elt.OnValueChanged += value => {
				if (def.setting.Value != value)
					def.setting.Value = value;
			};
			def.setting.SettingChanged += (_, _) => {
				if (elt.Value != def.setting.Value)
					elt.Value = def.setting.Value;
			};
			elts.Add(elt);
		}

		TextButton resetButton = new("Reset to Default Settings") {
			OnSubmit = () => {
				for (int i = 0; i < configs.Length; i++)
					elts[i].Value = (float)configs[i].setting.DefaultValue;
			}
		};

		SimpleMenuScreen screen = new(plugin.ModMenuName());
		screen.AddRange([.. elts, resetButton]);
		return screen;
	}

	private class FloatRangeChoiceModel : AbstractValueModel<float>, IChoiceModel<float>, IValueModel<float>, IBaseValueModel, IDisplayable, IBaseChoiceModel {
		private float value;

		public float Min { get; private set; }
		public float Max { get; private set; }
		public float Step { get; private set; }
		private int Precision => Step.ToString("0.########").Split('.')[1].Length;

		public FloatRangeChoiceModel(float min, float max, float step, float value)
			=> ResetParamsInternal(min, max, step, value);

		private float ClampVal(float val) =>
			Mathf.Clamp(MathF.Round(Mathf.RoundToMultipleOf(val, Step), Precision), Min, Max);

		public override float GetValue() => value;

		public bool MoveLeft() {
			if (value <= Min)
				return false;

			value = ClampVal(value - Step);
			InvokeOnValueChanged();
			return true;
		}

		public bool MoveRight() {
			if (value >= Max)
				return false;

			value = ClampVal(value + Step);
			InvokeOnValueChanged();
			return true;
		}

		public override bool SetValue(float value) {
			if (this.value == value)
				return true;

			if (value < Min || value > Max)
				return false;

			this.value = ClampVal(value);
			InvokeOnValueChanged();
			return true;
		}

		public override string DisplayString()
			=> value.ToString($"#0.{new string('0', Precision)}");

		public void ResetParams(float min, float max, float step, float value) {
			float num = this.value;
			ResetParamsInternal(min, max, step, value);
			if (this.value != num)
				InvokeOnValueChanged();
		}

		private void ResetParamsInternal(float min, float max, float step, float value) {
			if (max < min) {
				throw new ArgumentException(string.Format("{0} ({1}) must be <= than {2} ({3})", "min", min, "max", max));
			}
			if (step > max - min) {
				throw new ArgumentException($"Step can't be bigger than the range");
			}

			Min = min;
			Max = max;
			Step = step;
			this.value = ClampVal(value);
		}
	}

}
