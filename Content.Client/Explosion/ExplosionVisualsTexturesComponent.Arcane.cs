// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Client/Explosion/ExplosionVisualsTexturesComponent.cs

namespace Content.Client.Explosion;

public sealed partial class ExplosionVisualsTexturesComponent
{
    /// <summary>
    ///     Frame delays of each fire-state, read from the RSI together with <see cref="FireFrames"/>.
    /// </summary>
    public List<float[]> FireDelays = [];
}
