using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModdedControllerSupport
{
    public class RecipeBrowserCenterSystem : ModSystem
    {
        private bool centered = false;
        private bool previousOpen = false;
        private bool playerRangeIncreased = false;
        private readonly System.Collections.Generic.List<(FieldInfo field, object original)> savedFields = new System.Collections.Generic.List<(FieldInfo, object)>();
        private readonly System.Collections.Generic.List<(PropertyInfo prop, object original)> savedProps = new System.Collections.Generic.List<(PropertyInfo, object)>();
        private readonly System.Collections.Generic.List<(PropertyInfo prop, FieldInfo field)> cursorTargets = new System.Collections.Generic.List<(PropertyInfo, FieldInfo)>();
        private bool cursorTargetsScanned = false;
        private (PropertyInfo prop, FieldInfo field, object original)[] savedCursorOriginals = null;

        public override void PostUpdateEverything()
        {
            try
            {
                // Interact directly with the ported RecipeBrowserUI in this assembly
                Type uiType = typeof(RecipeBrowserUI);

                FieldInfo instField = uiType.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instField == null) return;
                object inst = instField.GetValue(null);
                if (inst == null)
                {
                    centered = false;
                    previousOpen = false;
                    return;
                }

                // Check whether the UI is open (ShowRecipeBrowser property or showRecipeBrowser field)
                bool isOpen = false;
                PropertyInfo showProp = uiType.GetProperty("ShowRecipeBrowser", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (showProp != null)
                {
                    object val = showProp.GetValue(inst);
                    if (val is bool b) isOpen = b;
                }
                else
                {
                    FieldInfo showField = uiType.GetField("showRecipeBrowser", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (showField != null)
                    {
                        object val = showField.GetValue(inst);
                        if (val is bool b2) isOpen = b2;
                    }
                }

                if (!isOpen)
                {
                    // reset so we re-center next time it's opened
                    centered = false;
                    previousOpen = false;
                   
                }

                previousOpen = true;

                if (centered) return;

                FieldInfo mainPanelField = uiType.GetField("mainPanel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mainPanelField == null) return;
                object panelObj = mainPanelField.GetValue(inst);
                if (panelObj == null) return;
                UIElement panel = panelObj as UIElement;
                if (panel == null) return;

                // Center using alignment so dragging still works. Reset Left/Top offsets.
                panel.HAlign = 0.5f;
                panel.VAlign = 0.5f;
                panel.Left.Set(0f, 0f);
                panel.Top.Set(0f, 0f);
                try { panel.Recalculate(); } catch { }

                centered = true;
              
            }
            catch { }
        }
        

    }
}
