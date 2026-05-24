//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor
{
    /// <summary>
    /// 打开文件夹相关的实用函数。
    /// </summary>
    public static class OpenFolder
    {
        /// <summary>
        /// 打开 Data Path 文件夹。
        /// </summary>
        [MenuItem("AZWorkingCat/打开文件夹/数据路径 Data Path", false, 10)]
        [MenuItem("Game Framework/Open Folder/Data Path", false, 10)]
        public static void OpenFolderDataPath()
        {
            Execute(Application.dataPath);
        }

        /// <summary>
        /// 打开 Persistent Data Path 文件夹。
        /// </summary>
        [MenuItem("AZWorkingCat/打开文件夹/持久数据路径 Persistent Data Path", false, 11)]
        [MenuItem("Game Framework/Open Folder/Persistent Data Path", false, 11)]
        public static void OpenFolderPersistentDataPath()
        {
            Execute(Application.persistentDataPath);
        }

        /// <summary>
        /// 打开 Streaming Assets Path 文件夹。
        /// </summary>
        [MenuItem("AZWorkingCat/打开文件夹/流媒体资源路径 Streaming Assets Path", false, 12)]
        [MenuItem("Game Framework/Open Folder/Streaming Assets Path", false, 12)]
        public static void OpenFolderStreamingAssetsPath()
        {
            Execute(Application.streamingAssetsPath);
        }

        /// <summary>
        /// 打开 Temporary Cache Path 文件夹。
        /// </summary>
        [MenuItem("AZWorkingCat/打开文件夹/临时缓存路径 Temporary Cache Path", false, 13)]
        [MenuItem("Game Framework/Open Folder/Temporary Cache Path", false, 13)]
        public static void OpenFolderTemporaryCachePath()
        {
            Execute(Application.temporaryCachePath);
        }

#if UNITY_2018_3_OR_NEWER

        /// <summary>
        /// 打开 Console Log Path 文件夹。
        /// </summary>
        [MenuItem("AZWorkingCat/打开文件夹/控制台日志路径 Console Log Path", false, 14)]
        [MenuItem("Game Framework/Open Folder/Console Log Path", false, 14)]
        public static void OpenFolderConsoleLogPath()
        {
            Execute(System.IO.Path.GetDirectoryName(Application.consoleLogPath));
        }

#endif

        /// <summary>
        /// 打开指定路径的文件夹。
        /// </summary>
        /// <param name="folder">要打开的文件夹的路径。</param>
        public static void Execute(string folder)
        {
            folder = Utility.Text.Format("\"{0}\"", folder);
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Process.Start("Explorer.exe", folder.Replace('/', '\\'));
                    break;

                case RuntimePlatform.OSXEditor:
                    Process.Start("open", folder);
                    break;

                default:
                    throw new GameFrameworkException(Utility.Text.Format("Not support open folder on '{0}' platform.", Application.platform));
            }
        }
    }
}
