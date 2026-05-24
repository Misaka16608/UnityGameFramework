//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------
// AzCat Mod: 中文支持 + 界面优化

using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源同步工具。
    /// </summary>
    internal sealed class ResourceSyncTools : EditorWindow
    {
        private const float ButtonHeight = 50f;
        private const float ButtonSpace = 5f;
        private ResourceSyncToolsController m_Controller = null;

        [MenuItem("AZWorkingCat/资源工具/资源同步 Resource Sync Tools", false, 44)]
        [MenuItem("Game Framework/Resource Tools/Resource Sync Tools", false, 44)]
        private static void Open()
        {
            ResourceSyncTools window = GetWindow<ResourceSyncTools>("资源同步 (Resource Sync Tools)", true);
#if UNITY_2019_3_OR_NEWER
            window.minSize = new Vector2(480, 220f);
#else
            window.minSize = new Vector2(480, 230f);
#endif
        }

        private void OnEnable()
        {
            m_Controller = new ResourceSyncToolsController();
            m_Controller.OnLoadingResource += OnLoadingResource;
            m_Controller.OnLoadingAsset += OnLoadingAsset;
            m_Controller.OnCompleted += OnCompleted;
            m_Controller.OnResourceDataChanged += OnResourceDataChanged;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height));
            {
                GUILayout.Space(ButtonSpace);

                EditorGUILayout.HelpBox(
                    TR("Resource Sync Tools 用于同步 Unity 的 AssetBundle Label 与 ResourceCollection.xml 配置。\n\n1. 清除项目中所有 AB 标记 → 2. 将 XML 同步到项目 → 3. 从项目同步回 XML"),
                    MessageType.Info);

                GUILayout.Space(ButtonSpace);

                if (GUILayout.Button(
                    TR("清除所有 AssetBundle 标记\n(Remove All Asset Bundle Names in Project)"),
                    GUILayout.Height(ButtonHeight)))
                {
                    if (!m_Controller.RemoveAllAssetBundleNames())
                    {
                        Debug.LogWarning(TR("清除失败 (Remove All Asset Bundle Names in Project failure)."));
                    }
                    else
                    {
                        Debug.Log(TR("清除完成 (Remove All Asset Bundle Names in Project completed)."));
                    }
                    AssetDatabase.Refresh();
                }

                GUILayout.Space(ButtonSpace);
                if (GUILayout.Button(
                    TR("XML → 项目\n(Sync ResourceCollection.xml to Project)"),
                    GUILayout.Height(ButtonHeight)))
                {
                    if (!m_Controller.SyncToProject())
                    {
                        Debug.LogWarning(TR("同步失败 (Sync ResourceCollection.xml to Project failure)."));
                    }
                    else
                    {
                        Debug.Log(TR("同步完成 (Sync ResourceCollection.xml to Project completed)."));
                    }
                    AssetDatabase.Refresh();
                }

                GUILayout.Space(ButtonSpace);
                if (GUILayout.Button(
                    TR("项目 → XML\n(Sync ResourceCollection.xml from Project)"),
                    GUILayout.Height(ButtonHeight)))
                {
                    if (!m_Controller.SyncFromProject())
                    {
                        Debug.LogWarning(TR("同步失败 (Sync Project to ResourceCollection.xml failure)."));
                    }
                    else
                    {
                        Debug.Log(TR("同步完成 (Sync Project to ResourceCollection.xml completed)."));
                    }
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static string TR(string text)
        {
            return text;
        }

        private void OnLoadingResource(int index, int count)
        {
            EditorUtility.DisplayProgressBar(TR("加载资源 Loading Resources"), Utility.Text.Format(TR("加载资源中 {0}/{1}"), index, count), (float)index / count);
        }

        private void OnLoadingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar(TR("加载资产 Loading Assets"), Utility.Text.Format(TR("加载资产中 {0}/{1}"), index, count), (float)index / count);
        }

        private void OnCompleted()
        {
            EditorUtility.ClearProgressBar();
        }

        private void OnResourceDataChanged(int index, int count, string assetName)
        {
            EditorUtility.DisplayProgressBar(TR("处理资产 Processing Assets"), Utility.Text.Format("({0}/{1}) {2}", index, count, assetName), (float)index / count);
        }
    }
}
