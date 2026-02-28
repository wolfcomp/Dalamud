using System.Diagnostics.CodeAnalysis;

using Hexa.NET.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class ButtonSettingsEntry : SettingsEntry
{
    private readonly LazyLoc description;
    private readonly Action runs;

    public ButtonSettingsEntry(LazyLoc name, LazyLoc description, Action runs)
    {
        this.description = description;
        this.runs = runs;
        this.Name = name;
    }

    public override void Load()
    {
        // ignored
    }

    public override void Save()
    {
        // ignored
    }

    public override void Draw()
    {
        if (ImGui.Button((ImU8String)this.Name))
        {
            this.runs.Invoke();
        }

        ImGuiHelpers.TextColoredWrapped(ImGuiColors.DalamudGrey, this.description);
    }
}
