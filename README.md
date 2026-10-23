# DogeHyperSecurity 登录框架说明

## 1. 项目概览

- 技术栈：WPF / .NET 9 (net9.0-windows) + WPF-UI 4.0.3 + C++ 原生桥接 DLL
- 模式：WPF 登录壳负责授权验证，登录成功后释放并启动独立外挂进程，随后自身退出
- 发布形式：单文件框架依赖版（目标机需要 .NET 9 Desktop Runtime）

## 2. 目录结构

| 路径 | 作用 |
|---|---|
| `App.xaml` / `App.xaml.cs` | 程序入口；检查 `VMProtectSDK64.dll`、启动时清理旧 overlay 目录 |
| `MainWindow.xaml` / `.cs` | 固定大小主窗口、标题栏、左侧导航、内容页、弹窗层 |
| `Views/` | `WelcomeView`（公告+账号/卡密登录）、`RegisterView`、`RechargeView`、`ChangePasswordView` |
| `Interop/QPBridge.cs` | QP SDK 的 C# P/Invoke 封装（`QpAuth`） |
| `Services/LoginSettings.cs` | 记住账号/卡密、自动登录配置 |
| `Services/GameLauncher.cs` | 登录成功后释放内嵌外挂、启动进程、启动时清理旧目录 |
| `Native/QPBridge/` | C++ 桥接层源码、VS 工程、QP SDK、已编译 `bin/Release/QPBridge.dll` |
| `Payload/` | 外挂本体：`uu.exe` + `VMProtectSDK64.dll`，以嵌入资源方式打包 |
| `Properties/PublishProfiles/win-x64-lite.pubxml` | 精简版单文件发布配置 |
| `publish/` | 最终成品：`DogeHyperSecurity.exe`（当前 QPBridge.dll 已加壳，无需外置 VMProtectSDK64.dll） |
| `运行库/` | .NET 9 Desktop Runtime 安装包（给目标电脑用） |
| `生成精简版.bat` | 一键发布脚本 |

## 3. 运行流程

1. 双击 `DogeHyperSecurity.exe`
2. .NET 单文件宿主在启动时解压程序集到 `%TEMP%\.net\DogeHyperSecurity\<哈希>\`
3. `App.OnStartup`：
   - 若 exe 同目录存在 `VMProtectSDK64.dll`，先复制到解压目录；不存在则删除解压目录里上次残留的副本
   - 尝试加载 `QPBridge.dll`：
     - 加壳后的 QPBridge.dll 不依赖 `VMProtectSDK64.dll`，可直接加载
     - 未加壳且缺少 `VMProtectSDK64.dll` 时加载失败，弹出系统错误框并退出
   - 清理 `%TEMP%\DogeHyperSecurity\overlay` 下的旧目录
4. 显示登录界面，并异步加载 QP 公告
5. 卡密登录成功：
   - 释放 WPF 自身 QP 会话
   - 从内嵌资源释放 `Payload` 到 `%TEMP%\DogeHyperSecurity\overlay\<GUID>\`
   - 以隐藏输入重定向方式启动其中任意 `.exe`，卡密写入其 stdin
   - WPF 调用 `Shutdown()` 退出，外挂独立运行
6. 卡密登录失败：
   - 自动调用一次 `QPUnBind`，随后无条件重试一次 `QPKeyLogin`
   - 若仍失败，界面显示真实错误；绑定类错误会追加「，请再次登录」

## 4. 常见修改速查

### 修改界面
- 主窗口/导航/弹窗：`MainWindow.xaml`
- 登录页/公告：`Views/WelcomeView.xaml`
- 注册/充值/改密：`Views/RegisterView.xaml`、`RechargeView.xaml`、`ChangePasswordView.xaml`

### 登录方式开关
- 左侧导航底部：`MainWindow.xaml` 中的 `AccountLoginSwitch`、`CardLoginSwitch`
- 默认：账号登录关、卡密登录开
- 两个开关互斥，始终至少开启一个
- 显示逻辑：`MainWindow.ApplyLoginModeAvailability()` → `WelcomeView.SetLoginModeAvailability()`
  - 卡密模式：登录卡片只显示卡密页签，左侧导航只保留「欢迎」
  - 账号模式：登录卡片只显示账号页签，左侧显示全部导航（欢迎/注册/充值/改密）
- 登录成功后保存当前模式到 `LoginSettings.LoginMode`，下次启动自动恢复开关状态

### 修改登录逻辑
- 卡密/账号/注册/充值/改密调用：`Interop/QPBridge.cs`
- 登录成功后的状态文字、自动登录：`Views/WelcomeView.xaml.cs`
- 记住/自动登录存储：`Services/LoginSettings.cs`
  - 数据文件：`%LOCALAPPDATA%\DogeHyperSecurity\login.json`（本机明文）

### 修改外挂释放/启动
- 释放与启动：`Services/GameLauncher.cs`
- 启动参数：卡密通过 stdin 写入，当前外挂代码里的 `cin >> key`
- 控制台显示：`CreateNoWindow = false`（可见）；改为 `true` 可隐藏
- 启动后退出：`Views/WelcomeView.xaml.cs` 中的 `Application.Current.Shutdown()`

### 更换外挂程序
1. 把新的 `.exe` 及其依赖 DLL 放进 `Payload/`
2. 不需要改代码：程序会释放并启动 Payload 中任意 `.exe`
3. 运行 `生成精简版.bat` 重新打包

### 修改 QP 服务器参数
位置：`Native/QPBridge/QPBridge.cpp` 的 `QPBridge_Init()`

```cpp
QPInit("***.***.***.***", *****, "**", "*", ********);   // 服务器地址/端口/软件名/版本/通信密钥均用星号代替，实际值见源码
```

修改后必须重新编译 `QPBridge.dll`：

1. 双击 `Native/QPBridge/编译.bat` 一键编译（Release x64）
   或命令行：
   ```
   msbuild Native\QPBridge\QPBridge.vcxproj /p:Configuration=Release /p:Platform=x64
   ```
2. 输出：`Native/QPBridge/bin/Release/QPBridge.dll`
3. 如需 VMProtect 加壳：只对 `QPBridge.dll` 加壳，加壳后覆盖同名文件
4. 运行 `生成精简版.bat` 重新发布

### 重新发布
```
生成精简版.bat
```
等价命令：
```
dotnet publish DogeHyperSecurity.csproj -p:PublishProfile=win-x64-lite
```

## 5. 重要注意事项

1. **不要用 VMProtect 给主程序 `DogeHyperSecurity.exe` 加壳**
   - 单文件 .NET 程序加壳后会崩溃（`0xc0000409` / BEX64），只对 `QPBridge.dll` 加壳。
2. **分发文件**
   - `DogeHyperSecurity.exe` 必须分发。
   - `VMProtectSDK64.dll`：仅当 `QPBridge.dll` **未加壳** 时需要一起分发。
3. **QPBridge.dll 加壳与不加壳的区别**
   - 加壳后：不再依赖 `VMProtectSDK64.dll`，exe 单独放桌面也能正常打开。
   - 未加壳：必须把 `VMProtectSDK64.dll` 放在 exe 同目录，否则启动时会弹出系统错误框。
   - 启动检测逻辑在 `App.xaml.cs` 的 `EnsureBridgeLoadable()`，通过实际加载 `QPBridge.dll` 判断，不写死加壳状态。
4. **管理员权限**
   - `app.manifest` 为 `requireAdministrator`，外挂加载驱动必须提权。
5. **QP 会话是进程内的**
   - WPF 验证成功后会先 `QPLeaveLogin`，再由外挂进程用卡密重新登录，避免双会话冲突。
6. **批量修改 `.bat` 时注意编码**
   - `生成精简版.bat` 必须是 ASCII + CRLF，否则 cmd 解析会出现乱码/命令错位。
7. **C++ 工程的 SDK**
   - `Native/QPBridge/QP` 已包含 `QPVerify.h`、`QPModX64.h`、`QPLibX64_MT*.lib`、`VMProtectSDK`。
   - 不要给 C++ 工程加 `/utf-8`，`QPVerify.h` 是 GBK 编码。
8. **临时目录**
   - 单文件程序集：`%TEMP%\.net\DogeHyperSecurity\<哈希>\`
   - 外挂释放目录：`%TEMP%\DogeHyperSecurity\overlay\<GUID>\`（每次启动清理旧目录）

## 6. 常见问题

| 现象 | 处理 |
|---|---|
| 提示「由于找不到 VMProtectSDK64.dll」 | 说明当前 `QPBridge.dll` 未加壳，把该 DLL 放到 exe 同目录，或改用加壳后的 `QPBridge.dll` |
| 提示「卡密已经是解绑状态，无需解绑！」 | 当前逻辑会继续重试登录，最终显示真实错误；若仍异常，检查 QP 服务器端绑定状态 |
| 提示「请在绑定的机器上登录，请再次登录」 | 已执行自动解绑，手动再点一次登录 |
| 登录成功但外挂没出现 | 检查 `Payload` 中的 exe 及其依赖是否完整，尤其是 `VMProtectSDK64.dll` |
| 外挂一闪而过 | 用可见控制台方式启动查看输出；确认外挂自身能独立运行 |
| 发布后无法启动 | 目标机需要 .NET 9 Desktop Runtime（见 `运行库/`） |
