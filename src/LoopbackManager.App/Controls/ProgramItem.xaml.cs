// Copyright (c) Richasy. All rights reserved.

using LoopbackManager.App.ViewModels;
using ReactiveUI;

namespace LoopbackManager.App.Controls
{
    /// <summary>
    /// 程序条目.
    /// </summary>
    internal sealed partial class ProgramItem : ProgramItemBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramItem"/> class.
        /// </summary>
        public ProgramItem() => InitializeComponent();
    }

    /// <summary>
    /// <see cref="ProgramItem"/> 的基类.
    /// </summary>
    internal class ProgramItemBase : ReactiveUserControl<ProgramItemViewModel>
    {
    }
}
