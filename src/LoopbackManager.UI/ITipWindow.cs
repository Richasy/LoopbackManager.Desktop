// Copyright (c) Richasy. All rights reserved.

using System.Threading.Tasks;
using Richasy.WinUI.Share.Base;

namespace LoopbackManager.UI;

/// <summary>
/// 提示窗口.
/// </summary>
public interface ITipWindow
{
    /// <summary>
    /// 显示提示.
    /// </summary>
    /// <param name="text">提示文本.</param>
    /// <param name="type">类型.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task ShowTipAsync(string text, InfoType type = InfoType.Error);
}
