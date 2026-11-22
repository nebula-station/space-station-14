// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.IO.Compression;
using Robust.Shared.GameStates;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Robust.Shared.Replays.ReplayConstants;

namespace Content.Server._Nebula.Bodycam.Replay;

public sealed class ReplayClipWriter : IDisposable
{
    private const int BatchSize = 1024 * 1024;

    private readonly IRobustSerializer _serializer;
    private readonly MemoryStream _output = new();
    private readonly ZipArchive _zip;
    private readonly ZStdCompressionContext _compression = new();
    private readonly MemoryStream _batch = new(BatchSize * 2);
    private int _index;

    public long CompressedSize { get; private set; }
    public long UncompressedSize { get; private set; }
    public int FileCount => _index;

    public ReplayClipWriter(IRobustSerializer serializer)
    {
        _serializer = serializer;
        _zip = new ZipArchive(_output, ZipArchiveMode.Create, true);
    }

    public void WriteRaw(ResPath file, byte[] data, CompressionLevel level = CompressionLevel.NoCompression)
    {
        var entry = _zip.CreateEntry((ReplayZipFolder / file).ToString(), level);
        using var stream = entry.Open();
        stream.Write(data);
    }

    public void WriteYaml(ResPath file, MappingDataNode node)
    {
        using var memory = new MemoryStream();
        using (var writer = new StreamWriter(memory, leaveOpen: true))
        {
            new YamlStream(new YamlDocument(node.ToYaml())).Save(writer, false);
        }

        WriteRaw(file, memory.ToArray(), CompressionLevel.Optimal);
    }

    public void WriteInit(ReplayMessage init)
    {
        using var memory = new MemoryStream();
        _serializer.SerializeDirect(memory, init);
        WriteRaw(FileInit, memory.ToArray(), CompressionLevel.Optimal);
    }

    public void AddTick(GameState state, ReplayMessage messages)
    {
        _serializer.SerializeDirect(_batch, state);
        _serializer.SerializeDirect(_batch, messages);
        if (_batch.Length >= BatchSize)
            FlushBatch();
    }

    public void Flush()
    {
        FlushBatch();
    }

    public ArraySegment<byte> Finish()
    {
        FlushBatch();
        _zip.Dispose();
        return new ArraySegment<byte>(_output.GetBuffer(), 0, (int) _output.Length);
    }

    private void FlushBatch()
    {
        if (_batch.Length == 0)
            return;

        var length = (int) _batch.Length;
        var source = _batch.GetBuffer().AsSpan(0, length);
        var buffer = new byte[4 + ZStd.CompressBound(length)];
        var compressed = _compression.Compress2(buffer.AsSpan(4), source);
        BitConverter.TryWriteBytes(buffer, length);

        var entry = _zip.CreateEntry((ReplayZipFolder / $"{DataFilePrefix}{_index++}.{Ext}").ToString(), CompressionLevel.NoCompression);
        using (var stream = entry.Open())
        {
            stream.Write(buffer, 0, compressed + 4);
        }

        CompressedSize += compressed;
        UncompressedSize += length;
        _batch.SetLength(0);
    }

    public void Dispose()
    {
        _compression.Dispose();
        _batch.Dispose();
        _output.Dispose();
    }
}
