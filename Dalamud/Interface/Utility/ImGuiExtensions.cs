using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;

using HexaGen.Runtime;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Class containing various extensions to ImGui, aiding with building custom widgets.
/// </summary>
public static class ImGuiExtensions
{
    /// <summary>
    /// Draw clipped text.
    /// </summary>
    /// <param name="drawListPtr">Pointer to the draw list.</param>
    /// <param name="posMin">Minimum position.</param>
    /// <param name="posMax">Maximum position.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="textSizeIfKnown">Size of the text, if known.</param>
    /// <param name="align">Alignment.</param>
    /// <param name="clipRect">Clip rect to use.</param>
    public static unsafe void AddTextClippedEx(this ImDrawListPtr drawListPtr, Vector2 posMin, Vector2 posMax, string text, Vector2? textSizeIfKnown, Vector2 align, Vector4? clipRect)
    {
        var pos = posMin;
        var textSize = textSizeIfKnown ?? ImGui.CalcTextSize(text, false, 0);

        var clipMin = clipRect.HasValue ? new Vector2(clipRect.Value.X, clipRect.Value.Y) : posMin;
        var clipMax = clipRect.HasValue ? new Vector2(clipRect.Value.Z, clipRect.Value.W) : posMax;

        var needClipping = (pos.X + textSize.X >= clipMax.X) || (pos.Y + textSize.Y >= clipMax.Y);
        if (clipRect.HasValue)
            needClipping |= (pos.X < clipMin.X) || (pos.Y < clipMin.Y);

        if (align.X > 0)
        {
            pos.X = Math.Max(pos.X, pos.X + ((posMax.X - pos.X - textSize.X) * align.X));
        }

        if (align.Y > 0)
        {
            pos.Y = Math.Max(pos.Y, pos.Y + ((posMax.Y - pos.Y - textSize.Y) * align.Y));
        }

        if (needClipping)
        {
            var fineClipRect = new Vector4(clipMin.X, clipMin.Y, clipMax.X, clipMax.Y);
            drawListPtr.AddText(
                ImGui.GetFont(),
                ImGui.GetFontSize(),
                pos,
                ImGui.GetColorU32(ImGuiCol.Text),
                text,
                0,
                &fineClipRect);
        }
        else
        {
            drawListPtr.AddText(ImGui.GetFont(), ImGui.GetFontSize(), pos, ImGui.GetColorU32(ImGuiCol.Text), text);
        }
    }

    extension(STBTexteditStatePtr state)
    {
        public unsafe ref int Cursor => ref ((STBTexteditState*)state.Handle)->Cursor;
        public unsafe ref int SelectStart => ref ((STBTexteditState*)state.Handle)->SelectStart;
        public unsafe ref int SelectEnd => ref ((STBTexteditState*)state.Handle)->SelectEnd;
    }

    extension(ImGui)
    {
        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "funcTable")]
        private static extern ref FunctionTable GetFunctionTable();

        public static unsafe void ImGuiInputTextStateOnKeyPressed(ImGuiInputTextState* self, int key) => ((delegate* unmanaged<ImGuiInputTextState*, int, void>)GetFunctionTable()[860])(self, key);

        public static unsafe ImRect* WindowTitleBarRect(ImGuiWindow* self) => ((delegate* unmanaged<ImGuiWindow*, ImRect*>)GetFunctionTable()[969])(self);
        public static unsafe bool RectContains(ImRect* self, Vector2 vec) => ((delegate* unmanaged<ImRect*, Vector2, bool>)GetFunctionTable()[969])(self, vec);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct STBTexteditState
{
    public int Cursor;
    public int SelectStart;
    public int SelectEnd;
    public byte InsertMode;
    int RowCountPerPage;
}
