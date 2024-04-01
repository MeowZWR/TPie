using Dalamud.Interface;
using Dalamud.Interface.Internal;
using DelvUI.Helpers;
using ImGuiNET;
using System.Numerics;
using System.Text.RegularExpressions;
using TPie.Helpers;
using TPie.Models.Elements;

namespace TPie.Config
{
    public class NestedRingElementWindow : RingElementWindow
    {
        private NestedRingElement? _nestedRingElement = null;
        public NestedRingElement? NestedRingElement
        {
            get => _nestedRingElement;
            set
            {
                _nestedRingElement = value;

                _inputText = value != null ? value.RingName : "";
                _iconInputText = value != null ? $"{value.IconID}" : "";
            }
        }

        protected override RingElement? Element
        {
            get => NestedRingElement;
            set => NestedRingElement = value is NestedRingElement o ? o : null;
        }

        protected string _commandInputText = "";
        protected string _iconInputText = "";

        public NestedRingElementWindow(string name) : base(name)
        {

        }

        public override void Draw()
        {
            if (NestedRingElement == null) return;

            ImGui.PushItemWidth(210 * _scale);

            // name
            FocusIfNeeded();
            if (ImGui.InputText("环名称 ##NestedRing", ref _inputText, 100))
            {
                NestedRingElement.RingName = _inputText;
            }

            // activation time
            ImGui.PushItemWidth(182 * _scale);
            ImGui.DragFloat("激活时间 ##NestedRing", ref NestedRingElement.ActivationTime, 0.1f, 0.2f, 5f);
            DrawHelper.SetTooltip("这确定了悬停在元素上需要多少秒才能激活嵌套环。");

            // keep center
            ImGui.Checkbox("保持在上一个环的中心", ref NestedRingElement.KeepCenter);

            ImGui.NewLine();

            // icon id
            ImGui.PushItemWidth(154 * _scale);
            string str = _iconInputText;
            if (ImGui.InputText("图标 ID ##Command", ref str, 100, ImGuiInputTextFlags.CharsDecimal))
            {
                _iconInputText = Regex.Replace(str, @"[^\d]", "");

                try
                {
                    NestedRingElement.IconID = uint.Parse(_iconInputText);
                }
                catch
                {
                    NestedRingElement.IconID = 0;
                }
            }

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button("\uf2f9"))
            {
                NestedRingElement.IconID = 66001;
                _iconInputText = "66001";
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("重置为默认");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Search.ToIconString()))
            {
                Plugin.ShowIconBrowserWindow(NestedRingElement.IconID, (iconId) =>
                {
                    NestedRingElement.IconID = iconId;
                    _iconInputText = $"{iconId}";
                });
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("搜索 ");

            ImGui.NewLine();

            // icon
            if (NestedRingElement.IconID > 0)
            {
                IDalamudTextureWrap? texture = TexturesHelper.GetTextureFromIconId(NestedRingElement.IconID);
                if (texture != null)
                {
                    ImGui.SetCursorPosX(110 * _scale);
                    ImGui.Image(texture.ImGuiHandle, new Vector2(80 * _scale));
                }
            }

            // draw text
            ImGui.NewLine();
            ImGui.Checkbox("绘制文字", ref NestedRingElement.DrawText);

            // border
            NestedRingElement.Border.Draw();
        }
    }
}
