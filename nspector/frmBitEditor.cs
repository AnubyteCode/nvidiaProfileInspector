using nspector.Common;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace nspector
{
    internal partial class frmBitEditor : MicaForm
    {
        private uint _Settingid = 0;
        private frmDrvSettings _SettingsOwner = null;
        private uint _InitValue = 0;
        private uint _CurrentValue = 0;
        private ListViewItem _hoveredItem = null; // Track hovered item

        internal frmBitEditor()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.DoubleBuffered = true;
        }

        internal void ShowDialog(frmDrvSettings SettingsOwner, uint SettingId, uint InitValue, string SettingName)
        {
            _Settingid = SettingId;
            _SettingsOwner = SettingsOwner;
            _InitValue = InitValue;
            Text = string.Format("Bit Value Editor - {0}", SettingName);

            // Ensure dark mode is applied before showing the dialog
            ApplyDarkMode();
            this.ShowDialog(SettingsOwner);
        }

        private void frmBitEditor_Load(object sender, EventArgs e)
        {
            ApplyDarkMode(); // Apply dark mode styling

            // Enable owner drawing and attach event handlers
            clbBits.OwnerDraw = true;
            clbBits.DrawColumnHeader += clbBits_DrawColumnHeader;
            clbBits.DrawItem += clbBits_DrawItem;
            clbBits.DrawSubItem += clbBits_DrawSubItem;
            clbBits.MouseMove += clbBits_MouseMove; // Track hovered item

            SplitBitsFromUnknownSettings();

            // Adjust column widths to fill the ListView
            clbBits.Columns[0].Width = 50; // Bit column
            clbBits.Columns[1].Width = 200; // Name column
            clbBits.Columns[2].Width = 50; // Count column
            clbBits.Columns[3].Width = clbBits.ClientSize.Width - 300; // Profiles column (fills remaining space)

            SetValue(_InitValue);
        }

        private void SplitBitsFromUnknownSettings()
        {
            uint lastValue = 0;
            lastValue = _CurrentValue;
            string[] filters = tbFilter.Text.Split(',');
            clbBits.Items.Clear();

            var referenceSettings = DrsServiceLocator.ReferenceSettings?.Settings.FirstOrDefault(s => s.SettingId == _Settingid);

            var settingsCache = DrsServiceLocator.ScannerService.CachedSettings.FirstOrDefault(x => x.SettingId == _Settingid);

            for (int bit = 0; bit < 32; bit++)
            {
                string profileNames = "";
                uint profileCount = 0;

                if (settingsCache != null)
                {
                    for (int i = 0; i < settingsCache.SettingValues.Count; i++)
                    {
                        if (((settingsCache.SettingValues[i].Value >> bit) & 0x1) == 0x1)
                        {
                            if (filters.Length == 0)
                            {
                                profileNames += settingsCache.SettingValues[i].ProfileNames + ",";
                            }
                            else
                            {
                                string[] settingProfileNames = settingsCache.SettingValues[i].ProfileNames.ToString().Split(',');
                                for (int p = 0; p < settingProfileNames.Length; p++)
                                {
                                    for (int f = 0; f < filters.Length; f++)
                                    {
                                        if (settingProfileNames[p].ToLowerInvariant().Contains(filters[f].ToLower()))
                                        {
                                            profileNames += settingProfileNames[p] + ",";
                                        }
                                    }
                                }
                            }
                            profileCount += settingsCache.SettingValues[i].ValueProfileCount;
                        }
                    }
                }

                uint mask = (uint)1 << bit;
                string maskStr = "";

                if (referenceSettings != null)
                {
                    var maskValue = referenceSettings.SettingValues.FirstOrDefault(v => v.SettingValue == mask);
                    if (maskValue != null)
                    {
                        maskStr = maskValue.UserfriendlyName;
                        if (maskStr.Contains("("))
                        {
                            maskStr = maskStr.Substring(0, maskStr.IndexOf("(") - 1);
                        }
                    }
                }

                clbBits.Items.Add(new ListViewItem(new string[] {
                        string.Format("#{0:00}",bit),
                        maskStr,
                        profileCount.ToString(),
                        profileNames,
                    }));
            }

            SetValue(lastValue);
        }

        private void updateValue(bool changeState, int changedIndex)
        {
            uint val = 0;
            for (int b = 0; b < clbBits.Items.Count; b++)
            {
                if (((clbBits.Items[b].Checked) && changedIndex != b) || (changeState && (changedIndex == b)))
                {
                    val = (uint)((uint)val | (uint)(1 << b));
                }
            }

            UpdateCurrent(val);
        }

        private void UpdateValue()
        {
            uint val = 0;
            for (int b = 0; b < clbBits.Items.Count; b++)
            {
                if (clbBits.Items[b].Checked)
                {
                    val = (uint)((uint)val | (uint)(1 << b));
                }
            }

            UpdateCurrent(val);
        }

        private void SetValue(uint val)
        {
            for (int b = 0; b < clbBits.Items.Count; b++)
            {
                if (((val >> b) & 0x1) == 0x1)
                    clbBits.Items[b].Checked = true;
                else
                    clbBits.Items[b].Checked = false;
            }

            UpdateValue();
        }

        private void UpdateCurrent(uint val)
        {
            _CurrentValue = val;
            textBox1.Text = "0x" + (val).ToString("X8");
        }

        private void UpdateCurrent(string text)
        {
            uint val = DrsUtil.ParseDwordByInputSafe(text);
            UpdateCurrent(val);
            SetValue(val);
        }

        private void clbBits_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            updateValue(e.NewValue == CheckState.Checked, e.Index);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _SettingsOwner.SetSelectedDwordValue(_CurrentValue);
            Close();
        }

        private void tbFilter_TextChanged(object sender, EventArgs e)
        {
            SplitBitsFromUnknownSettings();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            SplitBitsFromUnknownSettings();
        }

        private void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                UpdateCurrent(textBox1.Text);
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            UpdateCurrent(textBox1.Text);
        }

        private void ApplyValueToProfile(uint val)
        {
            DrsServiceLocator
               .SettingService
               .SetDwordValueToProfile(_SettingsOwner._CurrentProfile, _Settingid, val);
        }

        private async void btnDirectApply_Click(object sender, EventArgs e)
        {
            ApplyValueToProfile(_CurrentValue);

            await CheckIfSettingIsStored();

            if (File.Exists(tbGamePath.Text))
            {
                Process.Start(tbGamePath.Text);
            }
        }

        private async Task CheckIfSettingIsStored()
        {
            await Task.Run(async () =>
            {
                while (_CurrentValue != DrsServiceLocator.SettingService
                .GetDwordValueFromProfile(_SettingsOwner._CurrentProfile, _Settingid, false, true))
                {
                    await Task.Delay(50);
                }
            });
        }

        private void btnBrowseGame_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                DefaultExt = "*.exe",
                Filter = "Applications|*.exe",
                DereferenceLinks = false
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbGamePath.Text = ofd.FileName;
            }
        }

        private void ApplyDarkMode()
        {
            // Set form background and foreground colors
            this.BackColor = Color.FromArgb(32, 32, 32); // Dark background
            this.ForeColor = Color.White; // Light text

            // Apply dark mode to all controls recursively
            ApplyDarkModeToControls(this.Controls);

            // Apply dark mode to ListView
            clbBits.BackColor = Color.FromArgb(32, 32, 32);
            clbBits.ForeColor = Color.White;
        }

        private void ApplyDarkModeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                // Set control background and foreground colors
                control.BackColor = Color.FromArgb(32, 32, 32); // Dark background
                control.ForeColor = Color.White; // Light text

                // Handle specific control types
                if (control is ListView listView)
                {
                    listView.BackColor = Color.FromArgb(32, 32, 32);
                    listView.ForeColor = Color.White;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = Color.FromArgb(64, 64, 64); // Slightly lighter background for text boxes
                    textBox.ForeColor = Color.White;
                }
                else if (control is Button button)
                {
                    button.BackColor = Color.FromArgb(64, 64, 64); // Slightly lighter background for buttons
                    button.ForeColor = Color.White;
                }
                else if (control is CheckedListBox checkedListBox)
                {
                    checkedListBox.BackColor = Color.FromArgb(32, 32, 32);
                    checkedListBox.ForeColor = Color.White;
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.BackColor = Color.FromArgb(64, 64, 64);
                    numericUpDown.ForeColor = Color.White;
                }

                // Recursively apply dark mode to child controls
                if (control.HasChildren)
                {
                    ApplyDarkModeToControls(control.Controls);
                }
            }
        }

        private void clbBits_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // Draw the column header with dark mode colors
            using (var backBrush = new SolidBrush(Color.FromArgb(45, 45, 45))) // Dark background
            using (var foreBrush = new SolidBrush(Color.White)) // Light text
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
                e.Graphics.DrawString(e.Header.Text, e.Font, foreBrush, e.Bounds);
            }
        }

        private void clbBits_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false; // Disable default drawing

            // Determine the background color based on the item state
            Color backColor = e.Item.Selected ? Color.FromArgb(64, 64, 64) : Color.FromArgb(32, 32, 32);
            if (e.Item == _hoveredItem) // Check if the item is being hovered
            {
                backColor = Color.FromArgb(96, 96, 96); // Hover color
            }

            // Draw the background
            using (var backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            // Draw the checkbox
            int checkboxSize = 16; // Size of the checkbox
            int checkboxPadding = 2; // Padding around the checkbox
            Rectangle checkboxBounds = new Rectangle(
                e.Bounds.Left + checkboxPadding,
                e.Bounds.Top + (e.Bounds.Height - checkboxSize) / 2,
                checkboxSize,
                checkboxSize
            );

            CheckBoxRenderer.DrawCheckBox(
                e.Graphics,
                checkboxBounds.Location,
                e.Item.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal
            );

            // Draw the text
            using (var foreBrush = new SolidBrush(Color.White)) // Light text
            {
                Rectangle textBounds = new Rectangle(
                    checkboxBounds.Right + checkboxPadding,
                    e.Bounds.Top,
                    e.Bounds.Width - checkboxBounds.Right - checkboxPadding,
                    e.Bounds.Height
                );

                TextRenderer.DrawText(
                    e.Graphics,
                    e.Item.Text,
                    e.Item.Font,
                    textBounds,
                    Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                );
            }
        }

        private void clbBits_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = false; // Disable default drawing

            // Determine the background color based on the item state
            Color backColor = e.Item.Selected ? Color.FromArgb(64, 64, 64) : Color.FromArgb(32, 32, 32);
            if (e.Item == _hoveredItem) // Check if the item is being hovered
            {
                backColor = Color.FromArgb(96, 96, 96); // Hover color
            }

            // Draw the background
            using (var backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            // Draw the text
            using (var foreBrush = new SolidBrush(Color.White)) // Light text
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    e.SubItem.Text,
                    e.SubItem.Font,
                    e.Bounds,
                    Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                );
            }
        }

        private void clbBits_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the item under the mouse cursor
            var item = clbBits.GetItemAt(e.X, e.Y);

            if (item != _hoveredItem)
            {
                _hoveredItem = item;
                clbBits.Invalidate(); // Redraw the ListView to update the hover state
            }
        }
    }
}