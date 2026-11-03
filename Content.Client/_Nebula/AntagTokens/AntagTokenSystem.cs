// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.AntagTokens;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.AntagTokens;

public sealed partial class AntagTokenSystem : EntitySystem
{
    /// <summary>
    /// Last state from the server, null until the first answer.
    /// </summary>
    public AntagTokenStateMessage? State { get; private set; }

    public event Action? StateChanged;
    public event Action? RunLevelChanged;

    [SubscribeNetworkEvent]
    private void OnState(AntagTokenStateMessage msg)
    {
        State = msg;
        StateChanged?.Invoke();
    }

    [SubscribeNetworkEvent]
    private void OnOnline(AntagTokenOnlineMessage msg)
    {
        if (State == null)
            return;

        State.Online = msg.Online;
        StateChanged?.Invoke();
    }

    [SubscribeNetworkEvent]
    private void OnRunLevel(AntagTokenRunLevelMessage msg)
    {
        if (State == null)
            return;

        State.InLobby = msg.InLobby;
        StateChanged?.Invoke();
        RunLevelChanged?.Invoke();
    }

    [SubscribeNetworkEvent]
    private void OnError(AntagTokenErrorMessage msg)
    {
        if (State == null)
            return;

        State.Error = msg.Error;
        StateChanged?.Invoke();
    }

    public void RequestState()
    {
        RaiseNetworkEvent(new AntagTokenStateRequest());
    }

    public void Buy(ProtoId<AntagTokenListingPrototype> listing)
    {
        RaiseNetworkEvent(new AntagTokenBuyRequest { Listing = listing });
    }

    public void Cancel()
    {
        RaiseNetworkEvent(new AntagTokenCancelRequest());
    }
}
