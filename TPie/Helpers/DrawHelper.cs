﻿using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Numerics;

namespace TPie.Helpers
{
    internal static class DrawHelper
    {
        public static void DrawIcon(uint iconId, Vector2 position, Vector2 size, float alpha, ImDrawListPtr drawList)
        {
            TextureWrap? texture = TexturesCache.Instance.GetTextureFromIconId(iconId);
            if (texture == null) return;

            uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, alpha));
            drawList.AddImage(texture.ImGuiHandle, position, position + size, Vector2.Zero, Vector2.One, color);
        }

        public static void DrawOutlinedText(string text, Vector2 pos, bool centered, float scale, ImDrawListPtr drawList, int thickness = 1)
        {
            DrawOutlinedText(text, pos, centered, scale, 0xFFFFFFFF, 0xFF000000, drawList, thickness);
        }
        public static void DrawOutlinedText(string text, Vector2 pos, bool centered, float scale, uint color, uint outlineColor, ImDrawListPtr drawList, int thickness = 1)
        {
            FontsHelper.PushFont(scale);

            if (centered)
            {
                Vector2 size = ImGui.CalcTextSize(text);
                pos = pos - size / 2f;
            }

            // outline
            for (int i = 1; i < thickness + 1; i++)
            {
                drawList.AddText(new Vector2(pos.X - i, pos.Y + i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X, pos.Y + i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X + i, pos.Y + i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X - i, pos.Y), outlineColor, text);
                drawList.AddText(new Vector2(pos.X + i, pos.Y), outlineColor, text);
                drawList.AddText(new Vector2(pos.X - i, pos.Y - i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X, pos.Y - i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X + i, pos.Y - i), outlineColor, text);
            }

            // text
            drawList.AddText(new Vector2(pos.X, pos.Y), color, text);

            FontsHelper.PopFont();
        }

        public static void DrawCooldown(ActionType type, uint id, Vector2 position, Vector2 size, float scale, ImDrawListPtr drawList)
        {
            // arc
            float elapsed = CooldownHelper.GetRecastTimeElapsed(type, id);
            float total = CooldownHelper.GetRecastTime(type, id);
            float completion = 1 - (elapsed / total);
            float endAngle = (float)Math.PI * 2f * -completion;
            float offset = (float)Math.PI / 2;

            ImGui.PushClipRect(position - size / 2, position + size / 2, false);
            drawList.PathArcTo(position, size.X / 2, endAngle - offset, -offset, 50);
            drawList.PathStroke(0xCC000000, ImDrawFlags.None, size.X);
            ImGui.PopClipRect();

            // text
            if (elapsed > 0)
            {
                DrawOutlinedText($"{Math.Truncate(total - elapsed)}", position, true, scale, drawList);
            }
        }
    }
}
