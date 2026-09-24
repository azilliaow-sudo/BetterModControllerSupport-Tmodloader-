using Terraria;
using Terraria.UI;

namespace ModdedControllerSupport.RecipeBrowserPort.UIElements
{
	class UICraftQueryItemSlot : UIQueryItemSlot
	{
		public UICraftQueryItemSlot(Item item) : base(item)
		{
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);
			CraftUI.instance.SetItem(item.type);
		}
	}
}