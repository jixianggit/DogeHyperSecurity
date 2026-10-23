using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DogeHyperSecurity.Interop;

namespace DogeHyperSecurity.Services;

internal static class GameLauncher
{
    // 防止 Process 对象被回收后丢失异步读取回调
    private static readonly List<Process> Running = new();
    private static readonly object Sync = new();

    // 登录成功后，才把内嵌的外挂程序释放到临时目录
    private static string? ExtractOverlay()
    {
        var assembly = typeof(GameLauncher).Assembly;
        string[] resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("Payload.", StringComparison.Ordinal))
            .ToArray();

        if (resources.Length == 0)
        {
            return null;
        }

        string overlayRoot = Path.Combine(Path.GetTempPath(), "DogeHyperSecurity", "overlay");
        Directory.CreateDirectory(overlayRoot);

        string targetDirectory = Path.Combine(overlayRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(targetDirectory);

        foreach (string resource in resources)
        {
            string fileName = resource["Payload.".Length..];
            if (fileName.Length == 0)
            {
                continue;
            }

            using Stream? input = assembly.GetManifestResourceStream(resource);
            if (input is null)
            {
                continue;
            }

            string filePath = Path.Combine(targetDirectory, fileName);
            using FileStream output = File.Create(filePath);
            input.CopyTo(output);
        }

        return Directory.EnumerateFiles(targetDirectory, "*.exe", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    // 程序启动时清理上一次登录释放的外挂目录
    internal static void CleanupOverlayCache()
    {
        string overlayRoot = Path.Combine(Path.GetTempPath(), "DogeHyperSecurity", "overlay");
        if (!Directory.Exists(overlayRoot))
        {
            return;
        }

        foreach (string directory in Directory.EnumerateDirectories(overlayRoot))
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                // 仍被运行中的外挂占用时跳过
            }
        }
    }

    public static QpResult Launch(string cardKey)
    {
        string? exePath = ExtractOverlay();
        if (exePath is null)
        {
            return new QpResult(false, "内嵌资源中未找到可执行程序");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardInput = true,
            };

            Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return new QpResult(false, "外挂进程启动失败");
            }

            process.StandardInput.WriteLine(cardKey);
            process.StandardInput.Close();

            lock (Sync)
            {
                Running.Add(process);
            }

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                lock (Sync)
                {
                    Running.Remove(process);
                }
            };

            return new QpResult(true, "外挂已启动");
        }
        catch (Exception ex)
        {
            return new QpResult(false, ex.Message);
        }
    }
}
