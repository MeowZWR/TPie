using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DelvUI.Helpers;
using ImGuiNET;
using System.Numerics;
using System.Text.RegularExpressions;
using TPie.Helpers;
using TPie.Models;
using TPie.Models.Elements;

namespace TPie.Config
{
    public class RingSettingsWindow : Window
    {
        private Ring? _ring = null;
        public Ring? Ring
        {
            get => _ring;
            set
            {
                _selectedIndex = -1;
                _ring?.EndPreview();
                _ring = value;
            }
        }

        private int _selectedIndex = -1;
        private float _scale => ImGuiHelpers.GlobalScale;

        private Vector2 _windowPos = Vector2.Zero;
        private Vector2 ItemWindowPos => _windowPos + new Vector2(410 * _scale, 0);

        public RingSettingsWindow(string name) : base(name)
        {
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(400, 470);

            PositionCondition = ImGuiCond.Appearing;
        }

        public override void PreDraw()
        {
            if (Ring == null || !Plugin.Settings.Rings.Contains(Ring))
            {
                IsOpen = false;
            }
        }

        public override void Draw()
        {
            if (Ring == null) return;

            _windowPos = ImGui.GetWindowPos();

            // ring preview
            Vector2 margin = new Vector2(20 * _scale);
            Vector2 ringCenter = _windowPos + new Vector2(Size!.Value.X * _scale + Ring.Radius + margin.X, Size!.Value.Y * _scale / 2f);
            Ring.Preview(ringCenter);

            float infoHeight = Ring.KeyBind.Toggle ? 190 : 164;

            // info
            ImGui.BeginChild("##Ring_Info", new Vector2(384 * _scale, infoHeight * _scale), true);
            {
                ImGui.PushItemWidth(310 * _scale);

                if (ImGui.InputText("名称 ##Ring_Info_Name", ref Ring.Name, 100))
                {
                    WotsitHelper.Instance?.Update();
                }

                Vector3 color = new Vector3(Ring.Color.X, Ring.Color.Y, Ring.Color.Z);
                if (ImGui.ColorEdit3("颜色 ##Ring_Info_Color", ref color))
                {
                    Ring.Color = new Vector4(color.X, color.Y, color.Z, 1);
                }

                if (ImGui.Button(Ring.KeyBind.Description(), new Vector2(308 * _scale, 24)))
                {
                    Plugin.ShowKeyBindWindow(ImGui.GetMousePos(), Ring);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 4);
                ImGui.Text("快捷键");

                ImGui.PushItemWidth(128 * _scale);
                ImGui.DragFloat("环半径 ##Ring_Info_Radius", ref Ring.Radius, 1, 150, 500);

                ImGui.SameLine();
                ImGui.DragFloat("旋转 ##Ring_Info_Rotation", ref Ring.Rotation, .5f, -359, 359);

                ImGui.PushItemWidth(310 * _scale);
                ImGui.DragFloat2("图标尺寸 ##Ring_Info_ItemSize", ref Ring.ItemSize, 1, 10, 500);

                ImGui.Checkbox("指示线", ref Ring.DrawLine);

                ImGui.SameLine();
                ImGui.Checkbox("选区背景", ref Ring.DrawSelectionBackground);

                ImGui.SameLine();
                ImGui.Checkbox("鼠标提示", ref Ring.ShowTooltips);
                DrawHelper.SetTooltip("当鼠标悬停在元件图标上方时，将显示带有元件描述的提示。");

                if (Ring.KeyBind.Toggle)
                {
                    ImGui.Checkbox("仅在单击时执行操作", ref Ring.PreventActionOnClose);
                    DrawHelper.SetTooltip("启用后，将鼠标悬停在项目上并关闭环将不会执行悬停的操作。");
                }
            }
            ImGui.EndChild();

            // items
            var flags = ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.BordersOuter |
                ImGuiTableFlags.BordersInner |
                ImGuiTableFlags.ScrollY |
                ImGuiTableFlags.SizingFixedSame;

            float tableHeight = Ring.KeyBind.Toggle ? 242 : 268;

            if (ImGui.BeginTable("##Item_Table", 4, flags, new Vector2(354 * _scale, tableHeight * _scale)))
            {
                ImGui.TableSetupColumn("类型", ImGuiTableColumnFlags.WidthStretch, 22, 0);
                ImGui.TableSetupColumn("图标", ImGuiTableColumnFlags.WidthStretch, 8, 1);
                ImGui.TableSetupColumn("描述", ImGuiTableColumnFlags.WidthStretch, 46, 2);
                ImGui.TableSetupColumn("快捷操作", ImGuiTableColumnFlags.WidthStretch, 24, 3);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                for (int i = 0; i < Ring.Items.Count; i++)
                {
                    RingElement item = Ring.Items[i];

                    ImGui.PushID(i.ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                    // type
                    if (ImGui.TableSetColumnIndex(0))
                    {
                        if (ImGui.Selectable(item.UserFriendlyName(), _selectedIndex == i, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap, new Vector2(0, 24)))
                        {
                            _selectedIndex = i;
                            ShowEditItemWindow();
                        }
                    }

                    // icon
                    if (ImGui.TableSetColumnIndex(1))
                    {
                        IDalamudTextureWrap? texture = TexturesHelper.GetTextureFromIconId(item.IconID, item.isHQ());
                        if (texture != null)
                        {
                            ImGui.Image(texture.ImGuiHandle, new Vector2(24));
                        }
                    }

                    // description
                    if (ImGui.TableSetColumnIndex(2))
                    {
                        bool valid = item.IsValid();
                        Vector4 c = valid ? Vector4.One : new(1, 0, 0, 1);
                        ImGui.TextColored(c, item.Description());

                        if (!valid)
                        {
                            DrawHelper.SetTooltip(item.InvalidReason());
                        }
                    }

                    // quick action
                    if (ImGui.TableSetColumnIndex(3))
                    {
                        if (item is not NestedRingElement)
                        {
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 25);

                            bool active = Ring.QuickActionIndex == i;
                            if (ImGui.Checkbox("", ref active))
                            {
                                Ring.QuickActionIndex = active ? i : -1;
                            }
                        }
                    }
                }

                ImGui.EndTable();
            }

            float buttonsStartY = Ring.KeyBind.Toggle ? infoHeight + 50 : infoHeight + 66;

            ImGui.SetCursorPos(new Vector2(369 * _scale, buttonsStartY * _scale));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
            {
                ImGui.OpenPopup("##TPie_Add_Item_Menu");
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("添加");

            if (_selectedIndex >= 0)
            {
                ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 30) * _scale));
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Pen.ToIconString()))
                {
                    ShowEditItemWindow();
                }
                ImGui.PopFont();
                DrawHelper.SetTooltip("编辑");

                ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 60) * _scale));
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString()))
                {
                    Ring.Items.RemoveAt(_selectedIndex);

                    if (Ring.QuickActionIndex == _selectedIndex)
                    {
                        Ring.QuickActionIndex = -1;
                    }

                    _selectedIndex = -1;
                }
                ImGui.PopFont();
                DrawHelper.SetTooltip("删除");

                int count = Ring.Items.Count;
                if (count > 0)
                {
                    ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 150) * _scale));
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.ArrowUp.ToIconString()))
                    {
                        var tmp = Ring.Items[_selectedIndex];
                        bool moveQuickActionIndex = _selectedIndex == Ring.QuickActionIndex;

                        // circular?
                        if (_selectedIndex == 0)
                        {
                            Ring.Items.Remove(tmp);
                            Ring.Items.Add(tmp);
                            _selectedIndex = count - 1;
                        }
                        else
                        {
                            Ring.Items[_selectedIndex] = Ring.Items[_selectedIndex - 1];
                            Ring.Items[_selectedIndex - 1] = tmp;
                            _selectedIndex--;
                        }

                        if (moveQuickActionIndex)
                        {
                            Ring.QuickActionIndex = _selectedIndex;
                        }
                    }
                    ImGui.PopFont();
                    DrawHelper.SetTooltip("上移");

                    ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 180) * _scale));
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.ArrowDown.ToIconString()))
                    {
                        var tmp = Ring.Items[_selectedIndex];
                        bool moveQuickActionIndex = _selectedIndex == Ring.QuickActionIndex;

                        // circular?
                        if (_selectedIndex == count - 1)
                        {
                            Ring.Items.Remove(tmp);
                            Ring.Items.Insert(0, tmp);
                            _selectedIndex = 0;
                        }
                        else
                        {
                            Ring.Items[_selectedIndex] = Ring.Items[_selectedIndex + 1];
                            Ring.Items[_selectedIndex + 1] = tmp;
                            _selectedIndex++;
                        }

                        if (moveQuickActionIndex)
                        {
                            Ring.QuickActionIndex = _selectedIndex;
                        }
                    }
                    ImGui.PopFont();
                    DrawHelper.SetTooltip("下移");
                }
            }

            DrawAddItemMenu();
        }

        private void DrawAddItemMenu()
        {
            if (Ring == null) return;

            ImGui.SetNextWindowSize(new(94 * _scale, 150 * _scale));

            if (ImGui.BeginPopup("##TPie_Add_Item_Menu"))
            {
                RingElement? elementToAdd = null;

                if (ImGui.Selectable("技能"))
                {
                    elementToAdd = new ActionElement();
                }

                if (ImGui.Selectable("道具"))
                {
                    elementToAdd = new ItemElement();
                }

                if (ImGui.Selectable("套装"))
                {
                    elementToAdd = new GearSetElement();
                }

                if (ImGui.Selectable("命令"))
                {
                    elementToAdd = new CommandElement();
                }

                if (ImGui.Selectable("游戏宏"))
                {
                    elementToAdd = new GameMacroElement();
                }

                if (ImGui.Selectable("表情"))
                {
                    elementToAdd = new EmoteElement();
                }

                if (ImGui.Selectable("嵌套环"))
                {
                    elementToAdd = new NestedRingElement();
                }

                if (elementToAdd != null)
                {
                    if (Ring.Items.Count > 0 && _selectedIndex >= 0 && _selectedIndex < Ring.Items.Count - 1)
                    {
                        Ring.Items.Insert(_selectedIndex + 1, elementToAdd);
                        _selectedIndex++;
                    }
                    else
                    {
                        Ring.Items.Add(elementToAdd);
                        _selectedIndex = Ring.Items.Count - 1;
                    }

                    ShowEditItemWindow();
                }

                ImGui.EndPopup();
            }
        }

        private void ShowEditItemWindow()
        {
            if (Ring == null || _selectedIndex < 0 || _selectedIndex >= Ring.Items.Count) return;

            RingElement element = Ring.Items[_selectedIndex];
            Plugin.ShowElementWindow(ItemWindowPos, Ring, element);
        }

        public override void OnClose()
        {
            Ring = null;
            _selectedIndex = -1;

            Settings.Save(Plugin.Settings);
        }

        private static string UserFriendlyString(string str, string? remove)
        {
            string? s = remove != null ? str.Replace(remove, "") : str;

            Regex? regex = new(@"
                    (?<=[A-Z])(?=[A-Z][a-z]) |
                    (?<=[^A-Z])(?=[A-Z]) |
                    (?<=[A-Za-z])(?=[^A-Za-z])",
                RegexOptions.IgnorePatternWhitespace);

            return regex.Replace(s, " ");
        }
    }
}
