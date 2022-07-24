// Copyright (c) Richasy. All rights reserved.

using System;
using LoopbackManager.App.Enums;
using Windows.Storage;

namespace LoopbackManager.App.Toolkits
{
    /// <summary>
    /// 本地设置工具.
    /// </summary>
    internal static class SettingsToolkit
    {
        /// <summary>
        /// 读取本地设置.
        /// </summary>
        /// <typeparam name="T">设置的数据类型.</typeparam>
        /// <param name="settingName">设置名.</param>
        /// <param name="defaultValue">默认值.</param>
        /// <returns>设置值.</returns>
        internal static T ReadLocalSetting<T>(SettingNames settingName, T defaultValue)
            => ReadLocalSetting(settingName.ToString(), defaultValue);

        /// <summary>
        /// 写入本地设置.
        /// </summary>
        /// <typeparam name="T">设置的数据类型.</typeparam>
        /// <param name="settingName">设置名.</param>
        /// <param name="value">设置值.</param>
        internal static void WriteLocalSetting<T>(SettingNames settingName, T value)
            => WriteLocalSetting(settingName.ToString(), value);

        /// <summary>
        /// 删除本地设置.
        /// </summary>
        /// <param name="settingName">设置名.</param>
        internal static void DeleteLocalSetting(SettingNames settingName)
            => DeleteLocalSetting(settingName.ToString());

        /// <summary>
        /// 设置名称是否存在.
        /// </summary>
        /// <param name="settingName">设置名.</param>
        /// <returns>是否存在.</returns>
        internal static bool IsSettingKeyExist(SettingNames settingName)
            => IsSettingKeyExist(settingName.ToString());

        internal static void WriteLocalSetting<T>(string settingName, T value)
        {
            var settingContainer = ApplicationData.Current.LocalSettings;

            if (value is Enum)
            {
                settingContainer.Values[settingName] = value.ToString();
            }
            else
            {
                settingContainer.Values[settingName] = value;
            }
        }

        internal static T ReadLocalSetting<T>(string settingName, T defaultValue)
        {
            var settingContainer = ApplicationData.Current.LocalSettings;
            if (IsSettingKeyExist(settingName))
            {
                if (defaultValue is Enum)
                {
                    var tempValue = settingContainer.Values[settingName].ToString();
                    Enum.TryParse(typeof(T), tempValue, out var result);
                    return (T)result;
                }
                else
                {
                    return (T)settingContainer.Values[settingName];
                }
            }
            else
            {
                WriteLocalSetting(settingName, defaultValue);
                return defaultValue;
            }
        }

        internal static void DeleteLocalSetting(string settingName)
        {
            var settingContainer = ApplicationData.Current.LocalSettings;

            if (IsSettingKeyExist(settingName))
            {
                settingContainer.Values.Remove(settingName);
            }
        }

        internal static bool IsSettingKeyExist(string settingName)
            => ApplicationData.Current.LocalSettings.Values.ContainsKey(settingName.ToString());
    }
}
