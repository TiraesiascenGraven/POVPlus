using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Hypostasis.Debug;

namespace Hypostasis.Dalamud;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field), Conditional("DEBUG")]
public class HypostasisDebuggableAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class HypostasisInjectionAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public abstract class HypostasisMemberInjectionAttribute : HypostasisInjectionAttribute
{
    public string DetourName { get; init; } = null;
    public int Offset { get; init; } = 0;
    public bool Required { get; init; } = false;
    public bool EnableHook { get; init; } = true;
    public bool DisposeHook { get; init; } = true;
}

public sealed class HypostasisSignatureInjectionAttribute(string signature) : HypostasisMemberInjectionAttribute
{
    public string Signature { get; init; } = signature;
    public bool Static { get; init; } = false;
}

public class HypostasisClientStructsInjectionAttribute : HypostasisMemberInjectionAttribute
{
    public Type ClientStructsType { get; init; }
    public string MemberName { get; init; } = nameof(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance);
    protected HypostasisClientStructsInjectionAttribute() { }
    public HypostasisClientStructsInjectionAttribute(Type type) => ClientStructsType = type;
}

public sealed class HypostasisClientStructsInjectionAttribute<T> : HypostasisClientStructsInjectionAttribute
{
    public HypostasisClientStructsInjectionAttribute() => ClientStructsType = typeof(T);
}

[AttributeUsage(AttributeTargets.Struct)]
public class GameStructureAttribute(string ctor) : Attribute
{
    public string CtorSignature { get; init; } = ctor;
}

public class SigScannerWrapper(ISigScanner s) : IDisposable
{
    private readonly Dictionary<string, nint> sigCache = [];
    private readonly Dictionary<string, nint> staticSigCache = [];
    private readonly List<IDisposable> disposableHooks = [];

    public ISigScanner DalamudSigScanner { get; init; } = s;
    public ProcessModule Module => DalamudSigScanner.Module;
    public nint BaseAddress => Module.BaseAddress;
    public nint BaseTextAddress => (nint)(BaseAddress + DalamudSigScanner.TextSectionOffset);
    public nint BaseDataAddress => (nint)(BaseAddress + DalamudSigScanner.DataSectionOffset);
    public nint BaseRDataAddress => (nint)(BaseAddress + DalamudSigScanner.RDataSectionOffset);

    public nint Scan(nint address, int size, string signature)
    {
        var scanCopy = address >= BaseAddress && address < BaseRDataAddress;
        if (scanCopy)
            address = DalamudSigScanner.SearchBase + (address - BaseAddress);
        var ret = SigScanner.Scan(address, size, signature);
        if (scanCopy && ret >= DalamudSigScanner.SearchBase)
            ret = BaseAddress + (ret - DalamudSigScanner.SearchBase);
        return ret;
    }

    public nint Scan(nint address, nint endAddress, string signature) => Scan(address, (int)(endAddress - address), signature);

    public bool TryScan(nint address, int size, string signature, out nint result)
    {
        var scanCopy = address >= BaseAddress && address < BaseRDataAddress;
        if (scanCopy)
            address = DalamudSigScanner.SearchBase + (address - BaseAddress);
        var ret = SigScanner.TryScan(address, size, signature, out result);
        if (scanCopy && result >= DalamudSigScanner.SearchBase)
            result = BaseAddress + (result - DalamudSigScanner.SearchBase);
        return ret;
    }

    public bool TryScan(nint address, nint endAddress, string signature, out nint result) => TryScan(address, (int)(endAddress - address), signature, out result);

    public nint ScanText(string signature)
    {
        if (sigCache.TryGetValue(signature, out var ptr))
            return ptr;

        ptr = DalamudSigScanner.ScanText(signature);
        AddSignatureInfo(signature, ptr, 0, false);
        return ptr;
    }

    public bool TryScanText(string signature, out nint result)
    {
        if (sigCache.TryGetValue(signature, out result))
            return true;

        var b = DalamudSigScanner.TryScanText(signature, out result);
        AddSignatureInfo(signature, result, 0, false);
        return b;
    }

    public nint ScanData(string signature)
    {
        if (sigCache.TryGetValue(signature, out var ptr))
            return ptr;

        ptr = DalamudSigScanner.ScanData(signature);
        AddSignatureInfo(signature, ptr, 0, false);
        return ptr;
    }

    public bool TryScanData(string signature, out nint result)
    {
        if (sigCache.TryGetValue(signature, out result))
            return true;

        var b = DalamudSigScanner.TryScanData(signature, out result);
        AddSignatureInfo(signature, result, 0, false);
        return b;
    }

    public nint ScanModule(string signature)
    {
        if (sigCache.TryGetValue(signature, out var ptr))
            return ptr;

        ptr = DalamudSigScanner.ScanModule(signature);
        AddSignatureInfo(signature, ptr, 0, false);
        return ptr;
    }

    public bool TryScanModule(string signature, out nint result)
    {
        if (sigCache.TryGetValue(signature, out result))
            return true;

        var b = DalamudSigScanner.TryScanModule(signature, out result);
        AddSignatureInfo(signature, result, 0, false);
        return b;
    }

    public nint ScanStaticAddress(string signature, int offset = 0)
    {
        if (offset == 0 && staticSigCache.TryGetValue(signature, out var ptr))
            return ptr;

        ptr = DalamudSigScanner.GetStaticAddressFromSig(signature, offset);
        AddSignatureInfo(signature, ptr, offset, true);
        return ptr;
    }

    public bool TryScanStaticAddress(string signature, out nint result, int offset = 0)
    {
        if (offset == 0 && staticSigCache.TryGetValue(signature, out result))
            return true;

        var b = DalamudSigScanner.TryGetStaticAddressFromSig(signature, out result, offset);
        AddSignatureInfo(signature, result, offset, true);
        return b;
    }

    private Hook<T> HookAddress<T>(nint address, T detour, bool startEnabled = true, bool autoDispose = true, IGameInteropProvider.HookBackend backend = IGameInteropProvider.HookBackend.Automatic) where T : Delegate
    {
        var hook = DalamudApi.GameInteropProvider.HookFromAddress(address, detour, backend);
        AddHook(hook, startEnabled, autoDispose);
        return hook;
    }

    private Hook<T> HookSignature<T>(string signature, T detour, bool scanModule = false, bool startEnabled = true, bool autoDispose = true, IGameInteropProvider.HookBackend backend = IGameInteropProvider.HookBackend.Automatic) where T : Delegate
    {
        var address = !scanModule ? DalamudSigScanner.ScanText(signature) : DalamudSigScanner.ScanModule(signature);
        var hook = DalamudApi.GameInteropProvider.HookFromAddress(address, detour, backend);
        AddSignatureInfo(signature, address, 0, false);
        AddHook(hook, startEnabled, autoDispose);
        return hook;
    }

    private void AddSignatureInfo(string signature, nint ptr, int offset, bool stc)
    {
        if (!stc)
            sigCache[signature] = ptr;
        else
            staticSigCache[signature] = ptr;
    }

    public void InjectSignatures()
    {
        foreach (var (t, _) in Util.Assembly.GetTypesWithAttribute<HypostasisInjectionAttribute>())
            Inject(t);
    }

    public void Inject(Type type, object o = null)
    {
        if (o != null)
            DebugIPC.AddInjectedObject(o);

        foreach (var memberInfo in type.GetAllMembers().Where(memberInfo => memberInfo.MemberType is MemberTypes.Field or MemberTypes.Property))
            InjectMember(o, memberInfo);
    }

    public void Inject(object o) => Inject(o.GetType(), o);

    public void InjectMember(object o, MemberInfo memberInfo)
    {
        var attribute = memberInfo.GetCustomAttribute<HypostasisMemberInjectionAttribute>();
        if (attribute == null) return;

        switch (attribute)
        {
            case HypostasisSignatureInjectionAttribute sigAttribute:
                InjectSignature(o, memberInfo, sigAttribute);
                break;
            case HypostasisClientStructsInjectionAttribute csAttribute:
                InjectClientStructs(o, memberInfo, csAttribute);
                break;
        }
    }

    private void InjectSignature(object o, MemberInfo memberInfo, HypostasisSignatureInjectionAttribute sigAttribute)
    {
        var assignableInfo = new Util.AssignableInfo(o, memberInfo);
        var signature = sigAttribute.Signature;
        var stc = sigAttribute.Static;
        if (!stc ? !DalamudSigScanner.TryScanText(signature, out var address) : !DalamudSigScanner.TryGetStaticAddressFromSig(signature, out address))
        {
            LogInjectError(memberInfo, $"Failed to find signature: \"{signature}\" (Static: {stc})", sigAttribute.Required);
            return;
        }

        InjectAddress(assignableInfo, address, sigAttribute);
    }

    private void InjectClientStructs(object o, MemberInfo memberInfo, HypostasisClientStructsInjectionAttribute csAttribute)
    {
        var memberName = memberInfo.Name.EndsWith("Hook") ? memberInfo.Name.Replace("Hook", string.Empty) : csAttribute.MemberName;
        var csMember = csAttribute.ClientStructsType.GetMember(memberName)[0];
        var assignableInfo = new Util.AssignableInfo(o, memberInfo);
        var retrievedValue = csMember switch
        {
            FieldInfo f => f.GetValue(null),
            PropertyInfo p => p.GetValue(null),
            MethodInfo m => m.Invoke(null, []),
            _ => throw new ApplicationException("Member type is unsupported")
        };

        InjectAddress(assignableInfo, Util.ConvertObjectToIntPtr(retrievedValue), csAttribute);
    }

    private void InjectAddress(Util.AssignableInfo assignableInfo, nint address, HypostasisMemberInjectionAttribute attribute)
    {
        address += attribute.Offset;

        var type = assignableInfo.Type;
        if (type == typeof(nint) || type.IsPointer || type.IsFunctionPointer)
            assignableInfo.SetValue(address);
        else if (type.IsAssignableTo(typeof(Delegate)))
            assignableInfo.SetValue(Marshal.GetDelegateForFunctionPointer(address, type));
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Hook<>))
            InjectHook(assignableInfo, address, attribute);
        else if (type.IsPrimitive)
            assignableInfo.SetValue(Marshal.PtrToStructure(address, type));
        else
            LogInjectError(assignableInfo.MemberInfo, "Failed to determine how to inject member", attribute.Required);
    }

    private void InjectHook(Util.AssignableInfo assignableInfo, nint address, HypostasisMemberInjectionAttribute attribute)
    {
        var ownerType = assignableInfo.MemberInfo.ReflectedType;
        var o = assignableInfo.Object;
        var type = assignableInfo.Type;
        var hookDelegateType = type.GenericTypeArguments[0];

        if (!IsValidHookAddress(address))
        {
            LogInjectError(assignableInfo.MemberInfo, $"Attempted to place hook on invalid location {address:X}", attribute.Required);
            return;
        }

        var detour = GetMethodDelegate(ownerType, hookDelegateType, o, assignableInfo.Name.Replace("Hook", "Detour"));

        if (detour == null)
        {
            var detourName = attribute.DetourName;
            if (detourName != null)
            {
                detour = GetMethodDelegate(ownerType, hookDelegateType, o, detourName);
                if (detour == null)
                {
                    LogInjectError(assignableInfo.MemberInfo, $"Detour not found or was incompatible with delegate \"{detourName}\" {hookDelegateType.Name}", attribute.Required);
                    return;
                }
            }
            else
            {
                var matches = GetMethodDelegates(ownerType, hookDelegateType, o);
                if (matches.Length != 1)
                {
                    LogInjectError(assignableInfo.MemberInfo, $"Found {matches.Length} matching detours: specify a detour name", attribute.Required);
                    return;
                }

                detour = matches[0]!;
            }
        }

        var hook = type.GetMethod("FromAddress", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, [ address, detour, false ]);
        assignableInfo.SetValue(hook);

        if (attribute.EnableHook)
            type.GetMethod("Enable")?.Invoke(hook, null);
        if (attribute.DisposeHook)
            disposableHooks.Add(hook as IDisposable);
    }

    private static Delegate GetMethodDelegate(Type ownerType, Type delegateType, object o, string methodName)
    {
        var detourMethod = ownerType.GetMethod(methodName, Util.AllMembersBindingFlags);
        return CreateDelegate(delegateType, o, detourMethod);
    }

    private static Delegate[] GetMethodDelegates(IReflect ownerType, Type delegateType, object o) => ownerType.GetAllMethods()
        .Select(methodInfo => CreateDelegate(delegateType, o, methodInfo)).Where(del => del != null).ToArray();

    private static Delegate CreateDelegate(Type delegateType, object o, MethodInfo delegateMethod)
    {
        if (delegateType == null) return null;
        return delegateMethod.IsStatic
            ? Delegate.CreateDelegate(delegateType, delegateMethod, false)
            : Delegate.CreateDelegate(delegateType, o, delegateMethod, false);
    }

    public void AddHook<T>(Hook<T> hook, bool enable = true, bool dispose = true) where T : Delegate
    {
        if (enable)
            hook.Enable();
        if (dispose)
            disposableHooks.Add(hook);
    }

    public void InjectMember(Type type, object o, string member) => InjectMember(o, type.GetMember(member, Util.AllMembersBindingFlags)[0]);

    private static void LogInjectError(MemberInfo memberInfo, string message, bool required)
    {
        var errorMsg = $"Error injecting {memberInfo.ReflectedType?.FullName}.{memberInfo.Name}:\n{message}";

        if (required)
            throw new ApplicationException(errorMsg);

        DalamudApi.LogWarning(errorMsg);
    }

    public unsafe bool IsValidHookAddress(nint address) => address == BaseTextAddress || (address > BaseTextAddress && address < BaseRDataAddress && *(byte*)address != 0xCC && *(byte*)(address - 1) == 0xCC);

    public void Dispose()
    {
        foreach (var hook in disposableHooks)
            hook?.Dispose();
        GC.SuppressFinalize(this);
    }
}