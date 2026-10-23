using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using DogeHyperSecurity.Services;

namespace DogeHyperSecurity;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (!EnsureBridgeLoadable())
        {
            Environment.Exit(1);
            return;
        }

        GameLauncher.CleanupOverlayCache();
        base.OnStartup(e);
    }

    // QPBridge.dll 若已用 VMProtect 加壳，则不再依赖 VMProtectSDK64.dll；
    // 未加壳时才需要把 exe 旁边的 VMProtectSDK64.dll 复制到运行时目录。
    private static bool EnsureBridgeLoadable()
    {
        string bridgePath = Path.Combine(AppContext.BaseDirectory, "QPBridge.dll");
        if (!File.Exists(bridgePath))
        {
            ShowError("由于找不到 QPBridge.dll，无法继续执行代码。重新安装程序可能会解决此问题。");
            return false;
        }

        SyncVmProtectSdk();

        if (!NativeLibrary.TryLoad(bridgePath, out _))
        {
            ShowError("由于找不到 VMProtectSDK64.dll，无法继续执行代码。重新安装程序可能会解决此问题。");
            return false;
        }

        return true;
    }

    private static void SyncVmProtectSdk()
    {
        try
        {
            string exeDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            string source = Path.Combine(exeDirectory, "VMProtectSDK64.dll");
            string target = Path.Combine(AppContext.BaseDirectory, "VMProtectSDK64.dll");

            if (File.Exists(source))
            {
                if (!string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(source, target, true);
                }
            }
            else if (File.Exists(target))
            {
                // 外部 DLL 已移除时，清掉上次运行残留在解压目录的副本
                File.Delete(target);
            }
        }
        catch
        {
            // 清理失败时由 NativeLibrary.TryLoad 给出结果
        }
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(
            message,
            "DogeHyperSecurity.exe - 系统错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
