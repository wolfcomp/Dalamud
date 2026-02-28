using System.Diagnostics;
using System.Text;

using Dalamud.Interface.Utility;

using Hexa.NET.ImGui;

namespace Dalamud.Interface.Internal;

#pragma warning disable SA1600
internal static unsafe class ImGuiInputTextStatePtrExtensions
{
    public static (int Start, int End, int Cursor) GetSelectionTuple(this ImGuiInputTextStatePtr self) =>
        (self.Stb.SelectStart, self.Stb.SelectEnd, self.Stb.Cursor);

    public static void SetSelectionTuple(this ImGuiInputTextStatePtr self, (int Start, int End, int Cursor) value) =>
        (self.Stb.SelectStart, self.Stb.SelectEnd, self.Stb.Cursor) = value;

    public static void SetSelectionRange(this ImGuiInputTextStatePtr self, int offset, int length, int relativeCursorOffset)
    {
        self.Stb.SelectStart = offset;
        self.Stb.SelectEnd = offset + length;
        if (relativeCursorOffset >= 0)
            self.Stb.Cursor = self.Stb.SelectStart + relativeCursorOffset;
        else
            self.Stb.Cursor = self.Stb.SelectEnd + 1 + relativeCursorOffset;
        self.SanitizeSelectionRange();
    }

    public static void SanitizeSelectionRange(this ImGuiInputTextStatePtr self)
    {
        ref var s = ref self.Stb.SelectStart;
        ref var e = ref self.Stb.SelectEnd;
        ref var c = ref self.Stb.Cursor;
        s = Math.Clamp(s, 0, self.TextLen);
        e = Math.Clamp(e, 0, self.TextLen);
        c = Math.Clamp(c, 0, self.TextLen);
        if (s == e)
            s = e = c;
        if (s > e)
            (s, e) = (e, s);
    }

    public static void Undo(this ImGuiInputTextStatePtr self) => ImGui.ImGuiInputTextStateOnKeyPressed(self, 0x20000A); // #define STB_TEXTEDIT_K_UNDO         0x20000A // keyboard input to perform undo

    public static bool ReplaceSelectionAndPushUndo(this ImGuiInputTextStatePtr self, ReadOnlySpan<byte> newText)
    {
        var off = self.Stb.SelectStart;
        var len = self.Stb.SelectEnd - self.Stb.SelectStart;
        return self.ReplaceChars(off, len, newText);
    }

    public static bool ReplaceChars(this ImGuiInputTextStatePtr self, int pos, int len, ReadOnlySpan<byte> newText)
    {
        self.DeleteChars(pos, len);
        return self.InsertChars(pos, newText);
    }

    // See imgui_widgets.cpp: STB_TEXTEDIT_DELETECHARS
    public static void DeleteChars(this ImGuiInputTextStatePtr self, int pos, int n)
    {
        if (n == 0)
            return;

        var charArr = new char[Encoding.UTF8.GetCharCount(self.TextA.Data, self.TextA.Capacity)];
        Encoding.UTF8.GetChars(new Span<byte>(self.TextA.Data, self.TextA.Capacity), charArr);

        fixed (char* charPtr = charArr)
        {
            var dst = charPtr + pos;

            // We maintain our buffer length in both UTF-8 and wchar formats
            self.Edited = true;
            self.TextLen -= Encoding.UTF8.GetByteCount(dst, n);

            // Offset remaining text (FIXME-OPT: Use memmove)
            var src = charPtr + pos + n;
            int i;
            for (i = 0; src[i] != 0; i++)
                dst[i] = src[i];
            dst[i] = '\0';
        }

        Encoding.UTF8.GetBytes(charArr, new Span<byte>(self.TextA.Data, self.TextA.Capacity));
    }

    // See imgui_widgets.cpp: STB_TEXTEDIT_INSERTCHARS
    public static bool InsertChars(this ImGuiInputTextStatePtr self, int pos, ReadOnlySpan<byte> newText)
    {
        if (newText.Length == 0)
            return true;

        var isResizable = (self.Flags & ImGuiInputTextFlags.CallbackResize) != 0;
        var textLen = self.TextLen;
        Debug.Assert(pos <= textLen, "pos <= text_len");

        var newTextLenUtf8 = newText.Length;
        if (!isResizable && newTextLenUtf8 + self.TextLen + 1 > self.BufCapacity)
            return false;

        // Grow internal buffer if needed
        if (newText.Length + textLen + 1 > self.TextA.Size)
        {
            if (!isResizable)
                return false;

            Debug.Assert(textLen < self.TextA.Size, "text_len < self.TextA.Length");
            self.TextA.Resize(textLen + Math.Clamp(newText.Length * 4, 32, Math.Max(256, newText.Length)) + 1);
        }

        var text = new Span<byte>(self.TextA.Data, self.TextA.Size);
        if (pos != textLen)
            text[pos..textLen].CopyTo(text[(pos + newText.Length)..]);
        newText.CopyTo(text[pos..]);

        self.Edited = true;
        self.TextLen += newTextLenUtf8;
        self.TextA[self.TextLen] = 0;

        return true;
    }
}
