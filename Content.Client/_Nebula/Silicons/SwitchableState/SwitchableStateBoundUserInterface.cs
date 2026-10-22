// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.GameObjects;
using Content.Shared._Nebula.Silicons.SwitchableState;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Silicons.SwitchableState;

public sealed partial class SwitchableStateBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        Update();

        _menu.OpenOverMouseScreenPosition();
    }

    public override void Update()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent<BorgSwitchableStateComponent>(Owner, out var comp))
            return;

        var models = GetOptionsFromComp((Owner, comp));

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> GetOptionsFromComp(Entity<BorgSwitchableStateComponent> ent)
    {
        var list = new List<RadialMenuOptionBase>();

        if (!EntMan.TryGetComponent<SpriteComponent>(ent, out var sprite) || sprite.BaseRSI == null)
            return list;

        foreach (var (type, vector) in ent.Comp.StatesWhiteList)
        {
            if (type == ent.Comp.CurrentType)
                continue;

            var (path, state) = ent.Comp.BaseSprite switch
            {
                SpriteSpecifier.Rsi rsi => (rsi.RsiPath, rsi.RsiState),
                SpriteSpecifier.Texture tex => (tex.TexturePath, null),
                _ => throw new NotSupportedException()
            };

            var rsiState = type == BorgStateType.Base ? state
                : $"{state}_{type.ToString().ToLowerInvariant()}";

            if (rsiState == null)
                continue;

            var option = new RadialMenuActionOption<BorgStateType>(SendPredictedMessage, type)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(path, rsiState)),
                ToolTip = Loc.GetString($"borg-switchable-state-ui-{type.ToString().ToLowerInvariant()}")
            };

            list.Add(option);
        }

        return list;
    }

    private void SendPredictedMessage(BorgStateType type)
    {
        SendPredictedMessage(new SwitchStateMessage(type));
    }
}
