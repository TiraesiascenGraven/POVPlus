using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Hypostasis;

public static partial class Util
{
    public class AssignableInfo(object o, MemberInfo info)
    {
        public object Object { get; init; } = o;
        public MemberInfo MemberInfo { get; init; } = info;
        private readonly FieldInfo fieldInfo = info as FieldInfo;
        private readonly PropertyInfo propertyInfo = info as PropertyInfo;

        public string Name => MemberInfo.Name;
        public Type Type => MemberInfo.GetObjectType();

        public object GetValue() => fieldInfo?.GetValue(Object) ?? propertyInfo?.GetValue(Object);

        public void SetValue(object v)
        {
            fieldInfo?.SetValue(Object, v);
            propertyInfo?.SetValue(Object, v);
        }
    }

    public const BindingFlags AllMembersBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [LibraryImport("user32.dll")]
    private static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetWindowThreadProcessId(nint handle, out int processId);

    public static bool IsWindowFocused
    {
        get
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == nint.Zero)
                return false;

            var procId = Environment.ProcessId;
            _ = GetWindowThreadProcessId(activatedHandle, out var activeProcId);

            return activeProcId == procId;
        }
    }

    public static bool IsAprilFools => DateTime.Now is { Month: 4, Day: 1 };

    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static Type[] AssemblyTypes => Assembly.GetTypes();

    public static AssemblyName AssemblyName => Assembly.GetName();

    public static IEnumerable<Type> GetTypes<T>(this Assembly assembly) => typeof(T).IsInterface
        ? assembly.GetTypes().Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(T)))
        : assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(T)));

    public static IEnumerable<(Type, T)> GetTypesWithAttribute<T>(this Assembly assembly) where T : Attribute =>
        from t in assembly.GetTypes() let attribute = t.GetCustomAttribute<T>() where attribute != null select (t, attribute);

    public static MemberInfo[] GetAllMembers(this IReflect type) => type.GetMembers(AllMembersBindingFlags);

    public static IEnumerable<(MemberInfo, T)> GetAllMembersWithAttribute<T>(this IReflect type) where T : Attribute =>
        from memberInfo in type.GetAllMembers() let attribute = memberInfo.GetCustomAttribute<T>() where attribute != null select (memberInfo, attribute);

    public static FieldInfo[] GetAllFields(this IReflect type) => type.GetFields(AllMembersBindingFlags);

    public static PropertyInfo[] GetAllProperties(this IReflect type) => type.GetProperties(AllMembersBindingFlags);

    public static MethodInfo[] GetAllMethods(this IReflect type) => type.GetMethods(AllMembersBindingFlags);

    public static bool DeclaresMethod(this Type type, string method, Type[] types) => type.GetMethod(method, AllMembersBindingFlags, types)?.DeclaringType == type;

    public static bool DeclaresMethod(this Type type, string method) => DeclaresMethod(type, method, Type.EmptyTypes);

    public static Type GetObjectType(this MemberInfo memberInfo) => memberInfo switch
    {
        FieldInfo field => field.FieldType,
        PropertyInfo property => property.PropertyType,
        _ => null
    };

    public static bool StartProcess(ProcessStartInfo startInfo)
    {
        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Exception e)
        {
            DalamudApi.LogError("Failed to start process!", e);
            return false;
        }
    }

    public static bool StartProcess(string process, bool admin = false)
    {
        return StartProcess(new ProcessStartInfo
        {
            FileName = process,
            UseShellExecute = true,
            Verb = admin ? "runas" : string.Empty
        });
    }

    public static string CompressString(string s, string prefix = "")
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        using var ms = new MemoryStream();
        using (var gs = new GZipStream(ms, CompressionMode.Compress))
            gs.Write(bytes, 0, bytes.Length);
        return prefix + Convert.ToBase64String(ms.ToArray());
    }

    public static string DecompressString(string s, string prefix = "")
    {
        if (!s.StartsWith(prefix))
            throw new ApplicationException("This export is for a different plugin.");
        var data = Convert.FromBase64String(s[prefix.Length..]);
        using var ms = new MemoryStream(data);
        using var gs = new GZipStream(ms, CompressionMode.Decompress);
        using var r = new StreamReader(gs);
        return r.ReadToEnd();
    }

    public static int ToMilliseconds(this float f) => (int)(f * 1000);

    public static int ToMilliseconds(this double d) => (int)(d * 1000);

    public static unsafe nint ConvertObjectToIntPtr(object o) => o switch
    {
        Pointer p => (nint)Pointer.Unbox(p),
        nint p => p,
        nuint p => (nint)p,
        { } when IsNumeric(o) => (nint)Convert.ToInt64(o),
        _ => nint.Zero
    };

    public static bool IsNumeric(object o) => o switch
    {
        //Int128 => true, UInt128 => true,
        //nint => true, nuint => true,
        long => true, ulong => true,
        int => true, uint => true,
        short => true, ushort => true,
        sbyte => true, byte => true,
        double => true, float => true, decimal => true,
        _ => false
    };

    public static bool IsValidHookAddress(this nint address) => DalamudApi.SigScanner.IsValidHookAddress(address);

    public static unsafe T Deref<T>(this nint address, long offset = 0) where T : unmanaged => *(T*)(address + offset);

    public static string ReadCString(this nint address) => Marshal.PtrToStringUTF8(address);

    public static string ReadCString(this nint address, int len) => Marshal.PtrToStringUTF8(address, len);

    public static void WriteCString(this nint address, string str)
    {
        try
        {
            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                Marshal.WriteByte(address + i, Convert.ToByte(c));
            }
        }
        catch { }

        Marshal.WriteByte(address + str.Length, 0);
    }

    public static object Cast(this Type type, object data)
    {
        var dataParam = Expression.Parameter(typeof(object), "data");
        var body = Expression.Block(Expression.Convert(Expression.Convert(dataParam, data.GetType()), type));
        return Expression.Lambda(body, dataParam).Compile().DynamicInvoke(data);
    }

    public static Vector2 Rotate(this Vector2 v, float a)
    {
        var aCos = (float)Math.Cos(a);
        var aSin = (float)Math.Sin(a);
        return Rotate(v, aCos, aSin);
    }

    public static Vector2 Rotate(this Vector2 v, float aCos, float aSin) => new(v.X * aCos - v.Y * aSin, v.X * aSin + v.Y * aCos);

    public static Vector3 RotateAroundY(this Vector3 v, float a)
    {
        var aCos = (float)Math.Cos(a);
        var aSin = (float)Math.Sin(a);
        return RotateAroundY(v, aCos, aSin);
    }

    public static Vector3 RotateAroundY(this Vector3 v, float aCos, float aSin) => new(v.X * aCos + v.Z * aSin, v.Y, v.Z * aCos - v.X * aSin);

    public static void Shift(this IList list, int i, int amount)
    {
        var count = list.Count;
        if (i < 0 || i >= count) return;

        var item = list[i];
        list.RemoveAt(i);
        list.Insert(Math.Min(Math.Max(i + amount, 0), list.Count), item);
    }

    public static void Shift(this IList list, int i, float amount) => Shift(list, i, (int)amount);

    public static IEnumerable<K> SelectKeys<K, V>(this Dictionary<K, V> dict) => dict.Select(kv => kv.Key);

    public static IEnumerable<V> SelectValues<K, V>(this Dictionary<K, V> dict) => dict.Select(kv => kv.Value);

    public static string GetDisplayName<T>(this T e) where T : struct, Enum
    {
        var name = Enum.GetName(e)!;
        return typeof(T).GetField(name)!.GetCustomAttribute<DisplayAttribute>()?.Name ?? name;
    }
}