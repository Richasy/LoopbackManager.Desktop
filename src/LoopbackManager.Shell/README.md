# LoopbackManager.Shell

Sprout 绘制的网络回环管理器，使用 Direct2D/DirectComposition，不依赖 WinUI。
Windows App SDK Foundation 仅提供 `Microsoft.Windows.Storage.ApplicationData` 设置存储。

## 开发

安装 .NET 10 SDK，并在用户级 NuGet 配置中为 `sprout` Azure 源配置读取凭据；不要把凭据写入仓库。
仓库的 `global.json` 选择已安装的稳定 .NET 10 SDK。

```powershell
dotnet run --project src/LoopbackManager.Shell -p:SproutRunPackaged=false
dotnet test LoopbackManager.sln
```

Debug 使用原有 `Richasy/LoopbackManager` 的 `v1` 非打包设置容器。Release 使用包身份的设置容器；
发布版应从 MSIX 启动。窗口跟随系统主题并记住位置。

## 发布

```powershell
dotnet publish src/LoopbackManager.Shell -c Release -r win-x64
```

需要 Windows NativeAOT C++ 工具链和 Windows SDK。发布目录包含本机可执行文件、所需非 UI
Windows App SDK 文件、签名 MSIX 和证书。打包不执行安装，也不改变网络回环设置。
保留 `Package.appxmanifest` 中的应用身份；签名凭据由本机环境提供。

## 代码入口

- `Program.cs`：Direct2D、单实例和共享 store 注册。
- `App.cs`：窗口、位置持久化和首次加载。
- `AppRoot.cs` / `Controls/`：声明式界面、虚拟化应用列表与状态提示。
- `Data/AppStore.cs`：加载、过滤、保存快照与并发规则。
- `Data/LoopbackService.cs`：FirewallAPI / WinRT 边界、隐藏 SID 保留与写后校验。

详见仓库的 [代码风格](../../docs/code-style.md) 和 [Sprout 迁移审查](../../docs/sprout-upgrade-review.md)。
