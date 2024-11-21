// Copyright (c) Richasy. All rights reserved.

using LoopbackManager.UI.ViewModels;
using Richasy.WinUI.Share.Base;

namespace LoopbackManager.UI;

/// <summary>
/// 程序条目控件.
/// </summary>
public sealed partial class ProgramItemControl : ProgramItemControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramItemControl"/> class.
    /// </summary>
    public ProgramItemControl() => InitializeComponent();
}

/// <summary>
/// 程序条目控件基类.
/// </summary>
public abstract class ProgramItemControlBase : LayoutUserControlBase<ProgramItemViewModel>
{
}
