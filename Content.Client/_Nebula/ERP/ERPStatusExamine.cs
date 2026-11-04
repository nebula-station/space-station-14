// SPDX-FileCopyrightText: 2026 Lytheriia
// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Examine;
using Content.Shared._Nebula.DetailExaminable;
using Content.Shared._Nebula.ERP;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.ERP;

/// <summary>
/// Examine verb showing the target's ERP consent tags.
/// </summary>
public sealed partial class ERPStatusExamine : EntitySystem
{
    [Dependency] private ExamineSystem _examine = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(
        new("/Textures/_Nebula/Interface/ERP/heartIcon.png"));

    private static readonly Color YesColor = Color.FromHex("#44ff44");
    private static readonly Color AskColor = Color.FromHex("#ffcc00");
    private static readonly Color NoColor = Color.FromHex("#ff4444");
    private static readonly Color ChipColor = Color.FromHex("#252525");
    private static readonly Color HeaderLineColor = Color.FromHex("#4a6fa5");

    private const float ChipWidth = 340;
    private const float ColumnSeparation = 6;
    private const float TooltipWidth = ChipWidth * 2 + ColumnSeparation + 24;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ERPManagerComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<ERPManagerComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var user = args.User;
        var inRange = _examine.IsInDetailsRange(user, ent);

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var data = CompOrNull<HumanoidProfileComponent>(ent)?.DetailExaminableData ?? new DetailExaminableData();
                _examine.SendExamineTooltip(user, ent, BuildConsentControl(data), false, false, TooltipWidth);
            },
            Text = Loc.GetString("erp-examine-status-text"),
            Category = VerbCategory.Examine,
            Disabled = !inRange,
            Message = inRange ? null : Loc.GetString("erp-examine-verb-disabled"),
            Icon = VerbIcon,
            ClientExclusive = true,
        };

        args.Verbs.Add(verb);
    }

    private Control BuildConsentControl(DetailExaminableData data)
    {
        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(4, 4, 4, 6),
        };

        root.AddChild(new Label
        {
            Text = Loc.GetString("erp-consent-tags-examine-header"),
            StyleClasses = { "LabelHeading" },
        });
        root.AddChild(new PanelContainer
        {
            MinSize = new(0, 2),
            Margin = new Thickness(0, 2, 0, 6),
            PanelOverride = new StyleBoxFlat { BackgroundColor = HeaderLineColor },
        });

        var columns = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = (int) ColumnSeparation,
        };
        var left = CreateColumn();
        var right = CreateColumn();
        columns.AddChild(left);
        columns.AddChild(right);
        root.AddChild(columns);

        var index = 0;
        foreach (var tag in _prototypes.EnumeratePrototypes<ErpConsentTagPrototype>().OrderBy(t => t.Order))
        {
            var consent = data.GetConsent(tag);
            if (tag.Requires is { } required
                && _prototypes.TryIndex(required, out var requiredTag)
                && data.GetConsent(requiredTag) != ErpConsent.Yes)
                continue;

            var color = consent switch
            {
                ErpConsent.Yes => YesColor,
                ErpConsent.Ask => AskColor,
                _ => NoColor,
            };

            var column = index++ % 2 == 0 ? left : right;
            column.AddChild(CreateChip(Loc.GetString(tag.Name),
                Loc.GetString($"erp-consent-option-{consent}"),
                color,
                Loc.GetString(tag.Description)));
        }

        return root;
    }

    private static BoxContainer CreateColumn()
    {
        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinWidth = ChipWidth,
            MaxWidth = ChipWidth,
            SeparationOverride = 6,
        };
    }

    private static Control CreateChip(string name, string value, Color color, string tooltip)
    {
        var chip = new PanelContainer
        {
            HorizontalExpand = true,
            ToolTip = tooltip,
            MouseFilter = Control.MouseFilterMode.Pass,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = ChipColor,
                BorderColor = color,
                BorderThickness = new Thickness(3, 0, 0, 0),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
                ContentMarginTopOverride = 3,
                ContentMarginBottomOverride = 3,
            },
        };

        var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true };
        row.AddChild(new Label { Text = name, FontColorOverride = Color.LightGray, HorizontalExpand = true, ClipText = true });
        row.AddChild(new Label { Text = value, FontColorOverride = color, Margin = new Thickness(12, 0, 0, 0) });

        chip.AddChild(row);
        return chip;
    }
}
