///////////////////////////////////////////////////////////////////////////////////////////
// MetaHostStuff.cs : Managed definitions for the Metahost COM Interfaces.               //
// Application      : CLR V4 DST Test Infrastructure                                     //
// Author           : Mithun Shanbhag                                                    //
///////////////////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


namespace MetaHostWrapper
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    public class GUIDS
    {
        public static readonly string strIID_ICLRMetaHost = "D332DB9E-B9B3-4125-8207-A14884F53215";
        public static readonly string strCLSID_CLRMetaHost = "9280188d-0e8e-4867-b30c-7fa83884e8de";
        public static readonly string strIID_ICLRProfiling = "B349ABE3-B56F-4689-BFCD-76BF39D888EA";
        public static readonly string strCLSID_ICLRProfiling = "BD097ED8-733E-43fe-8ED7-A95FF9A8448C";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [
        ComImport, 
        Guid("00000100-0000-0000-C000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    public interface IEnumUnknown
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Next(
            [In] uint celt, 
            [MarshalAs(UnmanagedType.IUnknown)] out object rgelt, 
            uint pceltFetched
        );
        
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Skip(
            [In] uint celt
        );
        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void Reset(
        );
        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void Clone(
            [MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppenum
        );

    }


    ///////////////////////////////////////////////////////////////////////////////////////////////

    [
        ComImport,
        GuidAttribute("D332DB9E-B9B3-4125-8207-A14884F53215"), // IID_ICLRMetaHost
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    public interface ICLRMetaHost
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        IntPtr GetRuntime(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzVersion, 
            [In] ref Guid riid
        );

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetVersionFromFile(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath, 
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, 
            [In, Out] ref uint pcchBuffer
        );
 
        [return: MarshalAs(UnmanagedType.Interface)]        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        IEnumUnknown EnumerateInstalledRuntimes(
        );
 
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        IEnumUnknown EnumerateLoadedRuntimes(
            [In] IntPtr hndProcess
        );
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////

    [
        ComImport,
        GuidAttribute("B349ABE3-B56F-4689-BFCD-76BF39D888EA"), // IID_ICLRProfiling
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    public interface ICLRProfiling
    {
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint AttachProfiler(
            [In] uint dwProfileeProcessID, 
            [In] uint dwMillisecondsMax, 
            [In] ref Guid pClsidProfiler, 
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszProfilerPath, 
            [In] IntPtr pvClientData, 
            [In] uint cbClientData
        );
    }
    
    
    ///////////////////////////////////////////////////////////////////////////////////////////////
        
    [
        ComImport,
        GuidAttribute("BD097ED8-733E-43fe-8ED7-A95FF9A8448C"), // CLSID_CLRProfiling
    ]
    public interface CLRProfiling
    {
        // Please do not touch this!
    }

    
    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    [   ComImport, 
        Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown) 
    ]
    public interface ICLRRuntimeInfo
    {
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetVersionString(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, 
            [In, Out] ref uint pcchBuffer
        );

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetRuntimeDirectory(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, 
            [In, Out] ref uint pcchBuffer
        );

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsLoaded(
            [In] IntPtr hndProcess
        );

        // Methods that may load the runtime:

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), LCIDConversion(3)]
        void LoadErrorString(
            [In] uint iResourceID, 
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer, 
            [In, Out] ref uint pcchBuffer
        );

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        IntPtr LoadLibrary(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzDllName
        );

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        IntPtr GetProcAddress(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszProcName
        );

        [return: MarshalAs(UnmanagedType.IUnknown)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        object GetInterface(
            [In] ref Guid rclsid, 
            [In] ref Guid riid
        );
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

}