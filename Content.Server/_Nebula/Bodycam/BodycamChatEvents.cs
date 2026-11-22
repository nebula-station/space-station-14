// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language;
using Robust.Shared.Player;

namespace Content.Server._Nebula.Bodycam;

public sealed class BodycamSpeechEvent(
    EntityUid source,
    bool whisper,
    string message,
    string obfuscatedMessage,
    string name,
    string nameIdentity,
    LanguagePrototype language,
    Color? color) : EntityEventArgs
{
    public readonly EntityUid Source = source;
    public readonly bool Whisper = whisper;
    public readonly string Message = message;
    public readonly string ObfuscatedMessage = obfuscatedMessage;
    public readonly string Name = name;
    public readonly string NameIdentity = nameIdentity;
    public readonly LanguagePrototype Language = language;
    public readonly Color? Color = color;
}

public sealed class BodycamAnnouncementEvent(Filter? filter, EntityUid? station, string message, string wrapped, Color? color) : EntityEventArgs
{
    public readonly Filter? Filter = filter;
    public readonly EntityUid? Station = station;
    public readonly string Message = message;
    public readonly string Wrapped = wrapped;
    public readonly Color? Color = color;
}
