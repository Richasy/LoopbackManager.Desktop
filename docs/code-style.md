# 代码风格与边界

遵循现有 `.editorconfig`，并借鉴 [ub-mode](https://github.com/unclebob/ub-mode/blob/57a8bdd28e45ed41900ace0bafa2ff76dd9ed70f/SKILL.md)
的工程原则：优先解决真实行为与根因，再整理结构。清晰且局部的代码保持原样。

- 一个业务决策只放一处。`AppStore` 管理加载、保存与快照；`AppItemStore` 管理待保存值和基线；
  `LoopbackService` 负责 Windows 边界。不要把本机互操作放入 UI Body，也不要为简单规则制造框架。
- 先明确数据形状。SID 是应用身份；Windows 保存接口会替换完整集合，不能丢弃没有可见行的已豁免 SID。
  未取得成功清单前不能保存。保存期间的新编辑只改变待保存值，不能提前成为已保存基线。
- 在外部数据进入时校验一次。Windows 指针、错误码和设置类型在边界检查；内部使用已验证模型。
  分配与释放成对，并保证空结果、取消与失败路径都释放本机缓冲区。
- Store 和 Control 使用 Sprout `partial` 状态属性。轻量派生值用普通属性；过滤、排序等重复昂贵计算
  才使用 `[Computed]`。共享可变状态维持一个写入入口，异步操作选择明确的 latest-wins / droppable 策略。
- UI 保持一个可读的 `Ui Body`。容器使用 `Column`、`Row`、`Grid` 和集合表达式；编辑器同时提供值和回写。
  自定义控件用生成的声明式工厂；虚拟列表保留稳定 SID key，并在复用时更新数据和回调目标。
  不把任意子树拆成返回 `Ui` 的运行时 helper。
- 普通颜色使用主题 token 和默认/scheme palette，跟随绘制位置的主题。只有真正需要模式分支时才解析主题。
  应用窗口背景属于原生窗口 API，继续在主题事件中同步。
- 注释解释意图、风险和边界。避免模板式逐行说明；公共 API 的 XML 文档保持准确。
- 测试从业务入口、可访问性动作与用户可见结果断言行为。先固定回归，再改变结构；不通过实际 Windows
  网络写入验证保存逻辑，使用 `ILoopbackService` fake。真实窗口 smoke 只执行搜索和刷新。

从匹配 Sprout 版本的 [tools/Sprout.Format](https://github.com/Richasy/Sprout/tree/10d44ad338b76c29a465a6229bfa0cb1215b98d4/tools/Sprout.Format)
源码构建 `sprout-format`（框架 NuGet 包不会自动安装该命令），先运行 `sprout-format src`，再运行
`dotnet format LoopbackManager.sln`。
提交前用对应 `--check` / `--verify-no-changes`、`dotnet test` 和 Release NativeAOT publish 验证。
C# 文件使用 `.editorconfig` 规定的四空格、CRLF 和 UTF-8 BOM；文档中的命令从仓库根目录执行。

官方 [crapper](https://github.com/unclebob/crapper)、[dryer](https://github.com/unclebob/dryer) 和
[mutator](https://github.com/unclebob/mutator) 的支持列表未包含 C#，因此不宣称已运行它们或达到某个 CRAP/
重复度/变异分数。本项目使用 .NET、Sprout 编译诊断和实际行为证据。
