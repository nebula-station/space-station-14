// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Bitrunning/Systems/BitrunningDomainSystem.cs

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed partial class BitrunningDomainSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly List<BitrunningVirtualDomainPrototype> _allDomains = [];

    public override void Initialize()
    {
        base.Initialize();

        ReloadDomains();
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.TryGetValue(typeof(BitrunningVirtualDomainPrototype), out _))
            return;

        ReloadDomains();
    }

    private void ReloadDomains()
    {
        _allDomains.Clear();

        foreach (var domain in _prototype.EnumeratePrototypes<BitrunningVirtualDomainPrototype>())
        {
            _allDomains.Add(domain);
        }

        // Nebula start
        _allDomains.Sort((a, b) =>
        {
            var byDifficulty = a.Difficulty.CompareTo(b.Difficulty);
            if (byDifficulty != 0)
                return byDifficulty;

            var byCost = a.Cost.CompareTo(b.Cost);
            return byCost != 0 ? byCost : string.CompareOrdinal(a.ID, b.ID);
        });
        // Nebula end
    }

    public IReadOnlyList<BitrunningVirtualDomainPrototype> GetAllDomains()
    {
        return _allDomains.AsReadOnly();
    }

    public bool TryGetDomain(string id, [NotNullWhen(true)] out BitrunningVirtualDomainPrototype? domain)
    {
        return _prototype.TryIndex(id, out domain);
    }

    public string GetDisplayName(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (CanViewName(domain, scannerTier, points))
            return Loc.GetString(domain.Name);

        return Loc.GetString("bitrunning-console-redacted");
    }

    public string GetDisplayDescription(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (CanViewName(domain, scannerTier, points))
            return Loc.GetString(domain.Description);

        return Loc.GetString("bitrunning-console-redacted-desc");
    }

    public string GetDisplayReward(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (CanViewReward(domain, scannerTier, points))
        {
            return Loc.GetString("bitrunning-ui-domain-reward",
                ("server", domain.ServerRewardPoints),
                ("np", domain.BitrunningRewardPoints));
        }

        return Loc.GetString("bitrunning-console-redacted");
    }

    private static bool CanViewName(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (!domain.HiddenUntilScanned)
            return true;

        return scannerTier >= domain.RequiredScannerTier && points + domain.NameRevealPointBuffer >= domain.Cost;
    }

    private static bool CanViewReward(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        return scannerTier >= domain.RequiredScannerTier + 1 && points >= domain.RequiredPointsToRevealReward;
    }
}
