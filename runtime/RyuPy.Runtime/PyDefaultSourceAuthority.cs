// 默认AOT计划只授权真实Registration引用；公开名称和同值重注册不继承权限。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace RyuPy.Runtime;
public static partial class PyModules
{
    private static readonly HashSet<Registration> DefaultSourceAuthority = new(ReferenceEqualityComparer.Instance);
    private static readonly ConditionalWeakTable<PyModule, Registration> DefaultModuleAuthority = new();
    private static readonly Dictionary<PyCompiledModuleCode, Registration> DefaultCodeAuthority = new(ReferenceEqualityComparer.Instance);

    private static void BindDefaultSource(PyModule module, Registration registration)
    {
        if (DefaultSourceAuthority.Contains(registration))
        {
            if (DefaultModuleAuthority.TryGetValue(module, out var old))
            {
                if (!ReferenceEquals(old, registration)) throw new PyRuntimeError("conflicting default source authority");
            }
            else DefaultModuleAuthority.Add(module, registration);
        }
    }
    internal static void BindDefaultInstance(PyModule module, PyCompiledModuleCode code)
    {
        if (DefaultCodeAuthority.TryGetValue(code, out var registration))
        {
            BindDefaultSource(module, registration);
            if (!ResourceLocations.TryGetValue(module, out _)) ResourceLocations.Add(module, registration.ResourceLocation!);
        }
    }
    // 只对新计划中任一侧作强制检查；双方不属于新域时完全保旧显式kind2语义。
    private static bool DefaultSourcePair(PyModule? parent, Registration child)
    {
        if (parent is null) return false;
        bool childOwned = DefaultSourceAuthority.Contains(child);
        bool parentOwned = DefaultModuleAuthority.TryGetValue(parent, out var source);
        if (!childOwned && !parentOwned) return false;
        if (!childOwned || !parentOwned || !DefaultSourceAuthority.Contains(source!)
            || source!.ResourceLocation is not { Kind: 2, IsPackage: true }
            || child.ResourceLocation is not { Kind: 2 } || !child.MetadataEnabled)
            throw ImportPathUnsupported("mixed or unauthorised default source parents");
        return true;
    }
}
