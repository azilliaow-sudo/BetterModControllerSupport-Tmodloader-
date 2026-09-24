using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModdedControllerSupport
{
    public class RecipeBrowserControllerSupportSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            try
            {
                // Interact directly with the ported RecipeBrowserUI in this assembly
                Type uiType = typeof(RecipeBrowserUI);

                FieldInfo instField = uiType.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instField == null) return;
                object inst = instField.GetValue(null);
                if (inst == null) return;

                // Get dropdown instance field
                FieldInfo dropdownField = uiType.GetField("ModFilterDropdown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (dropdownField == null) return;
                object dropdown = dropdownField.GetValue(inst);
                if (dropdown == null) return;

                GamePadState gp = GamePad.GetState(PlayerIndex.One);
                if (!gp.IsConnected) return;

                Vector2 right = gp.ThumbSticks.Right;
                float stickY = -right.Y;
                const float deadzone = 0.25f;
                const float sensitivity = 600f;
                if (Math.Abs(stickY) <= deadzone) return;

                float effective = (Math.Abs(stickY) - deadzone) / (1f - deadzone) * Math.Sign(stickY);
                float delta = effective * sensitivity * (1f / 60f);

                // find UIList inside dropdown and apply delta
                var list = FindChildOfType(dropdown, "UIList");
                if (list != null)
                {
                    ApplyDeltaToList(list, delta);
                }
            }
            catch { }
        }

        private object FindChildOfType(object elemObj, string typeName)
        {
            if (elemObj == null) return null;
            try
            {
                if (elemObj is UIElement elem)
                {
                    PropertyInfo childrenProp = elem.GetType().GetProperty("Children", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (childrenProp != null)
                    {
                        var children = childrenProp.GetValue(elem) as System.Collections.IEnumerable;
                        if (children != null)
                        {
                            foreach (var c in children)
                            {
                                if (c == null) continue;
                                var ct = c.GetType();
                                if (ct.Name == typeName || (ct.FullName != null && ct.FullName.Contains(typeName))) return c;
                                if (c is UIElement ue)
                                {
                                    var found = FindChildOfType(ue, typeName);
                                    if (found != null) return found;
                                }
                            }
                        }
                    }
                }
                // fallback: inspect fields that are enumerable
                foreach (var f in elemObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    try
                    {
                        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(f.FieldType)) continue;
                        var val = f.GetValue(elemObj) as System.Collections.IEnumerable;
                        if (val == null) continue;
                        foreach (var c in val)
                        {
                            if (c == null) continue;
                            var ct = c.GetType();
                            if (ct.Name == typeName || (ct.FullName != null && ct.FullName.Contains(typeName))) return c;
                            if (c is UIElement ue)
                            {
                                var found = FindChildOfType(ue, typeName);
                                if (found != null) return found;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private void ApplyDeltaToList(object listObj, float delta)
        {
            if (listObj == null) return;
            try
            {
                Type t = listObj.GetType();
                PropertyInfo vp = t.GetProperty("ViewPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (vp != null && vp.PropertyType == typeof(float))
                {
                    float cur = (float)vp.GetValue(listObj);
                    vp.SetValue(listObj, cur + delta);
                    return;
                }
                FieldInfo vf = t.GetField("viewPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (vf != null && vf.FieldType == typeof(float))
                {
                    float cur = (float)vf.GetValue(listObj);
                    vf.SetValue(listObj, cur + delta);
                }
            }
            catch { }
        }
    }
}
