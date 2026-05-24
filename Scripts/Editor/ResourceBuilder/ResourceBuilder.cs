//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------
// AzCat Mod: 中文支持 + EditorPrefs 持久化

using GameFramework;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源生成器。
    /// </summary>
    internal sealed class ResourceBuilder : EditorWindow
    {
        private ResourceBuilderController m_Controller = null;
        private bool m_OrderBuildResources = false;
        private int m_CompressionHelperTypeNameIndex = 0;
        private int m_BuildEventHandlerTypeNameIndex = 0;

        private const string PrefsKeyPrefix = "AzCat.ResourceBuilder.";
        private bool m_AutoSavePrefs = true;

        [MenuItem("AZWorkingCat/资源工具/资源构建 Resource Builder", false, 40)]
        [MenuItem("Game Framework/Resource Tools/Resource Builder", false, 40)]
        private static void Open()
        {
            ResourceBuilder window = GetWindow<ResourceBuilder>("资源构建 (Resource Builder)", true);
#if UNITY_2019_3_OR_NEWER
            window.minSize = new Vector2(800f, 640f);
#else
            window.minSize = new Vector2(800f, 600f);
#endif
        }

        private void OnEnable()
        {
            m_Controller = new ResourceBuilderController();
            m_Controller.OnLoadingResource += OnLoadingResource;
            m_Controller.OnLoadingAsset += OnLoadingAsset;
            m_Controller.OnLoadCompleted += OnLoadCompleted;
            m_Controller.OnAnalyzingAsset += OnAnalyzingAsset;
            m_Controller.OnAnalyzeCompleted += OnAnalyzeCompleted;
            m_Controller.ProcessingAssetBundle += OnProcessingAssetBundle;
            m_Controller.ProcessingBinary += OnProcessingBinary;
            m_Controller.ProcessResourceComplete += OnProcessResourceComplete;
            m_Controller.BuildResourceError += OnBuildResourceError;

            m_OrderBuildResources = false;

            // 先尝试从 XML 配置加载
            bool xmlLoaded = m_Controller.Load();
            if (xmlLoaded)
            {
                Debug.Log("加载 XML 配置成功 (Load configuration success).");
            }
            else
            {
                Debug.LogWarning("加载 XML 配置失败 (Load configuration failure).");
            }

            // 再从 EditorPrefs 覆盖（EditorPrefs 优先级更高）
            LoadPrefs();

            // 刷新索引
            RefreshCompressionHelperIndex();
            RefreshBuildEventHandlerIndex();
        }

        private void LoadPrefs()
        {
            if (!EditorPrefs.HasKey(PrefsKeyPrefix + "Version"))
                return;

            m_Controller.InternalResourceVersion = EditorPrefs.GetInt(PrefsKeyPrefix + "InternalResourceVersion", m_Controller.InternalResourceVersion);
            string platforms = EditorPrefs.GetString(PrefsKeyPrefix + "Platforms", "");
            if (!string.IsNullOrEmpty(platforms))
                m_Controller.Platforms = (Platform)int.Parse(platforms);
            string compression = EditorPrefs.GetString(PrefsKeyPrefix + "AssetBundleCompression", "");
            if (!string.IsNullOrEmpty(compression))
                m_Controller.AssetBundleCompression = (AssetBundleCompressionType)byte.Parse(compression);
            m_Controller.AdditionalCompressionSelected = EditorPrefs.GetBool(PrefsKeyPrefix + "AdditionalCompressionSelected", m_Controller.AdditionalCompressionSelected);
            m_Controller.ForceRebuildAssetBundleSelected = EditorPrefs.GetBool(PrefsKeyPrefix + "ForceRebuildAssetBundleSelected", m_Controller.ForceRebuildAssetBundleSelected);
            m_Controller.OutputDirectory = EditorPrefs.GetString(PrefsKeyPrefix + "OutputDirectory", m_Controller.OutputDirectory);
            m_Controller.OutputPackageSelected = EditorPrefs.GetBool(PrefsKeyPrefix + "OutputPackageSelected", m_Controller.OutputPackageSelected);
            m_Controller.OutputFullSelected = EditorPrefs.GetBool(PrefsKeyPrefix + "OutputFullSelected", m_Controller.OutputFullSelected);
            m_Controller.OutputPackedSelected = EditorPrefs.GetBool(PrefsKeyPrefix + "OutputPackedSelected", m_Controller.OutputPackedSelected);
            string eventHandler = EditorPrefs.GetString(PrefsKeyPrefix + "BuildEventHandlerTypeName", "");
            if (!string.IsNullOrEmpty(eventHandler))
                m_Controller.BuildEventHandlerTypeName = eventHandler;
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefsKeyPrefix + "Version", "1");
            EditorPrefs.SetInt(PrefsKeyPrefix + "InternalResourceVersion", m_Controller.InternalResourceVersion);
            EditorPrefs.SetString(PrefsKeyPrefix + "Platforms", ((int)m_Controller.Platforms).ToString());
            EditorPrefs.SetString(PrefsKeyPrefix + "AssetBundleCompression", ((byte)m_Controller.AssetBundleCompression).ToString());
            EditorPrefs.SetBool(PrefsKeyPrefix + "AdditionalCompressionSelected", m_Controller.AdditionalCompressionSelected);
            EditorPrefs.SetBool(PrefsKeyPrefix + "ForceRebuildAssetBundleSelected", m_Controller.ForceRebuildAssetBundleSelected);
            EditorPrefs.SetString(PrefsKeyPrefix + "OutputDirectory", m_Controller.OutputDirectory);
            EditorPrefs.SetBool(PrefsKeyPrefix + "OutputPackageSelected", m_Controller.OutputPackageSelected);
            EditorPrefs.SetBool(PrefsKeyPrefix + "OutputFullSelected", m_Controller.OutputFullSelected);
            EditorPrefs.SetBool(PrefsKeyPrefix + "OutputPackedSelected", m_Controller.OutputPackedSelected);
            EditorPrefs.SetString(PrefsKeyPrefix + "BuildEventHandlerTypeName", m_Controller.BuildEventHandlerTypeName);
        }

        private void RefreshCompressionHelperIndex()
        {
            m_CompressionHelperTypeNameIndex = 0;
            string[] names = m_Controller.GetCompressionHelperTypeNames();
            for (int i = 0; i < names.Length; i++)
            {
                if (m_Controller.CompressionHelperTypeName == names[i])
                {
                    m_CompressionHelperTypeNameIndex = i;
                    break;
                }
            }
            m_Controller.RefreshCompressionHelper();
        }

        private void RefreshBuildEventHandlerIndex()
        {
            m_BuildEventHandlerTypeNameIndex = 0;
            string[] names = m_Controller.GetBuildEventHandlerTypeNames();
            for (int i = 0; i < names.Length; i++)
            {
                if (m_Controller.BuildEventHandlerTypeName == names[i])
                {
                    m_BuildEventHandlerTypeNameIndex = i;
                    break;
                }
            }
            m_Controller.RefreshBuildEventHandler();
        }

        private void Update()
        {
            if (m_OrderBuildResources)
            {
                m_OrderBuildResources = false;
                BuildResources();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height));
            {
                GUILayout.Space(5f);
                EditorGUILayout.LabelField(TR("环境信息 Environment Information"), EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("产品名 Product Name"), GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.ProductName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("公司名 Company Name"), GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.CompanyName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("游戏标识 Game Identifier"), GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.GameIdentifier);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("GF 版本 Game Framework Version"), GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.GameFrameworkVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("Unity 版本 Unity Version"), GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.UnityVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("适用游戏版本 Applicable Game Version"), GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.ApplicableGameVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField(TR("平台 Platforms"), EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal("box");
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.Windows, TR("Windows"));
                                DrawPlatform(Platform.Windows64, TR("Windows x64"));
                                DrawPlatform(Platform.MacOS, TR("macOS"));
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.Linux, TR("Linux"));
                                DrawPlatform(Platform.IOS, TR("iOS"));
                                DrawPlatform(Platform.Android, TR("Android"));
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.WindowsStore, TR("Windows Store"));
                                DrawPlatform(Platform.WebGL, TR("WebGL"));
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5f);
                EditorGUILayout.LabelField(TR("压缩 Compression"), EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("AB 压缩 AssetBundle Compression"), GUILayout.Width(200f));
                        m_Controller.AssetBundleCompression = (AssetBundleCompressionType)EditorGUILayout.EnumPopup(m_Controller.AssetBundleCompression);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("压缩辅助器 Compression Helper"), GUILayout.Width(200f));
                        string[] names = m_Controller.GetCompressionHelperTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(m_CompressionHelperTypeNameIndex, names);
                        if (selectedIndex != m_CompressionHelperTypeNameIndex)
                        {
                            m_CompressionHelperTypeNameIndex = selectedIndex;
                            m_Controller.CompressionHelperTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (m_Controller.RefreshCompressionHelper())
                            {
                                Debug.Log("设置压缩辅助器成功 (Set compression helper success).");
                            }
                            else
                            {
                                Debug.LogWarning("设置压缩辅助器失败 (Set compression helper failure).");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("额外压缩 Additional Compression"), GUILayout.Width(200f));
                        m_Controller.AdditionalCompressionSelected = EditorGUILayout.ToggleLeft(TR("使用压缩辅助器对完整包额外压缩"), m_Controller.AdditionalCompressionSelected);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
                EditorGUILayout.LabelField(TR("构建 Build"), EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("强制重建 AB Force Rebuild AssetBundle"), GUILayout.Width(200f));
                        m_Controller.ForceRebuildAssetBundleSelected = EditorGUILayout.Toggle(m_Controller.ForceRebuildAssetBundleSelected);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("构建事件处理 Build Event Handler"), GUILayout.Width(200f));
                        string[] names = m_Controller.GetBuildEventHandlerTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(m_BuildEventHandlerTypeNameIndex, names);
                        if (selectedIndex != m_BuildEventHandlerTypeNameIndex)
                        {
                            m_BuildEventHandlerTypeNameIndex = selectedIndex;
                            m_Controller.BuildEventHandlerTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (m_Controller.RefreshBuildEventHandler())
                            {
                                Debug.Log("设置构建事件处理成功 (Set build event handler success).");
                            }
                            else
                            {
                                Debug.LogWarning("设置构建事件处理失败 (Set build event handler failure).");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("内部资源版本 Internal Resource Version"), GUILayout.Width(200f));
                        m_Controller.InternalResourceVersion = EditorGUILayout.IntField(m_Controller.InternalResourceVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("资源版本 Resource Version"), GUILayout.Width(200f));
                        GUILayout.Label(Utility.Text.Format("{0} ({1})", m_Controller.ApplicableGameVersion, m_Controller.InternalResourceVersion));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("输出目录 Output Directory"), GUILayout.Width(200f));
                        m_Controller.OutputDirectory = EditorGUILayout.TextField(m_Controller.OutputDirectory);
                        if (GUILayout.Button(TR("浏览... Browse..."), GUILayout.Width(100f)))
                        {
                            BrowseOutputDirectory();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("工作路径 Working Path"), GUILayout.Width(200f));
                        GUILayout.Label(m_Controller.WorkingPath);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(!m_Controller.OutputPackageSelected);
                        EditorGUILayout.LabelField(TR("包输出路径 Output Package Path"), GUILayout.Width(200f));
                        GUILayout.Label(m_Controller.OutputPackagePath);
                        EditorGUI.EndDisabledGroup();
                        m_Controller.OutputPackageSelected = EditorGUILayout.ToggleLeft(TR("生成 Generate"), m_Controller.OutputPackageSelected, GUILayout.Width(80f));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(!m_Controller.OutputFullSelected);
                        EditorGUILayout.LabelField(TR("完整输出路径 Output Full Path"), GUILayout.Width(200f));
                        GUILayout.Label(m_Controller.OutputFullPath);
                        EditorGUI.EndDisabledGroup();
                        m_Controller.OutputFullSelected = EditorGUILayout.ToggleLeft(TR("生成 Generate"), m_Controller.OutputFullSelected, GUILayout.Width(80f));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(!m_Controller.OutputPackedSelected);
                        EditorGUILayout.LabelField(TR("分包输出路径 Output Packed Path"), GUILayout.Width(200f));
                        GUILayout.Label(m_Controller.OutputPackedPath);
                        EditorGUI.EndDisabledGroup();
                        m_Controller.OutputPackedSelected = EditorGUILayout.ToggleLeft(TR("生成 Generate"), m_Controller.OutputPackedSelected, GUILayout.Width(80f));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(TR("构建报告路径 Build Report Path"), GUILayout.Width(200f));
                        GUILayout.Label(m_Controller.BuildReportPath);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                string buildMessage = string.Empty;
                MessageType buildMessageType = MessageType.None;
                GetBuildMessage(out buildMessage, out buildMessageType);
                EditorGUILayout.HelpBox(buildMessage, buildMessageType);
                GUILayout.Space(2f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(m_Controller.Platforms == Platform.Undefined || string.IsNullOrEmpty(m_Controller.CompressionHelperTypeName) || !m_Controller.IsValidOutputDirectory);
                    {
                        if (GUILayout.Button(TR("开始构建资源 Start Build Resources")))
                        {
                            m_OrderBuildResources = true;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(TR("保存配置 Save"), GUILayout.Width(100f)))
                    {
                        SaveConfiguration();
                        SavePrefs();
                    }
                    m_AutoSavePrefs = EditorGUILayout.ToggleLeft(TR("自动保存 Auto Save"), m_AutoSavePrefs, GUILayout.Width(120f));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void BrowseOutputDirectory()
        {
            string directory = EditorUtility.OpenFolderPanel(TR("选择输出目录 Select Output Directory"), m_Controller.OutputDirectory, string.Empty);
            if (!string.IsNullOrEmpty(directory))
            {
                m_Controller.OutputDirectory = directory;
            }
        }

        private static string TR(string text)
        {
            return text;
        }

        private void GetBuildMessage(out string message, out MessageType messageType)
        {
            message = string.Empty;
            messageType = MessageType.Error;
            if (m_Controller.Platforms == Platform.Undefined)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += TR("平台未选择 (Platform is invalid).");
            }

            if (string.IsNullOrEmpty(m_Controller.CompressionHelperTypeName))
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += TR("压缩辅助器未设置 (Compression helper is invalid).");
            }

            if (!m_Controller.IsValidOutputDirectory)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += TR("输出目录无效 (Output directory is invalid).");
            }

            if (!string.IsNullOrEmpty(message))
            {
                return;
            }

            messageType = MessageType.Info;
            if (Directory.Exists(m_Controller.OutputPackagePath))
            {
                message += Utility.Text.Format(TR("{0} 将被覆盖 (will be overwritten)."), m_Controller.OutputPackagePath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(m_Controller.OutputFullPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format(TR("{0} 将被覆盖 (will be overwritten)."), m_Controller.OutputFullPath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(m_Controller.OutputPackedPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format(TR("{0} 将被覆盖 (will be overwritten)."), m_Controller.OutputPackedPath);
                messageType = MessageType.Warning;
            }

            if (messageType == MessageType.Warning)
            {
                return;
            }

            message = TR("准备就绪 (Ready to build).");
        }

        private void BuildResources()
        {
            if (m_Controller.BuildResources())
            {
                Debug.Log(TR("构建资源成功 (Build resources success)."));
                SaveConfiguration();
                if (m_AutoSavePrefs) SavePrefs();
            }
            else
            {
                Debug.LogWarning(TR("构建资源失败 (Build resources failure)."));
            }
        }

        private void SaveConfiguration()
        {
            if (m_Controller.Save())
            {
                Debug.Log(TR("保存配置成功 (Save configuration success)."));
            }
            else
            {
                Debug.LogWarning(TR("保存配置失败 (Save configuration failure)."));
            }
        }

        private void DrawPlatform(Platform platform, string platformName)
        {
            m_Controller.SelectPlatform(platform, EditorGUILayout.ToggleLeft(platformName, m_Controller.IsPlatformSelected(platform)));
        }

        private void OnLoadingResource(int index, int count)
        {
            EditorUtility.DisplayProgressBar(TR("加载资源 Loading Resources"), Utility.Text.Format(TR("加载资源中 Loading resources, {0}/{1}"), index, count), (float)index / count);
        }

        private void OnLoadingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar(TR("加载资产 Loading Assets"), Utility.Text.Format(TR("加载资产中 Loading assets, {0}/{1}"), index, count), (float)index / count);
        }

        private void OnLoadCompleted()
        {
            EditorUtility.ClearProgressBar();
        }

        private void OnAnalyzingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar(TR("分析资产 Analyzing Assets"), Utility.Text.Format(TR("分析资产中 Analyzing assets, {0}/{1}"), index, count), (float)index / count);
        }

        private void OnAnalyzeCompleted()
        {
            EditorUtility.ClearProgressBar();
        }

        private bool OnProcessingAssetBundle(string assetBundleName, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar(TR("处理 AssetBundle Processing AssetBundle"), Utility.Text.Format(TR("处理中 Processing '{0}'..."), assetBundleName), progress))
            {
                EditorUtility.ClearProgressBar();
                return true;
            }
            else
            {
                Repaint();
                return false;
            }
        }

        private bool OnProcessingBinary(string binaryName, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar(TR("处理二进制 Processing Binary"), Utility.Text.Format(TR("处理中 Processing '{0}'..."), binaryName), progress))
            {
                EditorUtility.ClearProgressBar();
                return true;
            }
            else
            {
                Repaint();
                return false;
            }
        }

        private void OnProcessResourceComplete(Platform platform)
        {
            EditorUtility.ClearProgressBar();
            Debug.Log(Utility.Text.Format(TR("构建平台 '{0}' 资源完成 (Build resources complete)."), platform));
        }

        private void OnBuildResourceError(string errorMessage)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogWarning(Utility.Text.Format(TR("构建资源出错 Build resources error: '{0}'"), errorMessage));
        }
    }
}
