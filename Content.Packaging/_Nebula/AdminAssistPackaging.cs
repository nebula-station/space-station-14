// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Robust.Packaging.AssetProcessing;

namespace Content.Packaging;

public static class AdminAssistPackaging
{
    private const string SourceDir = "Tools/_Nebula/admin_assist";
    private static readonly string[] PythonVersions = ["3.12", "3.13", "3.14"];

    public static async Task WriteSidecar(string rid, AssetPass pass, CancellationToken cancel)
    {
        foreach (var (path, file) in Files(SourceDir))
            pass.InjectFileFromDisk($"admin_assist/{path}", file);

        var arch = rid switch { "linux-x64" => "x86_64", "linux-arm64" => "aarch64", _ => null };
        if (arch == null)
            return;

        foreach (var version in PythonVersions)
        {
            var temp = Directory.CreateTempSubdirectory();
            try
            {
                await PipInstall(version, arch, temp.FullName, cancel);
                foreach (var (path, file) in Files(temp.FullName))
                    pass.InjectFileFromMemory($"admin_assist/lib/python{version}/{path}", await File.ReadAllBytesAsync(file, cancel));
            }
            finally
            {
                temp.Delete(true);
            }
        }
    }

    private static async Task PipInstall(string version, string arch, string target, CancellationToken cancel)
    {
        using var pip = Process.Start(OperatingSystem.IsWindows() ? "python" : "python3",
        [
            "-m", "pip", "install", "--quiet", "--no-compile", "--no-deps", "--require-hashes", "--only-binary=:all:",
            "--implementation", "cp", "--python-version", version,
            "--platform", $"manylinux_2_28_{arch}", "--platform", $"manylinux_2_17_{arch}", "--platform", $"manylinux2014_{arch}",
            "--target", target, "--requirement", $"{SourceDir}/requirements.txt",
        ]);
        await pip.WaitForExitAsync(cancel);
        if (pip.ExitCode != 0)
            throw new InvalidOperationException($"pip install of the Admin Assist libraries for Python {version} failed, exit code {pip.ExitCode}");
    }

    private static IEnumerable<(string Path, string File)> Files(string root)
    {
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(file => (Path.GetRelativePath(root, file).Replace('\\', '/'), file))
            .Where(f => !f.Item1.Split('/').Any(part => part.StartsWith('.') || part == "__pycache__"));
    }
}
