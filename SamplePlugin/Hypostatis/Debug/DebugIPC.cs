using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json;

namespace Hypostasis.Debug;

public sealed class HypostasisMemberDebugInfo
{
    public enum MemberDebugType
    {
        None,
        Pointer,
        Primitive,
        Hook,
        AsmHook,
        AsmPatch,
        GameFunction
    }

    public Util.AssignableInfo AssignableInfo { get; set; }
    [JsonIgnore]
    public HypostasisMemberInjectionAttribute InjectionAttribute => SignatureInjectionAttribute as HypostasisMemberInjectionAttribute ?? CSInjectionAttribute;
    public HypostasisSignatureInjectionAttribute SignatureInjectionAttribute { get; set; }
    public HypostasisClientStructsInjectionAttribute CSInjectionAttribute { get; set; }

    [JsonIgnore]
    public string Signature
    {
        get
        {
            if (SignatureInjectionAttribute != null) return SignatureInjectionAttribute.Signature;

            try
            {
                var o = AssignableInfo?.GetValue();
                if (o == null) return string.Empty;

                switch (DebugType)
                {
                    case MemberDebugType.AsmPatch:
                    case MemberDebugType.GameFunction:
                        var property = o.GetType().GetProperty(nameof(AsmPatch.Signature))?.GetValue(o);
                        return property != null ? (string)property : string.Empty;
                }
            }
            catch { }

            return string.Empty;
        }
    }

    [JsonIgnore]
    public nint Address
    {
        get
        {
            try
            {
                var o = AssignableInfo?.GetValue();
                if (o == null) return nint.Zero;

                switch (DebugType)
                {
                    case MemberDebugType.Pointer:
                    case MemberDebugType.Primitive:
                        return Util.ConvertObjectToIntPtr(o);
                    case MemberDebugType.Hook:
                    case MemberDebugType.AsmHook:
                    case MemberDebugType.AsmPatch:
                    case MemberDebugType.GameFunction:
                        var property = o.GetType().GetProperty(nameof(AsmPatch.Address))?.GetValue(o);
                        return property != null ? (nint)property : nint.Zero;
                }
            }
            catch { }

            return nint.Zero;
        }
    }

    public MemberDebugType DebugType { get; set; }

    public HypostasisMemberDebugInfo() { }

    public HypostasisMemberDebugInfo(MemberInfo memberInfo)
    {
        var attribute = memberInfo.GetCustomAttribute<HypostasisMemberInjectionAttribute>();
        SignatureInjectionAttribute = attribute as HypostasisSignatureInjectionAttribute;
        CSInjectionAttribute = attribute as HypostasisClientStructsInjectionAttribute;

        var type = memberInfo.GetObjectType();
        if (type == typeof(nint) || type.IsPointer || type.IsAssignableTo(typeof(Delegate)))
        {
            DebugType = MemberDebugType.Pointer;
        }
        else if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(Hook<>))
                DebugType = MemberDebugType.Hook;
            else if (genericType == typeof(GameFunction<>))
                DebugType = MemberDebugType.GameFunction;
        }
        else if (type.IsPrimitive)
        {
            DebugType = MemberDebugType.Primitive;
        }
        else if (type == typeof(AsmHook))
        {
            DebugType = MemberDebugType.AsmHook;
        }
        else if (type == typeof(AsmPatch))
        {
            DebugType = MemberDebugType.AsmPatch;
        }
    }
}

public static class DebugIPC
{
    public const string HypostasisTag = "_HYPOSTASISPLUGINS";

    public static bool DebugHypostasis { get; set; }
    public static ICallGateProvider<IDalamudPlugin> GetPluginProvider { get; private set; }
    public static ICallGateProvider<Hypostasis.PluginState> GetPluginStateProvider { get; private set; }
    public static ICallGateProvider<List<HypostasisMemberDebugInfo>> GetDebugInfosProvider { get; private set; }
    public static ICallGateProvider<Dictionary<int, (object, MemberInfo)>> GetMemberInfosProvider { get; private set; }

    private static readonly List<HypostasisMemberDebugInfo> debugInfos = [];
    private static readonly Dictionary<int, (object, MemberInfo)> memberInfos = [];
    private static readonly Dictionary<Type, object> injectedObjects = [];

    [Conditional("DEBUG")]
    public static void Initialize(IDalamudPlugin plugin)
    {
        var name = Hypostasis.PluginName;
        GetPluginProvider = DalamudApi.PluginInterface.GetIpcProvider<IDalamudPlugin>($"{name}.{nameof(Hypostasis)}.GetPlugin");
        GetPluginProvider.RegisterFunc(() => plugin);
        GetPluginStateProvider = DalamudApi.PluginInterface.GetIpcProvider<Hypostasis.PluginState>($"{name}.{nameof(Hypostasis)}.GetPluginState");
        GetPluginStateProvider.RegisterFunc(() => Hypostasis.State);
        GetDebugInfosProvider = DalamudApi.PluginInterface.GetIpcProvider<List<HypostasisMemberDebugInfo>>($"{name}.{nameof(Hypostasis)}.GetDebugInfos");
        GetDebugInfosProvider.RegisterFunc(() => debugInfos);
        GetMemberInfosProvider = DalamudApi.PluginInterface.GetIpcProvider<Dictionary<int, (object, MemberInfo)>>($"{name}.{nameof(Hypostasis)}.GetMemberInfos");
        GetMemberInfosProvider.RegisterFunc(() => memberInfos);
        DalamudApi.Framework.RunOnTick(EnableDebugging);
    }

    private static void EnableDebugging()
    {
        var plugins = DalamudApi.PluginInterface.GetOrCreateData(HypostasisTag, () => new HashSet<string>());
        lock (plugins)
            plugins.Add(Hypostasis.PluginName);
    }

    private static void DisableDebugging()
    {
        if (!DalamudApi.PluginInterface.TryGetData<HashSet<string>>(HypostasisTag, out var plugins)) return;
        lock (plugins)
            plugins.Remove(Hypostasis.PluginName);
    }

    [Conditional("DEBUG")]
    public static void AddInjectedObject(object o) => injectedObjects[o.GetType()] = o;

    [Conditional("DEBUG")]
    public static void SetupDebugMembers()
    {
        var debuggableTypes = Util.Assembly.GetTypesWithAttribute<HypostasisDebuggableAttribute>().Select(t => t.Item1).ToHashSet();
        foreach (var type in DebugHypostasis ? Util.AssemblyTypes : Util.AssemblyTypes.Where(type => type.Namespace is { } ns && !ns.Contains(nameof(Hypostasis))))
        {
            foreach (var memberInfo in type.GetAllMembers().Where(memberInfo => memberInfo.MemberType is MemberTypes.Field or MemberTypes.Property
                && (memberInfo.GetCustomAttribute<HypostasisMemberInjectionAttribute>() != null || memberInfo.GetCustomAttribute<HypostasisDebuggableAttribute>() != null
                    || (memberInfo.GetObjectType() is { } objectType && debuggableTypes.Contains(!objectType.IsGenericType ? objectType : objectType.GetGenericTypeDefinition())))))
            {
                AddDebugMember(memberInfo);
            }
        }
    }

    private static void AddDebugMember(MemberInfo memberInfo)
    {
        var ownerType = memberInfo.ReflectedType;
        if (ownerType == null) return;

        injectedObjects.TryGetValue(ownerType, out var o);
        memberInfos.Add(debugInfos.Count, (o, memberInfo));
        debugInfos.Add(new(memberInfo));
    }

    [Conditional("DEBUG")]
    public static void Dispose()
    {
        DisableDebugging();
        DalamudApi.PluginInterface.RelinquishData(HypostasisTag);
        GetPluginProvider?.UnregisterFunc();
        GetPluginStateProvider?.UnregisterFunc();
        GetDebugInfosProvider?.UnregisterFunc();
        GetMemberInfosProvider?.UnregisterFunc();
    }
}