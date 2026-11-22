// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Content.Server._Nebula.Bodycam.Replay;

public sealed class ReplayZipScanner : IDisposable
{
    private const uint LocalHeaderSignature = 0x04034b50;
    private const int LocalHeaderSize = 30;

    private readonly FileStream _file;
    private readonly Dictionary<string, Entry> _entries = new();

    public IReadOnlyDictionary<string, Entry> Entries => _entries;

    public readonly record struct Entry(long DataOffset, long CompressedSize, long UncompressedSize, ushort Method);

    public ReplayZipScanner(string path)
    {
        _file = OpenShared(path);
        Scan();
    }

    public bool Exists(string name) => _entries.ContainsKey(name);

    public byte[] ReadBytes(string name)
    {
        var entry = _entries[name];
        var raw = new byte[entry.CompressedSize];
        _file.Position = entry.DataOffset;
        _file.ReadExactly(raw);

        if (entry.Method == 0)
            return raw;

        using var input = new MemoryStream(raw);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        var output = new MemoryStream(entry.UncompressedSize > 0 ? (int) entry.UncompressedSize : raw.Length * 2);
        deflate.CopyTo(output);
        return output.ToArray();
    }

    public void Dispose()
    {
        _file.Dispose();
    }

    private void Scan()
    {
        var header = new byte[LocalHeaderSize];
        var length = _file.Length;
        long position = 0;

        while (position + LocalHeaderSize <= length)
        {
            _file.Position = position;
            _file.ReadExactly(header);

            if (BitConverter.ToUInt32(header, 0) != LocalHeaderSignature)
                break;

            var flags = BitConverter.ToUInt16(header, 6);
            var method = BitConverter.ToUInt16(header, 8);
            long compressed = BitConverter.ToUInt32(header, 18);
            long uncompressed = BitConverter.ToUInt32(header, 22);
            var nameLength = BitConverter.ToUInt16(header, 26);
            var extraLength = BitConverter.ToUInt16(header, 28);

            if ((flags & 0x8) != 0 || method is not (0 or 8))
                break;

            var nameBytes = new byte[nameLength];
            _file.ReadExactly(nameBytes);
            var extra = new byte[extraLength];
            _file.ReadExactly(extra);

            if (compressed == uint.MaxValue || uncompressed == uint.MaxValue)
            {
                if (!TryReadZip64(extra, ref compressed, ref uncompressed))
                    break;
            }

            var dataOffset = position + LocalHeaderSize + nameLength + extraLength;
            if (compressed <= 0 || dataOffset + compressed > length)
                break;

            var name = System.Text.Encoding.UTF8.GetString(nameBytes);
            _entries[name] = new Entry(dataOffset, compressed, uncompressed, method);
            position = dataOffset + compressed;
        }
    }

    private static bool TryReadZip64(byte[] extra, ref long compressed, ref long uncompressed)
    {
        var i = 0;
        while (i + 4 <= extra.Length)
        {
            var id = BitConverter.ToUInt16(extra, i);
            var size = BitConverter.ToUInt16(extra, i + 2);
            if (id == 0x0001 && i + 4 + size <= extra.Length)
            {
                var offset = i + 4;
                if (uncompressed == uint.MaxValue && offset + 8 <= i + 4 + size)
                {
                    uncompressed = BitConverter.ToInt64(extra, offset);
                    offset += 8;
                }

                if (compressed == uint.MaxValue && offset + 8 <= i + 4 + size)
                    compressed = BitConverter.ToInt64(extra, offset);

                return compressed != uint.MaxValue && uncompressed != uint.MaxValue;
            }

            i += 4 + size;
        }

        return false;
    }

    private static FileStream OpenShared(string path)
    {
        if (!OperatingSystem.IsLinux())
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        var fd = LibcOpen(path, 0);
        if (fd < 0)
            throw new IOException($"Could not open {path} (errno {Marshal.GetLastPInvokeError()})");

        return new FileStream(new SafeFileHandle(fd, true), FileAccess.Read);
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int LibcOpen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);
}
