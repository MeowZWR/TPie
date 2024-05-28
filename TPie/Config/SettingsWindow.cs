using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using TPie.Helpers;
using TPie.Models;
using TPie.Models.Elements;

namespace TPie.Config
{
    internal class SettingsWindow : Window
    {
        private Settings Settings => Plugin.Settings;
        private List<Ring> Rings => Settings.Rings;

        private string[] _fontSizes;
        private string[] _animationNames;

        private Vector2 _windowPos = Vector2.Zero;
        private Vector2 RingWindowPos => _windowPos + new Vector2(410 * _scale, 0);

        private Ring? _removingRing = null;
        private bool _applyingGlobalBorderSettings = false;

        private float _scale => ImGuiHelpers.GlobalScale;

        public SettingsWindow(string name) : base(name)
        {
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(400, 470);

            _fontSizes = new string[40 - 13];
            for (int i = 14; i <= 40; i++)
            {
                _fontSizes[i - 14] = $"{i}";
            }

            _animationNames = new string[]
            {
                "无动画", "螺旋", "序列", "淡入淡出"
            };
        }

        public override void OnClose()
        {
            Settings.Save(Settings);
        }

        public override void Draw()
        {
            _windowPos = ImGui.GetWindowPos();

            if (!ImGui.BeginTabBar("##TPie_Settings_TabBar"))
            {
                return;
            }

            // General
            if (ImGui.BeginTabItem("常规选项 ##TPie_Settings"))
            {
                DrawGeneralTab();
                ImGui.EndTabItem();
            }

            // Global Border Settings
            if (ImGui.BeginTabItem("全局边框设置 ##TPie_Settings"))
            {
                DrawGlobalBorderSettingsTab();
                ImGui.EndTabItem();
            }

            // Rings
            if (ImGui.BeginTabItem("   环    ##TPie_Settings"))
            {
                DrawRingsTab();
                ImGui.EndTabItem();
            }

            // donate button
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255f / 255f, 94f / 255f, 91f / 255f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(255f / 255f, 94f / 255f, 91f / 255f, .85f));

            ImGui.SetCursorPos(new Vector2(280 * _scale, 26 * _scale));
            if (ImGui.Button("去Ko-fi支持作者", new Vector2(104 * _scale, 24 * _scale)))
            {
                OpenUrl("https://ko-fi.com/Tischel");
            }

            ImGui.PopStyleColor(2);

            ImGui.EndTabBar();
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                try
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    if (RuntimeInformation.IsOSPlatform(osPlatform: OSPlatform.Windows))
                    {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.Error("打开网址时发生错误：" + e.Message);
                }
            }
        }

        private void DrawGeneralTab()
        {
            // position
            ImGui.Text("位置");
            ImGui.BeginChild("##Position", new Vector2(384 * _scale, 70 * _scale), true);
            {
                if (ImGui.RadioButton("跟随鼠标中心", Settings.AppearAtCursor))
                {
                    Settings.AppearAtCursor = true;
                }

                if (ImGui.RadioButton("固定位置", !Settings.AppearAtCursor))
                {
                    Settings.AppearAtCursor = false;
                }
                DrawHelper.SetTooltip("(0,0) 是屏幕中心。");

                if (!Settings.AppearAtCursor)
                {
                    ImGui.SameLine();
                    ImGui.PushItemWidth(140 * _scale);
                    ImGui.DragFloat2("##Position", ref Settings.CenterPositionOffset, 0.5f, -4000, 4000);
                    DrawHelper.SetTooltip("(0,0) 是屏幕中心。");

                    ImGui.SameLine();
                    ImGui.Checkbox("使鼠标居中", ref Settings.AutoCenterCursor);
                    DrawHelper.SetTooltip("激活环时，鼠标将自动移到环的中心。");
                }
            }
            ImGui.EndChild();

            // font
            ImGui.Spacing();
            ImGui.Text("字体");
            ImGui.BeginChild("##Font", new Vector2(384 * _scale, 40 * _scale), true);
            {
                ImGui.Checkbox("使用自定义字体", ref Settings.UseCustomFont);
                DrawHelper.SetTooltip("启用时使用TPie自带的\"Expressway\"字体(不支持中文)。\n禁用时使用游戏字体。");

                if (Settings.UseCustomFont)
                {
                    ImGui.SameLine();
                    ImGui.Text("\t");
                    ImGui.SameLine();

                    ImGui.PushItemWidth(80 * _scale);
                    int fontIndex = Settings.FontSize - 14;
                    if (ImGui.Combo("字号", ref fontIndex, _fontSizes, _fontSizes.Length))
                    {
                        Settings.FontSize = fontIndex + 14;
                        FontsHelper.LoadFont();
                    }
                }
            }
            ImGui.EndChild();

            // keybinds
            ImGui.Spacing();
            ImGui.Text("快捷键绑定");
            ImGui.BeginChild("##Keybinds", new Vector2(384 * _scale, 64 * _scale), true);
            {
                ImGui.Checkbox("快捷键透传", ref Settings.KeybindPassthrough);
                DrawHelper.SetTooltip("如果启用，TPie将不会阻止游戏接收已绑定到环的快捷键。");

                ImGui.SameLine();
                ImGui.Checkbox("启用快速设置", ref Settings.EnableQuickSettings);
                DrawHelper.SetTooltip("启用后，打开环时双击鼠标右键将打开该环的设置。");

                ImGui.Checkbox("启用ESC键关闭环", ref Settings.EnableQuickSettings);
                DrawHelper.SetTooltip("启用后，在打开具有可切换快捷键绑定的环时按ESC键可立即将其关闭。");
            }
            ImGui.EndChild();

            // style
            ImGui.Spacing();
            ImGui.Text("样式");
            ImGui.BeginChild("##Style", new Vector2(384 * _scale, 64 * _scale), true);
            {
                ImGui.Checkbox("绘制环背景   ", ref Settings.DrawRingBackground);

                ImGui.SameLine();
                ImGui.Checkbox("鼠标指向时调整图标大小", ref Settings.AnimateIconSizes);

                ImGui.Checkbox("显示冷却数值  ", ref Settings.ShowCooldowns);

                ImGui.SameLine();
                ImGui.Checkbox("显示剩余物品计数", ref Settings.ShowRemainingItemCount);
            }
            ImGui.EndChild();

            // animation
            ImGui.Spacing();
            ImGui.Text("动画");
            ImGui.BeginChild("##Animation", new Vector2(384 * _scale, 40 * _scale), true);
            {
                ImGui.PushItemWidth(100 * _scale);
                int animIndex = (int)Settings.AnimationType;
                if (ImGui.Combo("##AnimationType", ref animIndex, _animationNames, _animationNames.Length))
                {
                    Settings.AnimationType = (RingAnimationType)animIndex;
                }

                ImGui.SameLine();
                ImGui.Text("\t");

                ImGui.PushItemWidth(80);
                ImGui.SameLine();
                ImGui.DragFloat("持续时间", ref Settings.AnimationDuration, 0.1f, 0, 5);
                DrawHelper.SetTooltip("秒");
            }
            ImGui.EndChild();
        }

        private void DrawGlobalBorderSettingsTab()
        {
            ImGui.Text("这些是创建新的环组件时的默认边框设置。");
            ImGui.Text("在创建新环时使用。");
            ImGui.NewLine();

            ImGui.BeginChild("##GlobalBorderSettings", new Vector2(272 * _scale, 93 * _scale), true);
            {
                Settings.GlobalBorderSettings.Draw();
            }
            ImGui.EndChild();

            ImGui.NewLine();
            if (ImGui.Button("应用到所有存在的组件", new Vector2(272, 30)))
            {
                _applyingGlobalBorderSettings = true;
            }

            if (_applyingGlobalBorderSettings)
            {
                var (didConfirm, didClose) = DrawHelper.DrawConfirmationModal("应用？", "你确定要将边框设置", "应用到所有存在的组件？", "这将没有办法撤回！");

                if (didConfirm)
                {
                    foreach (Ring ring in Settings.Rings)
                    {
                        foreach (RingElement element in ring.Items)
                        {
                            element.Border = ItemBorder.GlobalBorderSettingsCopy();
                        }
                    }

                    Settings.Save(Settings);
                }

                if (didConfirm || didClose)
                {
                    _applyingGlobalBorderSettings = false;
                }
            }
        }

        private void DrawRingsTab()
        {
            // options
            ImGui.BeginChild("##Options", new Vector2(384 * _scale, 40 * _scale), true);
            {
                ImGui.SameLine();
                ImGui.Text("新建");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
                {
                    Ring newRing = new Ring($"Ring{Rings.Count + 1}", Vector4.One, new KeyBind(0), 150f, new Vector2(40));
                    Plugin.Settings.AddRing(newRing);
                }
                ImGui.PopFont();
                DrawHelper.SetTooltip("添加一个新的空环");

                ImGui.SameLine();
                ImGui.Text("\t\t\t导入");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Download.ToIconString()))
                {
                    string importString = ImGui.GetClipboardText();
                    List<Ring> newRings = ImportExportHelper.ImportRings(importString);

                    foreach (Ring ring in newRings)
                    {
                        Plugin.Settings.AddRing(ring);
                    }
                }
                ImGui.PopFont();
                DrawHelper.SetTooltip("从剪贴板数据导入来添加一个新环。");

                ImGui.SameLine();
                ImGui.Text("\t\t\t导出所有");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Upload.ToIconString()))
                {
                    string exportString = ImportExportHelper.GenerateExportString(Rings);
                    ImGui.SetClipboardText(exportString);
                }
                ImGui.PopFont();
                DrawHelper.SetTooltip("将所有环数据导出到剪贴板");
            }
            ImGui.EndChild();

            var flags =
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.BordersOuter |
                ImGuiTableFlags.BordersInner |
                ImGuiTableFlags.ScrollY |
                ImGuiTableFlags.SizingFixedSame;

            // rings
            if (ImGui.BeginTable("##Rings_Table", 5, flags, new Vector2(384 * _scale, 366 * _scale)))
            {
                ImGui.TableSetupColumn("颜色", ImGuiTableColumnFlags.WidthStretch, 8, 0);
                ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthStretch, 25, 1);
                ImGui.TableSetupColumn("快捷键", ImGuiTableColumnFlags.WidthStretch, 29, 2);
                ImGui.TableSetupColumn("动作", ImGuiTableColumnFlags.WidthStretch, 24, 3);
                ImGui.TableSetupColumn("移动", ImGuiTableColumnFlags.WidthStretch, 14, 4);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                for (int i = 0; i < Rings.Count; i++)
                {
                    Ring ring = Rings[i];

                    ImGui.PushID(i.ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                    // color
                    if (ImGui.TableSetColumnIndex(0))
                    {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 3 * _scale);

                        Vector3 color = new Vector3(ring.Color.X, ring.Color.Y, ring.Color.Z);
                        if (ImGui.ColorEdit3("", ref color, ImGuiColorEditFlags.NoInputs))
                        {
                            ring.Color = new Vector4(color.X, color.Y, color.Z, 1);
                        }
                    }

                    // name
                    if (ImGui.TableSetColumnIndex(1))
                    {
                        ImGui.Text(ring.Name);
                    }

                    // keybind
                    if (ImGui.TableSetColumnIndex(2))
                    {
                        if (ImGui.Button(ring.KeyBind.Description(), new Vector2(100, 24)))
                        {
                            Plugin.ShowKeyBindWindow(ImGui.GetMousePos(), ring);
                        }
                    }

                    // actions
                    if (ImGui.TableSetColumnIndex(3))
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button(FontAwesomeIcon.Pen.ToIconString()))
                        {
                            Plugin.ShowRingSettingsWindow(RingWindowPos, ring);
                        }
                        ImGui.PopFont();
                        DrawHelper.SetTooltip("编辑组件");

                        ImGui.SameLine();
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 3 * _scale);
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button(FontAwesomeIcon.Upload.ToIconString()))
                        {
                            string exportString = ImportExportHelper.GenerateExportString(ring);
                            ImGui.SetClipboardText(exportString);
                        }
                        ImGui.PopFont();
                        DrawHelper.SetTooltip("导出到剪贴板");

                        ImGui.SameLine();
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 3 * _scale);
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString()))
                        {
                            _removingRing = ring;
                        }
                        ImGui.PopFont();
                        DrawHelper.SetTooltip("删除");
                    }

                    // move
                    if (ImGui.TableSetColumnIndex(4))
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button(FontAwesomeIcon.ArrowUp.ToIconString()))
                        {
                            Ring tmp = Rings[i];

                            // circular?
                            if (i == 0)
                            {
                                Rings.Remove(tmp);
                                Rings.Add(tmp);
                            }
                            else
                            {
                                Rings[i] = Rings[i - 1];
                                Rings[i - 1] = tmp;
                            }
                        }
                        ImGui.PopFont();
                        DrawHelper.SetTooltip("上移");

                        ImGui.SameLine();
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 3 * _scale);
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button(FontAwesomeIcon.ArrowDown.ToIconString()))
                        {
                            Ring tmp = Rings[i];

                            // circular?
                            if (i == Rings.Count - 1)
                            {
                                Rings.Remove(tmp);
                                Rings.Insert(0, tmp);
                            }
                            else
                            {
                                Rings[i] = Rings[i + 1];
                                Rings[i + 1] = tmp;
                            }
                        }
                        ImGui.PopFont();
                        DrawHelper.SetTooltip("下移");
                    }
                }

                ImGui.EndTable();
            }

            if (_removingRing != null)
            {
                var (didConfirm, didClose) = DrawHelper.DrawConfirmationModal("删除？", $"你确定要删除 \"{_removingRing.Name}\" 环吗？");

                if (didConfirm)
                {
                    Rings.Remove(_removingRing);
                    WotsitHelper.Instance?.Update();
                }

                if (didConfirm || didClose)
                {
                    _removingRing = null;
                }
            }
        }
    }
}