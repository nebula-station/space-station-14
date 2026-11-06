// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._Nebula.DetailExaminable;
using Content.Shared._Nebula.ERP;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.ERP;

/// <summary>
/// Flavor text tab for picking a value for each <see cref="ErpConsentTagPrototype"/>.
/// </summary>
public sealed partial class ErpConsentTagsEditor : BoxContainer
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public event Action<string, ErpConsent>? OnConsentChanged;

    private readonly Dictionary<string, OptionButton> _buttons = new();
    private readonly BoxContainer _rows;

    public ErpConsentTagsEditor()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;

        AddChild(new StripeBack
        {
            Children =
            {
                new Label
                {
                    Text = Loc.GetString("erp-consent-tags-header"),
                    StyleClasses = { "LabelBig" },
                    Margin = new Thickness(5, 0, 0, 0),
                },
            },
        });

        AddChild(new Label
        {
            Text = Loc.GetString("erp-consent-tags-hint"),
            FontColorOverride = Color.Gray,
            Margin = new Thickness(10, 5),
        });

        _rows = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(10, 5),
        };

        AddChild(new ScrollContainer
        {
            HScrollEnabled = false,
            VerticalExpand = true,
            Children = { _rows },
        });

        BuildRows();
    }

    private void BuildRows()
    {
        foreach (var tag in _prototypes.EnumeratePrototypes<ErpConsentTagPrototype>().OrderBy(t => t.Order))
        {
            var tooltip = Loc.GetString(tag.Description);
            if (tag.Requires is { } required && _prototypes.TryIndex(required, out var requiredTag))
                tooltip += "\n\n" + Loc.GetString("erp-consent-tags-requires", ("tag", Loc.GetString(requiredTag.Name)));

            var button = new OptionButton { HorizontalAlignment = HAlignment.Right, MinWidth = 120 };
            foreach (var consent in Enum.GetValues<ErpConsent>())
            {
                if (consent == ErpConsent.Ask && !tag.AllowAsk)
                    continue;

                button.AddItem(Loc.GetString($"erp-consent-option-{consent}"), (int) consent);
            }

            var id = tag.ID;
            button.OnItemSelected += args =>
            {
                button.SelectId(args.Id);
                OnConsentChanged?.Invoke(id, (ErpConsent) args.Id);
            };

            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Margin = new Thickness(0, 2),
                ToolTip = tooltip,
                MouseFilter = MouseFilterMode.Pass,
            };
            row.AddChild(new Label { Text = Loc.GetString(tag.Name) });
            row.AddChild(new Control { HorizontalExpand = true });
            row.AddChild(button);

            _rows.AddChild(row);
            _buttons[id] = button;
        }
    }

    public void SetData(DetailExaminableData data)
    {
        foreach (var (id, button) in _buttons)
        {
            if (_prototypes.TryIndex<ErpConsentTagPrototype>(id, out var tag))
                button.SelectId((int) data.GetConsent(tag));
        }
    }
}
