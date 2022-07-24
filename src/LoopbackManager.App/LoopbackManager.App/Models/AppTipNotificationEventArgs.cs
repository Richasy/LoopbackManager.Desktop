﻿// Copyright (c) Richasy. All rights reserved.

using System;
using LoopbackManager.App.Enums;

namespace LoopbackManager.App.Models
{
    /// <summary>
    /// 应用提示通知事件参数.
    /// </summary>
    internal sealed class AppTipNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppTipNotificationEventArgs"/> class.
        /// </summary>
        /// <param name="msg">消息内容.</param>
        /// <param name="type">消息类型.</param>
        public AppTipNotificationEventArgs(string msg, InfoType type = InfoType.Information)
        {
            Message = msg;
            Type = type;
        }

        /// <summary>
        /// 消息内容.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 消息类型.
        /// </summary>
        public InfoType Type { get; set; }
    }
}
