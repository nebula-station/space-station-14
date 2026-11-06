// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// Passes when the character picked one of the allowed values for a consent tag.
/// Works on bodies and on minds, a mind is checked through its body.
/// </summary>
public sealed partial class ErpConsentEntityConditionSystem : EntityConditionSystem<MetaDataComponent, ErpConsentCondition>
{
    [Dependency] private ErpConsentSystem _consent = default!;

    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<ErpConsentCondition> args)
    {
        var uid = entity.Owner;
        if (TryComp<MindComponent>(uid, out var mind))
        {
            if (mind.OwnedEntity is not { } body)
                return;

            uid = body;
        }

        args.Result = args.Condition.Allowed.Contains(_consent.GetConsent(uid, args.Condition.Tag));
    }
}

public sealed partial class ErpConsentCondition : EntityConditionBase<ErpConsentCondition>
{
    [DataField(required: true)]
    public ProtoId<ErpConsentTagPrototype> Tag;

    [DataField]
    public List<ErpConsent> Allowed = new() { ErpConsent.Yes, ErpConsent.Ask };

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}
