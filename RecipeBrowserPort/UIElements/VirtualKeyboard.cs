using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.GameInput;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModdedControllerSupport.RecipeBrowserPort.UIElements
{
    internal class VirtualKeyboard : UIState
    {
        private UIPanel panel;
        private NewUITextBox target;
        private class KeyEntry { public UIPanel Panel; public UIText Text; public Action Action; public float CenterX; public int Row; }
        private List<KeyEntry> keyPanels = new List<KeyEntry>();
        private List<List<KeyEntry>> keyRows = new List<List<KeyEntry>>();
        private int selectedRow = 0;
        private int selectedCol = 0;
        private int navCooldown = 0;
        private bool caps = true;
        private GamePadState prevPadState;
        private bool wasUsingGamepad = true;

        public VirtualKeyboard()
        {
            panel = new UIPanel();
            panel.Width.Set(520, 0);
            panel.Height.Set(200, 0);
            // place at bottom center (lower on screen)
            panel.HAlign = 0.5f;
            panel.VAlign = 1f;
            panel.Top.Set(-40f, 0f);
            panel.BackgroundColor = Microsoft.Xna.Framework.Color.Gray * 0.9f;
            Append(panel);

            CreateKeys();
        }

        private void CreateKeys()
        {
            string row1 = "qwertyuiop";
            string row2 = "asdfghjkl";
            string row3 = "zxcvbnm";
            // Clear rows
            keyRows.Clear();

            AddKeyRow(row1, 6, 6);
            AddKeyRow(row2, 18, 46);
            AddKeyRow(row3, 36, 86);

            // Backspace (right side of row 0)
            AddSpecialKeyToRow(0, "<", 396, 6, () => Backspace(), width: 56);

            // Caps (left on bottom row)
            EnsureRowExists(3);
            AddSpecialKeyToRow(3, "Caps", 6, 146, () => ToggleCaps(), width: 80);

            // Space (center bottom)
            AddSpecialKeyToRow(3, "Space", 96, 146, () => Write(" "), width: 320);

            // Enter (right bottom) - move slightly inward to avoid cut-off
            AddSpecialKeyToRow(3, "Enter", 420, 146, () => { Submit(); }, width: 80);
        }

        private void EnsureRowExists(int row)
        {
            while (keyRows.Count <= row) keyRows.Add(new List<KeyEntry>());
        }

        private void AddKeyRow(string chars, float left, float top)
        {
            int rowIndex = keyRows.Count;
            EnsureRowExists(rowIndex);
            for (int i = 0; i < chars.Length; i++)
            {
                string s = chars[i].ToString();
                float x = left + i * 36;
                float width = 32f;
                var entry = CreateKeyEntry(s.ToUpper(), x, top, () => Write(s), width);
                panel.Append(entry.Panel);
                // compute center X for navigation
                entry.CenterX = x + (width / 2f);
                entry.Row = rowIndex;
                keyPanels.Add(entry);
                keyRows[rowIndex].Add(entry);
            }
        }

        private UIElement CreateKeyElement(string label, float left, float top, Action onClick, float width = 32f)
        {
            var keyPanel = new UIPanel();
            keyPanel.Width.Set(width, 0);
            keyPanel.Height.Set(30, 0);
            keyPanel.Left.Set(left, 0);
            keyPanel.Top.Set(top, 0);
            keyPanel.BackgroundColor = Microsoft.Xna.Framework.Color.DimGray * 0.9f;

            var txt = new UIText(label, 0.7f);
            txt.HAlign = 0.5f;
            txt.VAlign = 0.5f;
            keyPanel.Append(txt);
            keyPanel.OnLeftClick += (a, b) => onClick();
            return keyPanel;
        }

        private KeyEntry CreateKeyEntry(string label, float left, float top, Action onClick, float width = 32f)
        {
            var keyPanel = new UIPanel();
            keyPanel.Width.Set(width, 0);
            keyPanel.Height.Set(30, 0);
            keyPanel.Left.Set(left, 0);
            keyPanel.Top.Set(top, 0);
            keyPanel.BackgroundColor = Microsoft.Xna.Framework.Color.DimGray * 0.9f;

            var txt = new UIText(label, 0.7f);
            txt.HAlign = 0.5f;
            txt.VAlign = 0.5f;
            keyPanel.Append(txt);
            keyPanel.OnLeftClick += (a, b) => onClick();
            var entry = new KeyEntry { Panel = keyPanel, Text = txt, Action = onClick };
            // default CenterX and Row set by caller
            return entry;
        }

        private void AddSpecialKey(string label, float left, float top, Action onClick, float width = 40f)
        {
            var entry = CreateKeyEntry(label, left, top, onClick, width);
            panel.Append(entry.Panel);
            keyPanels.Add(entry);
        }

        private void AddSpecialKeyToRow(int row, string label, float left, float top, Action onClick, float width = 40f)
        {
            EnsureRowExists(row);
            var entry = CreateKeyEntry(label, left, top, onClick, width);
            panel.Append(entry.Panel);
            // compute center X and set row
            entry.CenterX = left + (width / 2f);
            entry.Row = row;
            keyPanels.Add(entry);
            keyRows[row].Add(entry);
        }

        public void AttachTo(NewUITextBox box)
        {
            if (target != null)
            {
                // detach previous
                try { target.OnEnterPressed -= OnTargetEnter; } catch { }
            }
            target = box;
            if (target != null)
            {
                // subscribe via internal TriggerEnter usage already added in NewUITextBox
                // keep local behavior if needed
                // no-op: file context update marker
            }
            UpdateKeyLabels();
        }

        private void OnTargetEnter()
        {
            Submit();
        }

        private void Submit()
        {
            // Trigger enter on textbox via its internal method and then hide
            try { target?.TriggerEnter(); } catch { }
            RecipeBrowserUI.instance?.HideVirtualKeyboard();
            // Ensure input is unblocked when submitting
            try { Main.blockInput = false; } catch { }
        }

        private void ToggleCaps()
        {
            caps = !caps;
            UpdateKeyLabels();
        }

        private void UpdateKeyLabels()
        {
            for (int i = 0; i < keyPanels.Count; i++)
            {
                var entry = keyPanels[i];
                if (entry == null || entry.Text == null) continue;
                string label = entry.Text.Text;
                if (label.Length == 1 && char.IsLetter(label[0]))
                {
                    entry.Text.SetText(caps ? label.ToUpper() : label.ToLower());
                }
            }
        }

        private void Write(string s)
        {
            if (target == null) return;
            var ch = caps ? s.ToUpper() : s.ToLower();
            target.SetText(target.currentString + ch);
        }

        private void Backspace()
        {
            if (target == null) return;
            if (target.currentString.Length == 0) return;
            target.SetText(target.currentString.Substring(0, target.currentString.Length - 1));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Navigation cooldown
            if (navCooldown > 0) navCooldown--;
            // Hide keyboard if player opened inventory
            if (Main.playerInventory)
            {
                try { target?.Unfocus(); } catch { }
                try { RecipeBrowserUI.instance?.HideVirtualKeyboard(); } catch { }
                try { Main.blockInput = false; } catch { }
                return;
            }

            // Detect switching from gamepad to mouse/keyboard: only hide when the active input device changed
            var padCheck = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
            bool nowUsingGamepad = PlayerInput.UsingGamepad || padCheck.IsConnected;
            if (wasUsingGamepad && !nowUsingGamepad)
            {
                try { target?.Unfocus(); } catch { }
                try { RecipeBrowserUI.instance?.HideVirtualKeyboard(); } catch { }
                try { Main.blockInput = false; } catch { }
                wasUsingGamepad = nowUsingGamepad;
                return;
            }
            wasUsingGamepad = nowUsingGamepad;

            // Only navigate with gamepad/dpad
            if (PlayerInput.UsingGamepad)
            {
                var cur = PlayerInput.Triggers.Current;
                if (navCooldown == 0)
                {
                    if (cur.Up) { MoveSelection2D(-1, 0); navCooldown = 8; }
                    else if (cur.Down) { MoveSelection2D(1, 0); navCooldown = 8; }
                    else if (cur.Left) { MoveSelection2D(0, -1); navCooldown = 8; }
                    else if (cur.Right) { MoveSelection2D(0, 1); navCooldown = 8; }
                }

                // Read gamepad state and detect A and DPad just-pressed
                var pad = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
                if (pad.IsConnected)
                {
                    if (navCooldown == 0)
                    {
                        if (pad.DPad.Up == ButtonState.Pressed && prevPadState.DPad.Up != ButtonState.Pressed) { MoveSelection2D(-1, 0); navCooldown = 8; }
                        else if (pad.DPad.Down == ButtonState.Pressed && prevPadState.DPad.Down != ButtonState.Pressed) { MoveSelection2D(1, 0); navCooldown = 8; }
                        else if (pad.DPad.Left == ButtonState.Pressed && prevPadState.DPad.Left != ButtonState.Pressed) { MoveSelection2D(0, -1); navCooldown = 8; }
                        else if (pad.DPad.Right == ButtonState.Pressed && prevPadState.DPad.Right != ButtonState.Pressed) { MoveSelection2D(0, 1); navCooldown = 8; }
                    }

                    if (pad.Buttons.A == ButtonState.Pressed && prevPadState.Buttons.A != ButtonState.Pressed)
                    {
                        ActivateSelected(); navCooldown = 8;
                    }
                }
                prevPadState = pad;
            }

            // Update visual highlight (compare against selected row/col)
            KeyEntry activeEntry = null;
            if (selectedRow >= 0 && selectedRow < keyRows.Count && selectedCol >= 0 && selectedCol < keyRows[selectedRow].Count)
            {
                activeEntry = keyRows[selectedRow][selectedCol];
            }
            for (int i = 0; i < keyPanels.Count; i++)
            {
                var entry = keyPanels[i];
                if (entry?.Panel != null)
                {
                    entry.Panel.BackgroundColor = (entry == activeEntry) ? Microsoft.Xna.Framework.Color.Orange : Microsoft.Xna.Framework.Color.DimGray * 0.9f;
                }
            }
        }

        private void MoveSelection(int delta)
        {
            // Legacy flat movement (left/right) kept for keyboard/controller combos
            if (keyPanels.Count == 0) return;
            // convert current row/col to index
            if (selectedRow >= 0 && selectedRow < keyRows.Count && selectedCol >= 0 && selectedCol < keyRows[selectedRow].Count)
            {
                var currentEntry = keyRows[selectedRow][selectedCol];
                int idx = keyPanels.IndexOf(currentEntry);
                if (idx >= 0)
                {
                    idx = (idx + delta) % keyPanels.Count;
                    if (idx < 0) idx += keyPanels.Count;
                    var entry = keyPanels[idx];
                    if (entry != null)
                    {
                        selectedRow = entry.Row;
                        selectedCol = keyRows[selectedRow].IndexOf(entry);
                    }
                }
            }
            else
            {
                // fallback: just move by delta across flat list
                var idx = delta >= 0 ? delta % keyPanels.Count : (keyPanels.Count + (delta % keyPanels.Count)) % keyPanels.Count;
                selectedRow = 0;
                selectedCol = 0;
            }
        }

        private void MoveSelection2D(int dRow, int dCol)
        {
            if (keyRows.Count == 0) return;
            int newRow = selectedRow + dRow;
            newRow = Math.Clamp(newRow, 0, keyRows.Count - 1);
            int newCol = selectedCol + dCol;
            // clamp within row
            if (newRow >= 0 && newRow < keyRows.Count && keyRows[newRow].Count > 0)
            {
                // when moving rows, pick the key in the new row whose CenterX is closest to current key CenterX
                float currentCenter = 0f;
                if (selectedRow >= 0 && selectedRow < keyRows.Count && selectedCol >= 0 && selectedCol < keyRows[selectedRow].Count)
                {
                    currentCenter = keyRows[selectedRow][selectedCol].CenterX;
                }
                // moving horizontally stays within same row
                if (dRow == 0)
                {
                    newCol = Math.Clamp(newCol, 0, keyRows[newRow].Count - 1);
                }
                else
                {
                    // pick closest by X
                    int best = 0;
                    float bestDist = float.MaxValue;
                    for (int i = 0; i < keyRows[newRow].Count; i++)
                    {
                        float dist = Math.Abs(keyRows[newRow][i].CenterX - currentCenter);
                        if (dist < bestDist) { bestDist = dist; best = i; }
                    }
                    newCol = best;
                }

                selectedRow = newRow;
                selectedCol = newCol;
                // update selectedIndex pointer as well
                var newEntry = keyRows[selectedRow][selectedCol];
                // ensure selectedIndex equivalent via row/col
                // (we now use selectedRow/selectedCol directly elsewhere)
            }
        }

        private void ActivateSelected()
        {
            if (selectedRow < 0 || selectedRow >= keyRows.Count) return;
            if (selectedCol < 0 || selectedCol >= keyRows[selectedRow].Count) return;
            var entry = keyRows[selectedRow][selectedCol];
            if (entry == null) return;
            // Prefer stored action if present
            if (entry.Action != null)
            {
                try { entry.Action(); } catch { /* ignore errors from action */ }
                return;
            }
            // Fallback: inspect stored text label
            if (entry.Text != null)
            {
                string label = entry.Text.Text;
                if (label.Equals("<")) Backspace();
                else if (label.Equals("Caps", StringComparison.OrdinalIgnoreCase)) ToggleCaps();
                else if (label.Equals("Space", StringComparison.OrdinalIgnoreCase)) Write(" ");
                else if (label.Equals("Enter", StringComparison.OrdinalIgnoreCase)) Submit();
                else if (label.Length == 1) Write(label);
            }
        }
    }
}
