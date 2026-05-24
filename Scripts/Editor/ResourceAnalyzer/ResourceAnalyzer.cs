//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------
// AzCat Mod: 中文支持

using GameFramework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源分析器。
    /// </summary>
    internal sealed class ResourceAnalyzer : EditorWindow
    {
        private ResourceAnalyzerController m_Controller = null;
        private bool m_Analyzed = false;
        private int m_ToolbarIndex = 0;

        private int m_AssetCount = 0;
        private string[] m_CachedAssetNames = null;
        private int m_SelectedAssetIndex = -1;
        private string m_SelectedAssetName = null;
        private DependencyData m_SelectedDependencyData = null;
        private AssetsOrder m_AssetsOrder = AssetsOrder.AssetNameAsc;
        private string m_AssetsFilter = null;
        private Vector2 m_AssetsScroll = Vector2.zero;
        private Vector2 m_DependencyResourcesScroll = Vector2.zero;
        private Vector2 m_DependencyAssetsScroll = Vector2.zero;
        private Vector2 m_ScatteredDependencyAssetsScroll = Vector2.zero;

        private int m_ScatteredAssetCount = 0;
        private string[] m_CachedScatteredAssetNames = null;
        private int m_SelectedScatteredAssetIndex = -1;
        private string m_SelectedScatteredAssetName = null;
        private Asset[] m_SelectedHostAssets = null;
        private ScatteredAssetsOrder m_ScatteredAssetsOrder = ScatteredAssetsOrder.AssetNameAsc;
        private string m_ScatteredAssetsFilter = null;
        private Vector2 m_ScatteredAssetsScroll = Vector2.zero;
        private Vector2 m_HostAssetsScroll = Vector2.zero;

        private int m_CircularDependencyCount = 0;
        private string[][] m_CachedCircularDependencyDatas = null;
        private Vector2 m_CircularDependencyScroll = Vector2.zero;

        private static readonly string[] m_ToolbarNames = new string[] { "概览 Summary", "资源依赖 Asset Dependency", "散落资源 Scattered Assets", "循环依赖 Circular Dependency" };

        [MenuItem("AZWorkingCat/资源工具/资源分析 Resource Analyzer", false, 42)]
        [MenuItem("Game Framework/Resource Tools/Resource Analyzer", false, 42)]
        private static void Open()
        {
            ResourceAnalyzer window = GetWindow<ResourceAnalyzer>("资源分析 (Resource Analyzer)", true);
            window.minSize = new Vector2(800f, 600f);
        }

        private void OnEnable()
        {
            m_Controller = new ResourceAnalyzerController();
            m_Controller.OnLoadingResource += OnLoadingResource;
            m_Controller.OnLoadingAsset += OnLoadingAsset;
            m_Controller.OnLoadCompleted += OnLoadCompleted;
            m_Controller.OnAnalyzingAsset += OnAnalyzingAsset;
            m_Controller.OnAnalyzeCompleted += OnAnalyzeCompleted;

            m_Analyzed = false;
            m_ToolbarIndex = 0;

            m_AssetCount = 0;
            m_CachedAssetNames = null;
            m_SelectedAssetIndex = -1;
            m_SelectedAssetName = null;
            m_SelectedDependencyData = new DependencyData();
            m_AssetsOrder = AssetsOrder.ScatteredDependencyAssetCountDesc;
            m_AssetsFilter = null;
            m_AssetsScroll = Vector2.zero;
            m_DependencyResourcesScroll = Vector2.zero;
            m_DependencyAssetsScroll = Vector2.zero;
            m_ScatteredDependencyAssetsScroll = Vector2.zero;

            m_ScatteredAssetCount = 0;
            m_CachedScatteredAssetNames = null;
            m_SelectedScatteredAssetIndex = -1;
            m_SelectedScatteredAssetName = null;
            m_SelectedHostAssets = new Asset[] { };
            m_ScatteredAssetsOrder = ScatteredAssetsOrder.HostAssetCountDesc;
            m_ScatteredAssetsFilter = null;
            m_ScatteredAssetsScroll = Vector2.zero;
            m_HostAssetsScroll = Vector2.zero;

            m_CircularDependencyCount = 0;
            m_CachedCircularDependencyDatas = null;
            m_CircularDependencyScroll = Vector2.zero;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height));
            {
                GUILayout.Space(5f);
                int toolbarIndex = GUILayout.Toolbar(m_ToolbarIndex, m_ToolbarNames, GUILayout.Height(30f));
                if (toolbarIndex != m_ToolbarIndex)
                {
                    m_ToolbarIndex = toolbarIndex;
                    GUI.FocusControl(null);
                }

                switch (m_ToolbarIndex)
                {
                    case 0:
                        DrawSummary();
                        break;

                    case 1:
                        DrawAssetDependencyViewer();
                        break;

                    case 2:
                        DrawScatteredAssetViewer();
                        break;

                    case 3:
                        DrawCircularDependencyViewer();
                        break;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAnalyzeButton()
        {
            if (!m_Analyzed)
            {
                EditorGUILayout.HelpBox(TR("请先进行分析 (Please analyze first)."), MessageType.Info);
            }

            if (GUILayout.Button(TR("开始分析 Analyze"), GUILayout.Height(30f)))
            {
                m_Controller.Clear();

                m_SelectedAssetIndex = -1;
                m_SelectedAssetName = null;
                m_SelectedDependencyData = new DependencyData();

                m_SelectedScatteredAssetIndex = -1;
                m_SelectedScatteredAssetName = null;
                m_SelectedHostAssets = new Asset[] { };

                if (m_Controller.Prepare())
                {
                    m_Controller.Analyze();
                    m_Analyzed = true;
                    m_AssetCount = m_Controller.GetAssetNames().Length;
                    m_ScatteredAssetCount = m_Controller.GetScatteredAssetNames().Length;
                    m_CachedCircularDependencyDatas = m_Controller.GetCircularDependencyDatas();
                    m_CircularDependencyCount = m_CachedCircularDependencyDatas.Length;
                    OnAssetsOrderOrFilterChanged();
                    OnScatteredAssetsOrderOrFilterChanged();
                }
                else
                {
                    EditorUtility.DisplayDialog(TR("资源分析 Resource Analyze"), TR("无法解析 'ResourceCollection.xml'，请先使用资源编辑器 (Resource Editor) 工具。"), TR("确定 OK"));
                }
            }
        }

        private void DrawSummary()
        {
            DrawAnalyzeButton();
        }

        private void DrawAssetDependencyViewer()
        {
            if (!m_Analyzed)
            {
                DrawAnalyzeButton();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(5f);
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
                {
                    GUILayout.Space(5f);
                    string title = null;
                    if (string.IsNullOrEmpty(m_AssetsFilter))
                    {
                        title = Utility.Text.Format(TR("资源内资产 Assets In Resources ({0})"), m_AssetCount);
                    }
                    else
                    {
                        title = Utility.Text.Format(TR("资源内资产 Assets In Resources ({0}/{1})"), m_CachedAssetNames.Length, m_AssetCount);
                    }
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height - 150f));
                    {
                        m_AssetsScroll = EditorGUILayout.BeginScrollView(m_AssetsScroll);
                        {
                            int selectedIndex = GUILayout.SelectionGrid(m_SelectedAssetIndex, m_CachedAssetNames, 1, "toggle");
                            if (selectedIndex != m_SelectedAssetIndex)
                            {
                                m_SelectedAssetIndex = selectedIndex;
                                m_SelectedAssetName = m_CachedAssetNames[selectedIndex];
                                m_SelectedDependencyData = m_Controller.GetDependencyData(m_SelectedAssetName);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.LabelField(TR("资产名 Asset Name"), m_SelectedAssetName ?? "<None>");
                        EditorGUILayout.LabelField(TR("资源名 Resource Name"), m_SelectedAssetName == null ? "<None>" : m_Controller.GetAsset(m_SelectedAssetName).Resource.FullName);
                        EditorGUILayout.BeginHorizontal();
                        {
                            AssetsOrder assetsOrder = (AssetsOrder)EditorGUILayout.EnumPopup(TR("排序 Order by"), m_AssetsOrder);
                            if (assetsOrder != m_AssetsOrder)
                            {
                                m_AssetsOrder = assetsOrder;
                                OnAssetsOrderOrFilterChanged();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            string assetsFilter = EditorGUILayout.TextField(TR("过滤 Filter"), m_AssetsFilter);
                            if (assetsFilter != m_AssetsFilter)
                            {
                                m_AssetsFilter = assetsFilter;
                                OnAssetsOrderOrFilterChanged();
                            }
                            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_AssetsFilter));
                            {
                                if (GUILayout.Button(TR("×"), GUILayout.Width(20f)))
                                {
                                    m_AssetsFilter = null;
                                    GUI.FocusControl(null);
                                    OnAssetsOrderOrFilterChanged();
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f - 14f));
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format(TR("依赖资源 Dependency Resources ({0})"), m_SelectedDependencyData.DependencyResourceCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height * 0.2f));
                    {
                        m_DependencyResourcesScroll = EditorGUILayout.BeginScrollView(m_DependencyResourcesScroll);
                        {
                            Resource[] dependencyResources = m_SelectedDependencyData.GetDependencyResources();
                            foreach (Resource dependencyResource in dependencyResources)
                            {
                                GUILayout.Label(dependencyResource.FullName);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.LabelField(Utility.Text.Format(TR("依赖资产 Dependency Assets ({0})"), m_SelectedDependencyData.DependencyAssetCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height * 0.3f));
                    {
                        m_DependencyAssetsScroll = EditorGUILayout.BeginScrollView(m_DependencyAssetsScroll);
                        {
                            Asset[] dependencyAssets = m_SelectedDependencyData.GetDependencyAssets();
                            foreach (Asset dependencyAsset in dependencyAssets)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                    {
                                        m_SelectedAssetName = dependencyAsset.Name;
                                        m_SelectedAssetIndex = new List<string>(m_CachedAssetNames).IndexOf(m_SelectedAssetName);
                                        m_SelectedDependencyData = m_Controller.GetDependencyData(m_SelectedAssetName);
                                    }

                                    GUILayout.Label(dependencyAsset.Name);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.LabelField(Utility.Text.Format(TR("散落依赖资产 Scattered Dependency Assets ({0})"), m_SelectedDependencyData.ScatteredDependencyAssetCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height * 0.5f - 116f));
                    {
                        m_ScatteredDependencyAssetsScroll = EditorGUILayout.BeginScrollView(m_ScatteredDependencyAssetsScroll);
                        {
                            string[] scatteredDependencyAssetNames = m_SelectedDependencyData.GetScatteredDependencyAssetNames();
                            foreach (string scatteredDependencyAssetName in scatteredDependencyAssetNames)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    int count = m_Controller.GetHostAssets(scatteredDependencyAssetName).Length;
                                    EditorGUI.BeginDisabledGroup(count < 2);
                                    {
                                        if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                        {
                                            m_SelectedScatteredAssetName = scatteredDependencyAssetName;
                                            m_SelectedScatteredAssetIndex = new List<string>(m_CachedScatteredAssetNames).IndexOf(m_SelectedScatteredAssetName);
                                            m_SelectedHostAssets = m_Controller.GetHostAssets(m_SelectedScatteredAssetName);
                                            m_ToolbarIndex = 2;
                                            GUI.FocusControl(null);
                                        }
                                    }
                                    EditorGUI.EndDisabledGroup();
                                    GUILayout.Label(count > 1 ? Utility.Text.Format("{0} ({1})", scatteredDependencyAssetName, count) : scatteredDependencyAssetName);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawScatteredAssetViewer()
        {
            if (!m_Analyzed)
            {
                DrawAnalyzeButton();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(5f);
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
                {
                    GUILayout.Space(5f);
                    string title = null;
                    if (string.IsNullOrEmpty(m_ScatteredAssetsFilter))
                    {
                        title = Utility.Text.Format(TR("散落资产 Scattered Assets ({0})"), m_ScatteredAssetCount);
                    }
                    else
                    {
                        title = Utility.Text.Format(TR("散落资产 Scattered Assets ({0}/{1})"), m_CachedScatteredAssetNames.Length, m_ScatteredAssetCount);
                    }
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height - 132f));
                    {
                        m_ScatteredAssetsScroll = EditorGUILayout.BeginScrollView(m_ScatteredAssetsScroll);
                        {
                            int selectedIndex = GUILayout.SelectionGrid(m_SelectedScatteredAssetIndex, m_CachedScatteredAssetNames, 1, "toggle");
                            if (selectedIndex != m_SelectedScatteredAssetIndex)
                            {
                                m_SelectedScatteredAssetIndex = selectedIndex;
                                m_SelectedScatteredAssetName = m_CachedScatteredAssetNames[selectedIndex];
                                m_SelectedHostAssets = m_Controller.GetHostAssets(m_SelectedScatteredAssetName);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.LabelField(TR("散落资产名 Scattered Asset Name"), m_SelectedScatteredAssetName ?? "<None>");
                        EditorGUILayout.BeginHorizontal();
                        {
                            ScatteredAssetsOrder scatteredAssetsOrder = (ScatteredAssetsOrder)EditorGUILayout.EnumPopup(TR("排序 Order by"), m_ScatteredAssetsOrder);
                            if (scatteredAssetsOrder != m_ScatteredAssetsOrder)
                            {
                                m_ScatteredAssetsOrder = scatteredAssetsOrder;
                                OnScatteredAssetsOrderOrFilterChanged();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            string scatteredAssetsFilter = EditorGUILayout.TextField(TR("过滤 Filter"), m_ScatteredAssetsFilter);
                            if (scatteredAssetsFilter != m_ScatteredAssetsFilter)
                            {
                                m_ScatteredAssetsFilter = scatteredAssetsFilter;
                                OnScatteredAssetsOrderOrFilterChanged();
                            }
                            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_ScatteredAssetsFilter));
                            {
                                if (GUILayout.Button(TR("×"), GUILayout.Width(20f)))
                                {
                                    m_ScatteredAssetsFilter = null;
                                    GUI.FocusControl(null);
                                    OnScatteredAssetsOrderOrFilterChanged();
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f - 14f));
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format(TR("宿主资产 Host Assets ({0})"), m_SelectedHostAssets.Length), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height - 68f));
                    {
                        m_HostAssetsScroll = EditorGUILayout.BeginScrollView(m_HostAssetsScroll);
                        {
                            foreach (Asset hostAsset in m_SelectedHostAssets)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                    {
                                        m_SelectedAssetName = hostAsset.Name;
                                        m_SelectedAssetIndex = new List<string>(m_CachedAssetNames).IndexOf(m_SelectedAssetName);
                                        m_SelectedDependencyData = m_Controller.GetDependencyData(m_SelectedAssetName);
                                        m_ToolbarIndex = 1;
                                        GUI.FocusControl(null);
                                    }

                                    GUILayout.Label(Utility.Text.Format("{0} [{1}]", hostAsset.Name, hostAsset.Resource.FullName));
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCircularDependencyViewer()
        {
            if (!m_Analyzed)
            {
                DrawAnalyzeButton();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(5f);
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format(TR("循环依赖 Circular Dependency ({0})"), m_CircularDependencyCount), EditorStyles.boldLabel);
                    m_CircularDependencyScroll = EditorGUILayout.BeginScrollView(m_CircularDependencyScroll);
                    {
                        int count = 0;
                        foreach (string[] circularDependencyData in m_CachedCircularDependencyDatas)
                        {
                            GUILayout.Label(Utility.Text.Format("{0}) {1}", ++count, circularDependencyData[circularDependencyData.Length - 1]), EditorStyles.boldLabel);
                            EditorGUILayout.BeginVertical("box");
                            {
                                foreach (string circularDependency in circularDependencyData)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label(circularDependency);
                                        if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                        {
                                            m_SelectedAssetName = circularDependency;
                                            m_SelectedAssetIndex = new List<string>(m_CachedAssetNames).IndexOf(m_SelectedAssetName);
                                            m_SelectedDependencyData = m_Controller.GetDependencyData(m_SelectedAssetName);
                                            m_ToolbarIndex = 1;
                                            GUI.FocusControl(null);
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                            EditorGUILayout.EndVertical();
                            GUILayout.Space(5f);
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnAssetsOrderOrFilterChanged()
        {
            m_CachedAssetNames = m_Controller.GetAssetNames(m_AssetsOrder, m_AssetsFilter);
            if (!string.IsNullOrEmpty(m_SelectedAssetName))
            {
                m_SelectedAssetIndex = new List<string>(m_CachedAssetNames).IndexOf(m_SelectedAssetName);
            }
        }

        private void OnScatteredAssetsOrderOrFilterChanged()
        {
            m_CachedScatteredAssetNames = m_Controller.GetScatteredAssetNames(m_ScatteredAssetsOrder, m_ScatteredAssetsFilter);
            if (!string.IsNullOrEmpty(m_SelectedScatteredAssetName))
            {
                m_SelectedScatteredAssetIndex = new List<string>(m_CachedScatteredAssetNames).IndexOf(m_SelectedScatteredAssetName);
            }
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

        private void OnLoadCompleted()
        {
            EditorUtility.ClearProgressBar();
        }

        private void OnAnalyzingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar(TR("分析资产 Analyzing Assets"), Utility.Text.Format(TR("分析资产中 {0}/{1}"), index, count), (float)index / count);
        }

        private void OnAnalyzeCompleted()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
