// QP card-key login bridge built on the QP SDK.
// VMProtect markers are placed on the exported licensing functions.
#include <windows.h>
#include "QP/QPVerify.h"
#include "QP/QPModX64.h"
#include "QP/VMProtectSDK/VMProtectSDK.h"

static bool g_inited = false;
static bool g_loggedIn = false;

// Hand the embedded encrypted QP module back to the SDK during init.
static BOOL LoadDllFromRes(PVOID* pDllData, DWORD* dwDllSize)
{
    *pDllData = QPMod;
    *dwDllSize = QPMod_size;
    return TRUE;
}

static void CopyAnsiToWide(const char* src, wchar_t* dst, int dstChars)
{
    if (dst == nullptr || dstChars <= 0) return;
    dst[0] = 0;
    if (src == nullptr) return;
    MultiByteToWideChar(CP_ACP, 0, src, -1, dst, dstChars);
}

// Reads remaining seconds/text from the current QP session.
static int ReadRemaining(wchar_t* expireText, int expireChars)
{
    int seconds = QPToInt(QPGetLoginData(3));
    char* day = SecToDay(static_cast<double>(seconds));
    if (day != nullptr)
    {
        CopyAnsiToWide(day, expireText, expireChars);
        QPFree(day);
    }
    return seconds;
}

static void DropPreviousSession()
{
    if (g_loggedIn)
    {
        QPLeaveLogin();
        g_loggedIn = false;
    }
}

extern "C" __declspec(dllexport) int QPBridge_Init(void)
{
    VMProtectBeginVirtualization("QPBridge_Init");

    int result = 0;
    if (g_inited)
    {
        result = 1;
    }
    else if (QPLibInit(LoadDllFromRes)
             && QPInit("xx", xx, "xx", "xx", xx))
    {
        g_inited = true;
        result = 1;
    }

    VMProtectEnd();
    return result;
}

// Returns 1 when the key is valid; writes remaining time text and seconds.
extern "C" __declspec(dllexport) int QPBridge_KeyLogin(
    const char* key, wchar_t* expireText, int expireChars, int* remainSeconds)
{
    VMProtectBeginVirtualization("QPBridge_KeyLogin");

    if (remainSeconds != nullptr) *remainSeconds = 0;
    if (expireText != nullptr && expireChars > 0) expireText[0] = 0;
    if (key == nullptr || key[0] == 0) return 0;

    if (!g_inited && QPBridge_Init() == 0) return 0;

    // Drop any previous session so a second attempt starts clean.
    DropPreviousSession();

    bool loggedIn = QPKeyLogin(const_cast<char*>(key)) != false;

    if (!loggedIn)
    {
        // Auto-unbind: the key may be bound to another machine, unbind then retry.
        int unbindSubTime = 0;
        int surplusUnbindNumber = 0;
        char* oldExpTime = nullptr;
        char* newExpTime = nullptr;
        char* oldSurplusTime = nullptr;
        char* newSurplusTime = nullptr;

        QPUnBind(const_cast<char*>(key), FALSE,
            &unbindSubTime, &surplusUnbindNumber,
            &oldExpTime, &newExpTime, &oldSurplusTime, &newSurplusTime);

        if (oldExpTime != nullptr) QPFree(oldExpTime);
        if (newExpTime != nullptr) QPFree(newExpTime);
        if (oldSurplusTime != nullptr) QPFree(oldSurplusTime);
        if (newSurplusTime != nullptr) QPFree(newSurplusTime);

        // Always retry: if unbind was unnecessary, the retry reveals the real login error
        // instead of the unbind status message.
        loggedIn = QPKeyLogin(const_cast<char*>(key)) != false;
    }

    if (!loggedIn) return 0;
    g_loggedIn = true;

    int seconds = ReadRemaining(expireText, expireChars);
    if (remainSeconds != nullptr) *remainSeconds = seconds;

    VMProtectEnd();
    return 1;
}

// Account login (user + password).
extern "C" __declspec(dllexport) int QPBridge_UserLogin(
    const char* user, const char* password, wchar_t* expireText, int expireChars, int* remainSeconds)
{
    VMProtectBeginVirtualization("QPBridge_UserLogin");

    if (remainSeconds != nullptr) *remainSeconds = 0;
    if (expireText != nullptr && expireChars > 0) expireText[0] = 0;
    if (user == nullptr || user[0] == 0 || password == nullptr) return 0;

    if (!g_inited && QPBridge_Init() == 0) return 0;
    DropPreviousSession();

    if (!QPUserLogin(user, password)) return 0;
    g_loggedIn = true;

    int seconds = ReadRemaining(expireText, expireChars);
    if (remainSeconds != nullptr) *remainSeconds = seconds;

    VMProtectEnd();
    return 1;
}

// Registration: user/password/super password/card key.
extern "C" __declspec(dllexport) int QPBridge_UserRegister(
    const char* user, const char* password, const char* superPassword, const char* cardKey,
    wchar_t* surplusText, int surplusChars)
{
    VMProtectBeginVirtualization("QPBridge_UserRegister");

    if (surplusText != nullptr && surplusChars > 0) surplusText[0] = 0;
    if (user == nullptr || user[0] == 0 || password == nullptr || cardKey == nullptr) return 0;

    if (!g_inited && QPBridge_Init() == 0) return 0;

    char* surplus = nullptr;
    bool ok = QPUserRegister(user, password, cardKey, "", superPassword, "", &surplus);
    if (surplus != nullptr)
    {
        CopyAnsiToWide(surplus, surplusText, surplusChars);
        QPFree(surplus);
    }

    VMProtectEnd();
    return ok ? 1 : 0;
}

// Recharge an account with a card key, then report the updated remaining time.
extern "C" __declspec(dllexport) int QPBridge_UserPay(
    const char* user, const char* cardKey, wchar_t* surplusText, int surplusChars)
{
    VMProtectBeginVirtualization("QPBridge_UserPay");

    if (surplusText != nullptr && surplusChars > 0) surplusText[0] = 0;
    if (user == nullptr || user[0] == 0 || cardKey == nullptr || cardKey[0] == 0) return 0;

    if (!g_inited && QPBridge_Init() == 0) return 0;
    if (!QPUserPay(user, cardKey)) return 0;

    int statusCode = 0;
    char* expTime = nullptr;
    char* surplusTime = nullptr;
    if (QPUserQueryInfo(user, &statusCode, &expTime, &surplusTime))
    {
        char* text = surplusTime != nullptr ? surplusTime : expTime;
        if (text != nullptr)
        {
            CopyAnsiToWide(text, surplusText, surplusChars);
        }
    }
    if (expTime != nullptr) QPFree(expTime);
    if (surplusTime != nullptr) QPFree(surplusTime);

    VMProtectEnd();
    return 1;
}

// Change account password with the super password.
extern "C" __declspec(dllexport) int QPBridge_UserSetPass(
    const char* user, const char* superPassword, const char* newPassword)
{
    VMProtectBeginVirtualization("QPBridge_UserSetPass");

    if (user == nullptr || user[0] == 0 || newPassword == nullptr || newPassword[0] == 0) return 0;

    if (!g_inited && QPBridge_Init() == 0) return 0;

    int result = QPUserSetPass(user, superPassword, newPassword) ? 1 : 0;

    VMProtectEnd();
    return result;
}

extern "C" __declspec(dllexport) void QPBridge_GetLastErrorW(wchar_t* buffer, int chars)
{
    VMProtectBeginVirtualization("QPBridge_GetLastErrorW");

    if (buffer == nullptr || chars <= 0) return;
    buffer[0] = 0;

    char* err = QPGetLastErrorString();
    if (err != nullptr)
    {
        CopyAnsiToWide(err, buffer, chars);
        QPFree(err);
    }

    VMProtectEnd();
}

extern "C" __declspec(dllexport) void QPBridge_GetBulletinW(wchar_t* buffer, int chars)
{
    VMProtectBeginVirtualization("QPBridge_GetBulletinW");

    if (buffer == nullptr || chars <= 0) return;
    buffer[0] = 0;

    char* bulletin = nullptr;
    if (QPGetBulletin(&bulletin) && bulletin != nullptr)
    {
        CopyAnsiToWide(bulletin, buffer, chars);
        QPFree(bulletin);
    }

    VMProtectEnd();
}

extern "C" __declspec(dllexport) void QPBridge_LeaveLogin(void)
{
    VMProtectBeginVirtualization("QPBridge_LeaveLogin");

    QPLeaveLogin();
    g_loggedIn = false;

    VMProtectEnd();
}
