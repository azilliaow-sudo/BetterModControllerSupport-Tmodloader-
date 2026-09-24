using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.ID;
using ModdedControllerSupport.NewFolder;

namespace ModdedControllerSupport
{

    
  
    public class QuickBuffConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;


        public Buttons ControllerPrimary { get; set; } = Buttons.LeftShoulder;


        public bool RequireRightStick { get; set; } = true;


        public float RightStickThreshold { get; set; } = 0.2f;


        public RightStickDirection RightStickDirection { get; set; } = RightStickDirection.Down;


        //public int CooldownTicks { get; set; } = 30;
    }


    public class QuickBuffSystem : ModSystem
    {

        private ulong lastQuickBuffTick = 0UL;
        // State for enforcing "primary pressed before stick" rule
        private bool primaryWasDownLast = false;
        private bool rightWasMovedLast = false;
        private ulong primaryPressedTick = 0UL;
        private ulong rightMovedPressedTick = 0UL;
        private const ulong PrimaryToStickWindow = 60UL; // 1 second at 60 TPS

        public override void PostUpdateEverything()
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server) return;
            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            var cfg = ModContent.GetInstance<QuickBuffConfig>();
            // Read flattened config properties directly
            var primary = cfg.ControllerPrimary;
            var requireRight = cfg.RequireRightStick;
            var threshold = cfg.RightStickThreshold;
            var dir = cfg.RightStickDirection;
            //var cooldown = cfg.CooldownTicks;

            GamePadState pad = GamePad.GetState(PlayerIndex.One);
            if (!pad.IsConnected) return;

            // Use flattened config values
            bool primaryDown = pad.IsButtonDown(primary);

            // Edge-detect primary press
            if (primaryDown)
            {
                if (!primaryWasDownLast)
                {
                    primaryPressedTick = Main.GameUpdateCount;
                }
                primaryWasDownLast = true;
            }
            else
            {
                primaryWasDownLast = false;
            }

            Vector2 right = pad.ThumbSticks.Right;
            bool magnitudeOk = right.Length() >= Math.Max(0f, Math.Min(1f, threshold));
            bool directionOk = false;

            switch (dir)
            {
                case RightStickDirection.Up:
                    directionOk = right.Y >= threshold && magnitudeOk;
                    break;
                case RightStickDirection.Down:
                    directionOk = right.Y <= -threshold && magnitudeOk;
                    break;
                case RightStickDirection.Right:
                    directionOk = right.X >= threshold && magnitudeOk;
                    break;
                case RightStickDirection.Left:
                    directionOk = right.X <= -threshold && magnitudeOk;
                    break;
            }

            bool rightMoved = directionOk;

            // Edge-detect right-stick movement
            if (rightMoved)
            {
                if (!rightWasMovedLast)
                {
                    rightMovedPressedTick = Main.GameUpdateCount;
                }
                rightWasMovedLast = true;
            }
            else
            {
                rightWasMovedLast = false;
            }

            // Require primary to have been pressed before the stick moved (if RequireRightStick)
            bool allowByOrder = true;
            if (requireRight && rightMoved)
            {
                allowByOrder = primaryPressedTick != 0UL && rightMovedPressedTick != 0UL && primaryPressedTick < rightMovedPressedTick && (rightMovedPressedTick - primaryPressedTick) <= PrimaryToStickWindow;
            }

            if (primaryDown && (!requireRight || (rightMoved && allowByOrder)))
            {
                TryTriggerQuickBuff(player);
                lastQuickBuffTick = Main.GameUpdateCount;
            }
        }

        private void TryTriggerQuickBuff(Player player)
        {
            if (player == null) return;

            // Attempt to call vanilla quick heal helper. If your tModLoader version
            // does not expose QuickHeal(), replace this with appropriate logic
            // or send a ModPacket to request the action server-side.
            try
            {
                player.QuickBuff();
            }
            catch
            {
                // Fail silently if QuickHeal isn't available in this tModLoader version.
            }
        }
    }
}
