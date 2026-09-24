using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModdedControllerSupport
{
    public class ModdedControllerSupportSystem : ModSystem
    {
        private bool scanned = false;
        private int scanTimer = 0;
        private List<UIState> discoveredRoots = new List<UIState>();
        private List<UIScrollbar> trackedScrollbars = new List<UIScrollbar>();
        private List<object> trackedLists = new List<object>();

        public override void PostUpdateEverything()
        {
            try
            {
                if (scanTimer > 0) scanTimer--;

                if (!scanned || (trackedScrollbars.Count == 0 && trackedLists.Count == 0 && scanTimer <= 0))
                {
                    scanTimer = 30; 
                    scanned = true;
                    discoveredRoots.Clear();
                    trackedScrollbars.Clear();
                    trackedLists.Clear();

                    Mod boss = ModLoader.GetMod("BossChecklist");
                    if (boss == null)
                        return;

                    Assembly asm = boss.GetType().Assembly;
                    Type uiType = typeof(UserInterface);
                    Type uiStateType = typeof(UIState);

                    foreach (Type t in asm.GetTypes())
                    {
                    
                        foreach (FieldInfo f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            try
                            {
                                object val = f.GetValue(null);
                                if (val == null) continue;
                                if (uiType.IsAssignableFrom(val.GetType()))
                                {
                                    UserInterface ui = (UserInterface)val;
                                    UIState state = GetCurrentStateSafe(ui);
                                    if (state != null) discoveredRoots.Add(state);
                                }
                                else if (uiStateType.IsAssignableFrom(val.GetType()))
                                {
                                    discoveredRoots.Add((UIState)val);
                                }
                            }
                            catch { }
                        }

                      
                        foreach (PropertyInfo p in t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            try
                            {
                                object val = p.GetValue(null);
                                if (val == null) continue;
                                if (uiType.IsAssignableFrom(val.GetType()))
                                {
                                    UserInterface ui = (UserInterface)val;
                                    UIState state = GetCurrentStateSafe(ui);
                                    if (state != null) discoveredRoots.Add(state);
                                }
                                else if (uiStateType.IsAssignableFrom(val.GetType()))
                                {
                                    discoveredRoots.Add((UIState)val);
                                }
                            }
                            catch { }
                        }
                    }

               
     
                    foreach (UIState root in discoveredRoots)
                    {
                        FindScrollbarsAndLists(root, trackedScrollbars, trackedLists);
                    }
                }

            
                if (trackedScrollbars.Count > 0 || trackedLists.Count > 0)
                {
                    GamePadState gp = GamePad.GetState(PlayerIndex.One);
                    if (gp.IsConnected)
                    {
                        Vector2 right = gp.ThumbSticks.Right;
                        float stickY = -right.Y;
                        const float deadzone = 0.25f;
                        const float sensitivity = 600f;
                        if (Math.Abs(stickY) > deadzone)
                        {
                            float effective = (Math.Abs(stickY) - deadzone) / (1f - deadzone) * Math.Sign(stickY);
                            float delta = effective * sensitivity * (1f / 60f); // per-frame approx

                            foreach (var sb in trackedScrollbars.ToArray())
                            {
                                try { sb.ViewPosition += delta; }
                                catch { trackedScrollbars.Remove(sb); }
                            }

                            foreach (var list in trackedLists.ToArray())
                            {
                                try
                                {
                                    Type t = list.GetType();
                                    PropertyInfo vp = t.GetProperty("ViewPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (vp != null && vp.PropertyType == typeof(float))
                                    {
                                        float cur = (float)vp.GetValue(list);
                                        vp.SetValue(list, cur + delta);
                                        continue;
                                    }
                                    FieldInfo vf = t.GetField("viewPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (vf != null && vf.FieldType == typeof(float))
                                    {
                                        float cur = (float)vf.GetValue(list);
                                        vf.SetValue(list, cur + delta);
                                    }
                                }
                                catch { trackedLists.Remove(list); }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private UIState GetCurrentStateSafe(UserInterface ui)
        {
            try
            {
                PropertyInfo p = ui.GetType().GetProperty("CurrentState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null)
                {
                    object val = p.GetValue(ui);
                    if (val is UIState us) return us;
                }

                FieldInfo f = ui.GetType().GetField("currentState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    object val = f.GetValue(ui);
                    if (val is UIState us2) return us2;
                }
            }
            catch { }
            return null;
        }

        private void FindScrollbarsAndLists(UIElement element, List<UIScrollbar> list, List<object> lists)
        {
            if (element == null) return;
            if (element is UIScrollbar sb && !list.Contains(sb))
            {
                list.Add(sb);
            }

            Type t = element.GetType();
            if ((t.Name == "UIList" || (t.FullName != null && t.FullName.Contains("UIList"))) && !lists.Contains(element))
            {
                lists.Add(element);
            }

            string[] childNames = new[] { "Children", "Elements", "children", "elements", "_children", "_elements" };
            foreach (string name in childNames)
            {
                MemberInfo[] members = t.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var m in members)
                {
                    try
                    {
                        object val = null;
                        if (m is PropertyInfo p) val = p.GetValue(element);
                        else if (m is FieldInfo f) val = f.GetValue(element);
                        if (val is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var c in enumerable)
                            {
                                if (c is UIElement ue) FindScrollbarsAndLists(ue, list, lists);
                            }
                        }
                    }
                    catch { }
                }
            }

           
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(f.FieldType)) continue;
                    var val = f.GetValue(element) as System.Collections.IEnumerable;
                    if (val == null) continue;
                    foreach (var c in val)
                    {
                        if (c is UIElement ue) FindScrollbarsAndLists(ue, list, lists);
                    }
                }
                catch { }
            }
        }
    }
}
