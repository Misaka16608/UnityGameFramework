//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Procedure;
using System;
using System.Collections.Generic;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 流程注册表 — 通过静态工厂方法消除运行时反射。
    /// </summary>
    /// <remarks>
    /// 项目侧生成工厂文件在 <c>[RuntimeInitializeOnLoadMethod]</c> 中调用 <see cref="Register"/>，
    /// <see cref="ProcedureComponent"/> 在 <see cref="Create"/> 中优先查表，未命中时降级到反射。
    /// </remarks>
    public static class ProcedureRegistry
    {
        private static readonly Dictionary<string, Func<ProcedureBase>> s_Factories =
            new Dictionary<string, Func<ProcedureBase>>(StringComparer.Ordinal);

        /// <summary>
        /// 注册一个流程工厂。
        /// </summary>
        /// <param name="typeName">流程的完全限定类型名（与 prefab 中序列化的一致）。</param>
        /// <param name="factory">创建该流程实例的委托。</param>
        public static void Register(string typeName, Func<ProcedureBase> factory)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Log.Error("ProcedureRegistry.Register: typeName is null or empty.");
                return;
            }

            if (factory == null)
            {
                Log.Error("ProcedureRegistry.Register: factory is null for type '{0}'.", typeName);
                return;
            }

            s_Factories[typeName] = factory;
        }

        /// <summary>
        /// 通过类型名创建流程实例。
        /// </summary>
        /// <param name="typeName">流程的完全限定类型名。</param>
        /// <returns>创建的流程实例，未注册时返回 null。</returns>
        public static ProcedureBase Create(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Log.Error("ProcedureRegistry.Create: typeName is null or empty.");
                return null;
            }

            if (s_Factories.TryGetValue(typeName, out Func<ProcedureBase> factory))
            {
                return factory();
            }

            return null;
        }
    }
}
