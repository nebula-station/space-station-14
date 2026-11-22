// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using static Robust.Shared.Replays.ReplayConstants;

namespace Content.Server._Nebula.Bodycam.Replay;

public sealed class ReplayStreamReader : IDisposable
{
    private readonly IRobustSerializer _serializer;
    private readonly ReplayZipScanner _zip;

    public ReplayStreamReader(IRobustSerializer serializer, string path)
    {
        _serializer = serializer;
        _zip = new ReplayZipScanner(path);
    }

    public static string Name(ResPath file) => (ReplayZipFolder / file).ToString();

    public bool Exists(ResPath file) => _zip.Exists(Name(file));

    public byte[] ReadRaw(ResPath file) => _zip.ReadBytes(Name(file));

    public MappingDataNode? ReadYaml(ResPath file)
    {
        if (!Exists(file))
            return null;

        using var reader = new StreamReader(new MemoryStream(ReadRaw(file)));
        return DataNodeParser.ParseYamlStream(reader).FirstOrDefault()?.Root as MappingDataNode;
    }

    public ReplayMessage? ReadInit()
    {
        if (!Exists(FileInit))
            return null;

        using var stream = new MemoryStream(ReadRaw(FileInit));
        _serializer.DeserializeDirect(stream, out ReplayMessage init);
        return init;
    }

    public IEnumerable<(GameState State, ReplayMessage Messages)> ReadHistory()
    {
        var intBuf = new byte[4];
        for (var i = 0; ; i++)
        {
            var file = new ResPath($"{DataFilePrefix}{i}.{Ext}");
            if (!Exists(file))
                yield break;

            MemoryStream decompressed;
            try
            {
                using var raw = new MemoryStream(ReadRaw(file));
                raw.ReadExactly(intBuf);
                var size = BitConverter.ToInt32(intBuf);
                using var zstd = new ZStdDecompressStream(raw, false);
                decompressed = new MemoryStream(size);
                zstd.CopyTo(decompressed);
                if (decompressed.Length != size)
                    yield break;
                decompressed.Position = 0;
            }
            catch (Exception)
            {
                yield break;
            }

            while (decompressed.Position < decompressed.Length)
            {
                _serializer.DeserializeDirect(decompressed, out GameState state);
                _serializer.DeserializeDirect(decompressed, out ReplayMessage msg);
                yield return (state, msg);
            }
        }
    }

    public void Dispose()
    {
        _zip.Dispose();
    }
}
