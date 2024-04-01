using Dalamud.Interface.Utility;
using ImGuiNET;
using System.Numerics;

namespace TPie.Models.Elements
{
    public class ItemBorder
    {
        public Vector3 Color;
        public int Thickness;
        public int Radius;

        public ItemBorder(Vector3 color, int thickness, int radius)
        {
            Color = color;
            Thickness = thickness;
            Radius = radius;
        }

        public static ItemBorder Default() => new ItemBorder(Vector3.Zero, 3, 2);

        public void Draw()
        {
            ImGui.PushItemWidth(154 * ImGuiHelpers.GlobalScale);

            ImGui.ColorEdit3("边框颜色 ##ItemBorder", ref Color);
            ImGui.DragInt("边框厚度 ##ItemBorder", ref Thickness, 0.1f, 0, 10);
            ImGui.DragInt("边框半径 ##ItemBorder", ref Radius, 0.1f, 0, 500);
        }

        public ItemBorder Clone()
        {
            return new ItemBorder(Color, Thickness, Radius);
        }

        public static ItemBorder GlobalBorderSettingsCopy()
        {
            return Plugin.Settings?.GlobalBorderSettings.Clone() ?? Default();
        }
    }
}
