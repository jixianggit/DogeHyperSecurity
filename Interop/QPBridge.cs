using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DogeHyperSecurity.Interop;

internal static class NativeQp
{
    private const string Dll = "QPBridge.dll";

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int QPBridge_Init();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int QPBridge_KeyLogin(
        [MarshalAs(UnmanagedType.LPStr)] string key,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder expireText,
        int expireChars,
        out int remainSeconds);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int QPBridge_UserLogin(
        [MarshalAs(UnmanagedType.LPStr)] string user,
        [MarshalAs(UnmanagedType.LPStr)] string password,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder expireText,
        int expireChars,
        out int remainSeconds);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int QPBridge_UserRegister(
        [MarshalAs(UnmanagedType.LPStr)] string user,
        [MarshalAs(UnmanagedType.LPStr)] string password,
        [MarshalAs(UnmanagedType.LPStr)] string superPassword,
        [MarshalAs(UnmanagedType.LPStr)] string cardKey,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder surplusText,
        int surplusChars);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int QPBridge_UserPay(
        [MarshalAs(UnmanagedType.LPStr)] string user,
        [MarshalAs(UnmanagedType.LPStr)] string cardKey,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder surplusText,
        int surplusChars);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int QPBridge_UserSetPass(
        [MarshalAs(UnmanagedType.LPStr)] string user,
        [MarshalAs(UnmanagedType.LPStr)] string superPassword,
        [MarshalAs(UnmanagedType.LPStr)] string newPassword);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void QPBridge_GetLastErrorW(
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer, int chars);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void QPBridge_GetBulletinW(
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer, int chars);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void QPBridge_LeaveLogin();
}

internal readonly record struct QpResult(bool Success, string Message);

internal static class QpAuth
{
    private static readonly object Gate = new();

    public static QpResult CardLogin(string key)
    {
        return Guard(() =>
        {
            lock (Gate)
            {
                if (!EnsureInit(out string initError))
                {
                    return new QpResult(false, initError);
                }

                var expire = new StringBuilder(64);
                if (NativeQp.QPBridge_KeyLogin(key, expire, expire.Capacity, out _) != 0)
                {
                    return new QpResult(true, expire.ToString());
                }

                return new QpResult(false, ReadLastError("卡密验证失败"));
            }
        });
    }

    public static QpResult AccountLogin(string user, string password)
    {
        return Guard(() =>
        {
            lock (Gate)
            {
                if (!EnsureInit(out string initError))
                {
                    return new QpResult(false, initError);
                }

                var expire = new StringBuilder(64);
                if (NativeQp.QPBridge_UserLogin(user, password, expire, expire.Capacity, out _) != 0)
                {
                    return new QpResult(true, expire.ToString());
                }

                return new QpResult(false, ReadLastError("账号或密码错误"));
            }
        });
    }

    public static QpResult Register(string user, string password, string superPassword, string cardKey)
    {
        return Guard(() =>
        {
            lock (Gate)
            {
                if (!EnsureInit(out string initError))
                {
                    return new QpResult(false, initError);
                }

                var surplus = new StringBuilder(64);
                if (NativeQp.QPBridge_UserRegister(user, password, superPassword, cardKey,
                        surplus, surplus.Capacity) != 0)
                {
                    return new QpResult(true, Compose("注册成功", surplus.ToString()));
                }

                return new QpResult(false, ReadLastError("注册失败"));
            }
        });
    }

    public static QpResult Recharge(string user, string cardKey)
    {
        return Guard(() =>
        {
            lock (Gate)
            {
                if (!EnsureInit(out string initError))
                {
                    return new QpResult(false, initError);
                }

                var surplus = new StringBuilder(64);
                if (NativeQp.QPBridge_UserPay(user, cardKey, surplus, surplus.Capacity) != 0)
                {
                    return new QpResult(true, Compose("充值成功", surplus.ToString()));
                }

                return new QpResult(false, ReadLastError("充值失败"));
            }
        });
    }

    public static QpResult ChangePassword(string user, string superPassword, string newPassword)
    {
        return Guard(() =>
        {
            lock (Gate)
            {
                if (!EnsureInit(out string initError))
                {
                    return new QpResult(false, initError);
                }

                if (NativeQp.QPBridge_UserSetPass(user, superPassword, newPassword) != 0)
                {
                    return new QpResult(true, "密码修改成功");
                }

                return new QpResult(false, ReadLastError("修改密码失败"));
            }
        });
    }

    public static void Logout()
    {
        lock (Gate)
        {
            try
            {
                NativeQp.QPBridge_LeaveLogin();
            }
            catch
            {
                // 忽略释放失败
            }
        }
    }

    public static string GetBulletin()
    {
        lock (Gate)
        {
            try
            {
                if (NativeQp.QPBridge_Init() == 0)
                {
                    return string.Empty;
                }

                var buffer = new StringBuilder(4096);
                NativeQp.QPBridge_GetBulletinW(buffer, buffer.Capacity);
                return buffer.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    private static bool EnsureInit(out string error)
    {
        error = string.Empty;
        if (NativeQp.QPBridge_Init() != 0)
        {
            return true;
        }

        error = ReadLastError("初始化失败");
        return false;
    }

    private static string Compose(string prefix, string detail)
    {
        return string.IsNullOrWhiteSpace(detail) ? prefix : $"{prefix}，剩余 {detail}";
    }

    private static string ReadLastError(string fallback)
    {
        var buffer = new StringBuilder(512);
        NativeQp.QPBridge_GetLastErrorW(buffer, buffer.Capacity);
        string message = buffer.ToString();
        return string.IsNullOrWhiteSpace(message) ? fallback : message;
    }

    private static QpResult Guard(Func<QpResult> action)
    {
        try
        {
            return action();
        }
        catch (DllNotFoundException ex)
        {
            return new QpResult(false, $"缺少依赖库：{ex.Message}");
        }
        catch (BadImageFormatException)
        {
            return new QpResult(false, "QPBridge.dll 需要 x64 运行环境");
        }
        catch (Exception ex)
        {
            return new QpResult(false, ex.Message);
        }
    }
}
