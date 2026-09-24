using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModdedControllerSupport;
using ModdedControllerSupport.RecipeBrowserPort.UIElements;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModdedControllerSupport
{
	public class UISystem : ModSystem
	{
		public override void UpdateUI(GameTime gameTime) => ModdedControllerSupport.instance.UpdateUI(gameTime);

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) => ModdedControllerSupport.instance.ModifyInterfaceLayers(layers);

		public override void PreSaveAndQuit() => ModdedControllerSupport.instance.PreSaveAndQuit();

		public override void PostAddRecipes()
		{
			if (!Main.dedServ) {
				LootCacheManager.Setup(ModdedControllerSupport.instance);
				RecipeBrowserUI.instance.PostSetupContent();
			}
		}

		public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber) {
			if(!Main.dedServ && messageType == MessageID.PlayerTeam) {
				RecipeBrowserUI.instance.favoritePanelUpdateNeeded = true;
			}
			return false;
		}

		public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7) {
			if (!Main.dedServ && msgType == MessageID.PlayerTeam) {
				RecipeBrowserUI.instance.favoritePanelUpdateNeeded = true;
			}
			return false;
		}
	}
}
