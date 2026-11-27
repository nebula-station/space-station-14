// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Common.Research;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Research.UI;

[GenerateTypedNameReferences]
public sealed partial class FancyResearchConsoleItem : LayoutContainer
{
    // Public fields
    public TechnologyPrototype Prototype;
    public Action<TechnologyPrototype, ResearchAvailability>? SelectAction;
    public ResearchAvailability Availability;

    // Some visuals
    public static readonly Color DefaultColor = Color.FromHex("#141F2F");
    public static readonly Color DefaultBorderColor = Color.FromHex("#4972A1");
    public static readonly Color DefaultHoveredColor = Color.FromHex("#4972A1");

    public Color Color = DefaultColor;
    public Color BorderColor = DefaultBorderColor;
    public Color HoveredColor = DefaultHoveredColor;

    public FancyResearchConsoleItem(TechnologyPrototype proto, SpriteSystem sprite, IPrototypeManager prototypeManager, ResearchAvailability availability) // Nebula edit
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        Availability = availability;
        Prototype = proto;

        ResearchDisplay.Texture = sprite.Frame0(proto.Icon);

        // Nebula start
        if (prototypeManager.TryIndex(proto.Discipline, out var discipline))
            DisciplineDisplay.Texture = sprite.Frame0(discipline.Icon);
        // Nebula end

        Button.OnPressed += Selected;
        Button.OnDrawModeChanged += UpdateColor;

        (Color, HoveredColor, BorderColor) = availability switch
        {
            ResearchAvailability.Researched => (Color.DarkOliveGreen, Color.PaleGreen, Color.LimeGreen),
            ResearchAvailability.Available => (Color.FromHex("#7c7d2a"), Color.FromHex("#ecfa52"), Color.FromHex("#e8fa25")),
            ResearchAvailability.PrereqsMet => (Color.FromHex("#6b572f"), Color.FromHex("#fad398"), Color.FromHex("#cca031")),
            ResearchAvailability.Unavailable => (Color.DarkRed, Color.PaleVioletRed, Color.Crimson),
            _ => (Color.DarkRed, Color.PaleVioletRed, Color.Crimson)
        };

        UpdateColor();
    }

    private void UpdateColor()
    {
        var panel = (StyleBoxFlat) Panel.PanelOverride!;
        panel.BackgroundColor = Button.IsHovered ? HoveredColor : Color;

        panel.BorderColor = BorderColor;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        Button.OnPressed -= Selected;
    }

    private void Selected(BaseButton.ButtonEventArgs args)
    {
        SelectAction?.Invoke(Prototype, Availability);
    }

    public void SetScale(float scale)
    {
        var box = (BoxContainer) GetChild(0)!;
        box.SetSize = new Vector2(80 * scale, 80 * scale);

        // Nebula start
        var badgeSize = new Vector2(20 * scale, 20 * scale);
        DisciplineBadge.SetSize = badgeSize;
        SetPosition(DisciplineBadge, Vector2.Zero);
        DisciplineDisplay.SetSize = badgeSize;
        // Nebula end
    }
}

public sealed class DrawButton : Button
{
    public event Action? OnDrawModeChanged;

    public DrawButton()
    {
    }

    protected override void DrawModeChanged()
    {
        OnDrawModeChanged?.Invoke();
    }
}
