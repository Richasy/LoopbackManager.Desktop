# Sprout 升级与全项目审查

当前依赖已更新为 Azure 完整发布 `0.1.0-rodel.105.1.g5af5aae144af`，本地安装版为 `2.2609.6.0`。
以下主体保留首次升级至 Azure 104 的审查与验证记录；105 的追加验证和部署见文末。

审查日期：2026-10-29。工作基线 `d0e8df74ef6e09c6dd0988ff4917214d67da1a59`，已经包含当时的
`origin/main`（`6fe38cb`）。所有修改位于独立 `codex/loopback-sprout-review` worktree；未修改 Sprout
源码或用户主 checkout，未推送、提交 PR、安装 MSIX 或修改 Windows 回环配置。

## 版本与来源

| 项目 | 原版本 | 当前版本 |
| --- | --- | --- |
| Sprout NuGet | `0.1.0-local.g3052b620dd96` | `0.1.0-rodel.104.1.g10d44ad338b7` |
| Sprout 源码提交 | `3052b620dd96b19e3931bacb72aef4e330fb2907` | `10d44ad338b76c29a465a6229bfa0cb1215b98d4` |
| Windows App SDK Foundation | `2.1.0` | `2.3.12` |
| InteractiveExperiences | 传递依赖 | 显式 `2.1.9`，满足 Foundation 的依赖要求 |

以已完成的 `Sprout.ReleaseManifest` 同版本标记及其 `sprout-package-set.json` 为发布集依据，而非仅凭某个
包的最新搜索结果。manifest 的 schema 为 1，包含 34 个包，`repositoryCommit` 对应上述新提交。
本次在本机缓存逐项核对了 14 个已下载包的长度与 SHA-256，全部匹配 manifest；没有宣称核验未下载的其余包。
保留四个实际使用的直接 Sprout 包（核心、D2D、FluentIcon、Packaging），共解析到八个同版本 Sprout 包。

`nuget.config` 将本地相邻目录 feed 替换为
[Azure sprout 源](https://pkgs.dev.azure.com/zrichasy/_packaging/sprout/nuget/v3/index.json)，并用
package source mapping 将 `Sprout` / `Sprout.*` 限定到该源。凭据仍由用户级配置提供，仓库中没有密钥，
没有使用源码 ProjectReference 或本地 feed 兜底。`SproutVersion` 统一直接包版本。

Foundation 由 `SettingsToolkit` 的 `Microsoft.Windows.Storage.ApplicationData` 实际使用，继续保留
自包含运行时。删除没有调用者的 `Sprout.WindowsAppSDK` 窗口/选择器桥接包。显式指定已发布的
InteractiveExperiences 2.1.9，避免 Foundation 对不可用 2.1.8 最低依赖产生 NU1603。
解析资产和发布目录均未出现 `Microsoft.WindowsAppSDK.WinUI` 或 `Microsoft.UI.Xaml`。

`global.json` 允许稳定 .NET 10 feature band 前滚，避免本机默认选择 .NET 11 预览 SDK；本次使用
SDK 10.0.401。项目的 `LangVersion=preview` 与匹配版本 Sprout 模板一致。

## 已审查的框架历史与采用方式

阅读了上述旧、新提交间的历史，以及新提交的 DSL、custom-controls、items-repeater、store、theme、window、
ui-thread-switch 指南和 DSL/formatting skill。重点变化如下：

| 提交 | 相关能力 | 本项目处理 |
| --- | --- | --- |
| `fc7881568`（#973）、`4c651f098`（#974） | typed DSL、键控状态、生成本地化后的 modifier 解析 | 将旧 Stack/散列参数改为 `Column` / `Row` / `UiChildren`，保留布局与文案 |
| `0f9127be7`（#1004）、`7b31fd064`、`4a20a8878`（#1009） | 自定义控件工厂、统一 `Ui Body`、保留实例输入更新 | 根布局通过 `AppHeader()` / `AppList()` / `AppFooter()` 声明控件，不在重建中配置新实例 |
| `c31618fb1`（#1012） | Body 根布局默认值 | 保留当前自然布局；`Cell` 继续只放在实际父 Grid 使用处 |
| `38d12efd6`（#955） | UI SynchronizationContext 与 UI 线程访问检查 | 保留 UI 发起的 await 和注入的 `DispatcherQueueScheduler`；本机同步调用继续由服务放在线程池 |
| `996ee2ea5`（#957）、`7277e163b`（#985） | 虚拟列表末端与 compositor 范围修正 | 继续使用框架虚拟化，不添加滚动补偿；改用 SID 自然键并实现真实行重绑定 |
| `0011d1e1a`（#953）、`9d9db5d40`（#1039）、`32b535b7e`（#1057） | 隐藏、边缘对齐窗口恢复 | 保留 `WindowPlacement` API 和既有设置 key/container，直接受益于框架修正 |

主题采用最新版指南推荐的 `Brush.Theme(...)` 与默认/scheme palette，替代每个 Body 中的 literal
`FromTheme(Theme.Resolve().Colors)` 快照。按钮、复选框和说明文字在绘制时解析主题；原生窗口背景仍通过
`ActualThemeChanged` 同步。TextBox 同时声明 filter 值和回写方法；保存确认状态改为 Control 的 partial 状态属性。

## 项目修正与保留的行为

- 用 `[Computed]` 缓存过滤与排序结果，搜索同时匹配显示名和包名，仍按已保存豁免优先、显示名排序。
  待保存勾选不改变顺序；保存改变基线后仍按原规则排序。
- 虚拟行原先的 configure 回调为空。现在用 SID 保持身份，`SetItem` 替换当前 store 并请求 Body reconcile，
  刷新同 SID 或回收行后，文字、勾选、可访问性和操作目标都跟随新数据。
- `SaveAsync` 在没有成功清单、清单加载中或另一保存中拒绝写入；失败刷新保留的旧值不再使保存按钮可用。
  继续保留隐藏豁免 SID、全量去重、保存时刻快照、写后回读校验和保存期间的新编辑。
- FirewallAPI 成功返回非空 buffer / 零个条目时，原实现提前返回而没有释放 buffer；现在走统一 finally。
  非零条目但空 buffer 在边界报错，避免把损坏响应当作完整空清单。
- 设置 helper 收敛到实际使用的字符串，不再包含没有调用者的 enum/generic 分支；读取只访问一次容器，
  类型不符时使用默认值。原 `v1` 容器和 `WindowPlacement` key 保持兼容。
- 清理模板说明（包括不存在的 Ctrl+T 功能）并补充真实开发、发布入口。依据 `.editorconfig` 将两份原无
  BOM 的 Data 文件标准化为 UTF-8 BOM；这是全项目格式门禁产生的两行编码变更。
- 测试程序集不是 MSIX 应用，关闭其 `AppxGeneratePriEnabled`，不再把 MSTest 卫星资源当作应用 PRI 索引。
  该设置只作用于测试项目，产品资源、三种语言和 MSIX PRI 继续正常生成。

未采用 `ItemsView` 的选择/拖拽、Forms、Query、导航、动画或新主题选择器：当前产品是每行独立豁免勾选与
打开目录，增加这些能力没有业务收益。保留始终挂载的 ScrollView 和状态覆盖层，避免首次加载后再引入原生
滚动输入宿主。未按函数长度机械拆分 DSL，也没有新建设置框架、通用服务层或改应用身份。

## 行为与构建证据

本次日志位于 worktree 的 `artifacts/validation/`、`artifacts/test-results/` 和 `artifacts/ui-smoke-final/`；
这些机器生成产物不纳入 Git。基线为 16/16 MSTest。先新增未加载保存和重复过滤快照测试，确认 2 项按预期失败；
再修正实现。失败刷新可保存的回归也先观察红灯，再修正 `CanSave`。

| 验证 | 结果 |
| --- | --- |
| Sprout 官方 tree formatter（对应 `10d44ad3` 源码独立构建）`--check src` | 退出 0，全部文件已符合树格式 |
| `dotnet format LoopbackManager.sln --no-restore --verify-no-changes` | 退出 0，无格式漂移 |
| `dotnet build LoopbackManager.sln -c Release` | 成功，0 警告、0 错误 |
| `dotnet test LoopbackManager.sln -c Debug --no-restore` | 22/22，通过 |
| `dotnet test LoopbackManager.sln -c Release --no-restore` | 22/22，通过，无原 PRI 资源警告 |
| `dotnet publish src/LoopbackManager.Shell -c Release -r win-x64` | NativeAOT 编译、MSIX 打包与签名成功 |
| `dotnet publish src/LoopbackManager.Shell -c Release -r win-arm64` | 交叉 NativeAOT 编译、ARM64 MSIX 打包与签名成功 |
| `tests/ui/read-only-smoke.json`，DevTools preview0026，Debug unpackaged | 16 步 UIA 全通过，截图成功，hash 绑定的进程清理证明通过 |

新增测试覆盖：没有可信清单时禁止保存；失败刷新禁止保存旧清单；过滤快照复用、大小写搜索与待保存勾选
顺序；过期加载不能覆盖最新结果；加载期间拒绝保存；真实 mounted 行更换 store 后，保留 render/interaction/
semantic 身份，并通过同一可访问性 Toggle 动作确认新回调目标，旧 store 改动不再重绘该行。

UI smoke 只搜索、清空搜索和刷新，断言保存始终禁用；没有调用 Windows 写接口。检查了真实窗口截图，
保留原产品布局、图标与暗色主题。打包身份仍为 `Richasy.LoopbackManager`、版本 `2.2609.5.0`、publisher
`CN=Richasy`；没有执行安装或替换现有包。

## 限制与后续复审

- 真机 UI 证据来自 Windows 11 26200 x64 Debug；没有宣称在 Windows 10、所有 DPI、主题、辅助技术或
  Release 已安装包上逐项完成验证。没有改变用户系统主题来取得额外截图。
- 保存成功/失败、并发与 SID 安全行为由 fake 服务测试；未更改用户系统的实际回环配置。
- 全量替换配置没有跨进程 compare-and-swap；应用内的单实例和操作互斥无法阻止其他工具同时修改系统配置，
  这类外部并发仍是现有接口的限制。
- 本机缓冲区的异常组合没有通过重建 Win32 API 的测试替身模拟；修正基于现有分配/释放契约和代码路径审查。
- 官方 crapper/dryer/mutator 均未列出 C# 支持，未运行或报告其指标。采用其背后的单点决策、去重复、
  边界校验、单写者与行为测试原则；外部 skill 的 push 指令不改变用户要求的 worktree 交付边界。
- 未增加自动发布、推送或安装流程。评审与后续合并由用户决定。

主要操作顺序：确认隔离基线与仓库约定 → 跑旧包测试 → 查完整 Azure manifest/包校验与框架历史 →
先固定回归 → 迁移依赖和 DSL → 修复状态/重绑定/释放边界 → 运行全项目格式与行为测试 → NativeAOT/MSIX
构建 → 只读真实窗口验证 → 形成本文和简短风格准则。

## Azure 105 追加更新

2026-10-29，更新到 `0.1.0-rodel.105.1.g5af5aae144af`，对应提交
`5af5aae144af60d05866635bfc8f2f61e717331b`（#1066）。这次正式发布包含之前在 Launcher 分支上验证的
静态 UI 失效、root slice 损伤跟踪、指针观察、语言表延迟创建和根 raster 退休改进。

同版本 ReleaseManifest 含 34 个包，已下载的 14 个包长度与 SHA-256 均匹配。
应用解析的八个 Sprout 包全部来自 Azure 源且版本一致；没有采用 Launcher 本地开发 feed。
为满足同身份 MSIX 严格递增的原位更新要求，项目和清单版本同步升为 `2.2609.6.0`。
继续使用默认 Direct2D 缓存策略，没有启用可选的 64 MiB 阈值。

验证：Release 22/22 测试、格式验证和 x64 NativeAOT/MSIX 打包签名通过；
第一方签名原位更新与安装路径核对通过，随后启动已安装的新版本并完成 WGC 捕获。
没有卸载旧包或删除应用数据。证据集中在 `artifacts/azure105/`：
`test.log`、`format.log`、`publish.log`、`resolved-packages.json`、`package-integrity.json`、
`deploy/result.json` 和 `launch/result.json`。

此前的滚动数值仍是 Azure 104 与 Launcher 开发包的隔离对照，本次没有将它们改称 Azure 105 的实测结果。
