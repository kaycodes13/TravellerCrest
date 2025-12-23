using UnityEngine;

namespace TravellerCrest.Data;

internal class RefillItem : SavedItem {

	public ToolItem tool;
	public int amountRefunded = 1;

	public override bool CanGetMore() => true;

	public override void Get(bool showPopup = true) {
		tool.CollectFree(amountRefunded);

		CollectableUIMsg.Spawn(new UIMsgDisplay {
			Name = GetPopupName(),
			Icon = GetPopupIcon(),
			IconScale = 1f,
			RepresentingObject = this,
		});
	}

	public override Sprite GetPopupIcon() => tool.GetPopupIcon();

	public override string GetPopupName() => tool.GetPopupName();
}
