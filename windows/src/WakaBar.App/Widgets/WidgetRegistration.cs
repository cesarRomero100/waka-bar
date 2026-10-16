using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace WakaBar.App.Widgets;

/// <summary>
/// Registers the widget provider with COM, so the Widgets Board can reach it in this process.
/// If WakaBar isn't running, COM starts it (com:ExeServer in Package.appxmanifest) and waits for this
/// registration. Follows "Implement a widget provider in a C# Windows App" on Microsoft Learn.
/// </summary>
internal static class WidgetRegistration
{
    /// <summary>CLSID of the provider: must match com:Class Id and CreateInstance ClassId in Package.appxmanifest.</summary>
    public const string ClassId = "754aec8c-7102-43be-abf4-c1a581be67c7";

    private const uint ClsctxLocalServer = 0x4;
    private const uint RegclsMultipleUse = 0x1;

    private static uint cookie;

    public static void Register(IWidgetProvider provider)
    {
        var result = CoRegisterClassObject(
            Guid.Parse(ClassId),
            new WidgetProviderFactory(provider),
            ClsctxLocalServer,
            RegclsMultipleUse,
            out cookie);
        Marshal.ThrowExceptionForHR(result);
    }

    public static void Revoke()
    {
        if (cookie != 0)
        {
            _ = CoRevokeClassObject(cookie);
            cookie = 0;
        }
    }

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);
}

[ComImport]
[ComVisible(false)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000001-0000-0000-C000-000000000046")]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(nint pUnkOuter, ref Guid riid, out nint ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}

/// <summary>Hands the app's single provider to every COM client.</summary>
[ComVisible(true)]
internal sealed class WidgetProviderFactory(IWidgetProvider provider) : IClassFactory
{
    private const int ClassENoAggregation = unchecked((int)0x80040110);
    private static readonly Guid IUnknown = new("00000000-0000-0000-C000-000000000046");

    public int CreateInstance(nint pUnkOuter, ref Guid riid, out nint ppvObject)
    {
        ppvObject = 0;
        if (pUnkOuter != 0)
        {
            return ClassENoAggregation;
        }

        var instance = MarshalInspectable<IWidgetProvider>.FromManaged(provider);
        if (riid == IUnknown)
        {
            ppvObject = instance;
            return 0;
        }

        // Any other interface (IWidgetProvider itself): let the object answer, then drop our reference.
        var result = Marshal.QueryInterface(instance, in riid, out ppvObject);
        Marshal.Release(instance);
        return result;
    }

    public int LockServer(bool fLock) => 0;
}
