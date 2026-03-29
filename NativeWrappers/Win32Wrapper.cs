///////////////////////////////////////////////////////////////////////////////////////////
// Win32Wrapper.cs  : PInvoke wrappers for commonly used Win32 Methods.                  //
// Application      : CLR V4 DST Test Infrastructure                                     //
// Author           : Mithun Shanbhag, mithuns@microsoft.com                             //
///////////////////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


namespace Win32Wrapper
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum WinNTDefines : uint
    {
        ANYSIZE_ARRAY                       = 1,
        INFINITE                            = 0xFFFFFFFF
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum SystemErrorCodes
    {
        ERROR_SUCCESS                       = 0x0,
        ERROR_INVALID_FUNCTION              = 0x1,
        ERROR_FILE_NOT_FOUND                = 0x2,
        ERROR_PATH_NOT_FOUND                = 0x3,
        ERROR_TOO_MANY_OPEN_FILES           = 0x4,
        ERROR_ACCESS_DENIED                 = 0x5,
        ERROR_INVALID_HANDLE                = 0x6,
        ERROR_NOT_ALL_ASSIGNED              = 0x514
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum MemoryState
    {
        MEM_COMMIT  = 0x1000,
        MEM_FREE    = 0x10000,
        MEM_RESERVE = 0x2000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum MemoryType
    {
        MEM_IMAGE   = 0x100000,
        MEM_PRIVATE = 0x40000,
        MEM_MAPPED  = 0x20000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum MemoryProtection
    {
        PAGE_NOACCESS           = 0x01,     
        PAGE_READONLY           = 0x02,     
        PAGE_READWRITE          = 0x04,     
        PAGE_WRITECOPY          = 0x08,     
        PAGE_EXECUTE            = 0x10,     
        PAGE_EXECUTE_READ       = 0x20,     
        PAGE_EXECUTE_READWRITE  = 0x40,     
        PAGE_EXECUTE_WRITECOPY  = 0x80,     
        PAGE_GUARD              = 0x100,     
        PAGE_NOCACHE            = 0x200,     
        PAGE_WRITECOMBINE       = 0x400     
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum WaitResults : uint
    {
        WAIT_ABANDONED          = 0x00000080,
        WAIT_OBJECT_0           = 0x00000000,
        WAIT_TIMEOUT            = 0x00000102,
        WAIT_FAILED             = 0xFFFFFFFF
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    [Flags]
    public enum AccessFlags : uint
    {
        /* Predefined standard access types */
        DELETE                              = 0x00010000,
        READ_CONTROL                        = 0x00020000,
        WRITE_DAC                           = 0x00040000,
        WRITE_OWNER                         = 0x00080000,
        SYNCHRONIZE                         = 0x00100000,

        STANDARD_RIGHTS_REQUIRED            = 0x000F0000,
        STANDARD_RIGHTS_READ                = READ_CONTROL,
        STANDARD_RIGHTS_WRITE               = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE             = READ_CONTROL,
        STANDARD_RIGHTS_ALL                 = 0x001F0000,
        SPECIFIC_RIGHTS_ALL                 = 0x0000FFFF,
        
        /* process access flags */
        PROCESS_TERMINATE                   = 0x00000001,
        PROCESS_CREATE_THREAD               = 0x00000002,
        PROCESS_SET_SESSIONID               = 0x00000004,
        PROCESS_VM_OPERATION                = 0x00000008,
        PROCESS_VM_READ                     = 0x00000010,
        PROCESS_VM_WRITE                    = 0x00000020,
        PROCESS_DUP_HANDLE                  = 0x00000040,
        PROCESS_CREATE_PROCESS              = 0x00000080,
        PROCESS_SET_QUOTA                   = 0x00000100,
        PROCESS_SET_INFORMATION             = 0x00000200,
        PROCESS_QUERY_INFORMATION           = 0x00000400,
        PROCESS_SUSPEND_RESUME              = 0x00000800,
        PROCESS_QUERY_LIMITED_INFORMATION   = 0x00001000,
        PROCESS_ALL_ACCESS                  = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF,
        
        /* token access flags */
        TOKEN_ASSIGN_PRIMARY                = 0x00000001,
        TOKEN_DUPLICATE                     = 0x00000002,
        TOKEN_IMPERSONATE                   = 0x00000004,
        TOKEN_QUERY                         = 0x00000008,
        TOKEN_QUERY_SOURCE                  = 0x00000010,
        TOKEN_ADJUST_PRIVILEGES             = 0x00000020,
        TOKEN_ADJUST_GROUPS                 = 0x00000040,
        TOKEN_ADJUST_DEFAULT                = 0x00000080,
        TOKEN_ADJUST_SESSIONID              = 0x00000100,
        TOKEN_ALL_ACCESS                    = STANDARD_RIGHTS_REQUIRED  | 
                                              TOKEN_ASSIGN_PRIMARY      | 
                                              TOKEN_DUPLICATE           |
                                              TOKEN_IMPERSONATE         |
                                              TOKEN_QUERY               |
                                              TOKEN_QUERY_SOURCE        |
                                              TOKEN_ADJUST_PRIVILEGES   |
                                              TOKEN_ADJUST_GROUPS       |
                                              TOKEN_ADJUST_DEFAULT,
        TOKEN_READ                          = STANDARD_RIGHTS_READ | TOKEN_QUERY,
        TOKEN_WRITE                         = STANDARD_RIGHTS_WRITE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT,
        TOKEN_EXECUTE                       = STANDARD_RIGHTS_EXECUTE,

        /* event access flags */
        EVENT_ALL_ACCESS                    = 0x001F0003,
        EVENT_MODIFY_STATE                  = 0x00000002,

        /* mutex access flags */
        MUTEX_ALL_ACCESS                    = 0x001F0001,
        MUTEX_MODIFY_STATE                  = 0x00000001,

        /* semaphore access flags */
        SEMAPHORE_ALL_ACCESS                = 0x001F0003,
        SEMAPHORE_MODIFY_STATE              = 0x00000002,

        /* timer access flags */
        TIMER_ALL_ACCESS                    = 0x001F0003,
        TIMER_MODIFY_STATE                  = 0x00000002,
        TIMER_QUERY_STATE                   = 0x00000001
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    [Flags]
    public enum ContextFlags : uint 
    {
        CLSCTX_INPROC_SERVER          = 0x1, 
        CLSCTX_INPROC_HANDLER         = 0x2, 
        CLSCTX_LOCAL_SERVER           = 0x4, 
        CLSCTX_INPROC_SERVER16        = 0x8,
        CLSCTX_REMOTE_SERVER          = 0x10,
        CLSCTX_INPROC_HANDLER16       = 0x20,
        CLSCTX_RESERVED1              = 0x40,
        CLSCTX_RESERVED2              = 0x80,
        CLSCTX_RESERVED3              = 0x100,
        CLSCTX_RESERVED4              = 0x200,
        CLSCTX_NO_CODE_DOWNLOAD       = 0x400,
        CLSCTX_RESERVED5              = 0x800,
        CLSCTX_NO_CUSTOM_MARSHAL      = 0x1000,
        CLSCTX_ENABLE_CODE_DOWNLOAD   = 0x2000,
        CLSCTX_NO_FAILURE_LOG         = 0x4000,
        CLSCTX_DISABLE_AAA            = 0x8000,
        CLSCTX_ENABLE_AAA             = 0x10000,
        CLSCTX_FROM_DEFAULT_CONTEXT   = 0x20000,
        CLSCTX_ACTIVATE_32_BIT_SERVER = 0x40000,
        CLSCTX_ACTIVATE_64_BIT_SERVER = 0x80000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    public class Privileges
    {
        public const string SE_ASSIGNPRIMARYTOKEN_NAME      = "SeAssignPrimaryTokenPrivilege";
        public const string SE_AUDIT_NAME                   = "SeAuditPrivilege";
        public const string SE_BACKUP_NAME                  = "SeBackupPrivilege";
        public const string SE_CHANGE_NOTIFY_NAME           = "SeChangeNotifyPrivilege";
        public const string SE_CREATE_GLOBAL_NAME           = "SeCreateGlobalPrivilege";
        public const string SE_CREATE_PAGEFILE_NAME         = "SeCreatePagefilePrivilege";
        public const string SE_CREATE_PERMANENT_NAME        = "SeCreatePermanentPrivilege";
        public const string SE_CREATE_SYMBOLIC_LINK_NAME    = "SeCreateSymbolicLinkPrivilege";
        public const string SE_CREATE_TOKEN_NAME            = "SeCreateTokenPrivilege";
        public const string SE_DEBUG_NAME                   = "SeDebugPrivilege";
        public const string SE_ENABLE_DELEGATION_NAME       = "SeEnableDelegationPrivilege";
        public const string SE_IMPERSONATE_NAME             = "SeImpersonatePrivilege";
        public const string SE_INC_BASE_PRIORITY_NAME       = "SeIncreaseBasePriorityPrivilege";
        public const string SE_INCREASE_QUOTA_NAME          = "SeIncreaseQuotaPrivilege";
        public const string SE_INC_WORKING_SET_NAME         = "SeIncreaseWorkingSetPrivilege";
        public const string SE_LOAD_DRIVER_NAME             = "SeLoadDriverPrivilege";
        public const string SE_LOCK_MEMORY_NAME             = "SeLockMemoryPrivilege";
        public const string SE_MACHINE_ACCOUNT_NAME         = "SeMachineAccountPrivilege";
        public const string SE_MANAGE_VOLUME_NAME           = "SeManageVolumePrivilege";
        public const string SE_PROF_SINGLE_PROCESS_NAME     = "SeProfileSingleProcessPrivilege";
        public const string SE_RELABEL_NAME                 = "SeRelabelPrivilege";
        public const string SE_REMOTE_SHUTDOWN_NAME         = "SeRemoteShutdownPrivilege";
        public const string SE_RESTORE_NAME                 = "SeRestorePrivilege";
        public const string SE_SECURITY_NAME                = "SeSecurityPrivilege";
        public const string SE_SHUTDOWN_NAME                = "SeShutdownPrivilege";
        public const string SE_SYNC_AGENT_NAME              = "SeSyncAgentPrivilege";
        public const string SE_SYSTEM_ENVIRONMENT_NAME      = "SeSystemEnvironmentPrivilege";
        public const string SE_SYSTEM_PROFILE_NAME          = "SeSystemProfilePrivilege";
        public const string SE_SYSTEMTIME_NAME              = "SeSystemtimePrivilege";
        public const string SE_TAKE_OWNERSHIP_NAME          = "SeTakeOwnershipPrivilege";
        public const string SE_TCB_NAME                     = "SeTcbPrivilege";
        public const string SE_TIME_ZONE_NAME               = "SeTimeZonePrivilege";
        public const string SE_TRUSTED_CREDMAN_ACCESS_NAME  = "SeTrustedCredManAccessPrivilege";
        public const string SE_UNDOCK_NAME                  = "SeUndockPrivilege";
        public const string SE_UNSOLICITED_INPUT_NAME       = "SeUnsolicitedInputPrivilege";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum PrivilegeAttributes : uint
    {
        SE_PRIVILEGE_ENABLED_BY_DEFAULT   = 0x00000001,        
        SE_PRIVILEGE_ENABLED              = 0x00000002,
        SE_PRIVILEGE_REMOVED              = 0x00000004,
        SE_PRIVILEGE_USED_FOR_ACCESS      = 0x80000000,
        SE_PRIVILEGE_VALID_ATTRIBUTES     = SE_PRIVILEGE_ENABLED_BY_DEFAULT | 
                                            SE_PRIVILEGE_ENABLED            | 
                                            SE_PRIVILEGE_REMOVED            | 
                                            SE_PRIVILEGE_USED_FOR_ACCESS
    } 

    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID 
    {
        public UInt32 LowPart;
        public Int32 HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID_AND_ATTRIBUTES 
    {
        public LUID Luid;
        public PrivilegeAttributes Attributes;
    }

    // Here we have deviated from the structure defined in WinNT.h. This is an alternate simple structure for single privilege setting.
    [StructLayout(LayoutKind.Sequential)]    
    public struct TOKEN_PRIVILEGES 
    {
        public UInt32 PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    public class Kernel32
    {
        private const string dllName = "kernel32.dll";
  
        [DllImport(dllName, SetLastError=true)]
        public static extern IntPtr OpenProcess(
            AccessFlags dwDesiredAccess, 
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            uint dwProcessId
        );

        [DllImport(dllName, SetLastError=true)]
        public static extern IntPtr GetCurrentProcess(
        );

        [DllImport(dllName, SetLastError=true)]
        public static extern void CloseHandle(
            IntPtr handle
        );

        [DllImport(dllName, SetLastError = true)]
        public static extern IntPtr CreateEvent(
            IntPtr lpEventAttributes, 
            bool bManualReset, 
            bool bInitialState, 
            string lpName
        );

        [DllImport(dllName, SetLastError = true)]
        public static extern IntPtr OpenEvent(
            uint dwDesiredAccess, 
            bool bInheritHandle, 
            string lpName
        );

        [DllImport(dllName, SetLastError=true)]
        public static extern bool SetEvent(
            IntPtr hEvent
        );

        [DllImport(dllName, SetLastError = true)]
        public static extern uint WaitForSingleObject(
            IntPtr hHandle, 
            uint dwMilliseconds
        );

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    public class Advapi32
    {
        private const string dllName = "advapi32.dll";

        [DllImport(dllName, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(
            IntPtr hProcess,
            AccessFlags dwDesiredAccess, 
            [Out] out IntPtr hProcessToken
        );

        [DllImport(dllName, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenThreadToken(
            IntPtr hThread,
            AccessFlags dwDesiredAccess, 
            [MarshalAs(UnmanagedType.Bool)] bool bOpenAsSelf,
            [Out] out IntPtr hThreadToken
        );
        
        [DllImport(dllName, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeValue(
            string lpSystemName, 
            string lpPrivilegeName,
            [Out] out LUID lpLuid
        );

        [DllImport(dllName, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle, 
            [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, 
            ref TOKEN_PRIVILEGES NewState, 
            UInt32 BufferLengthInBytes,
            IntPtr/*out TOKEN_PRIVILEGES*/ PreviousState, 
            IntPtr/*out UInt32*/ ReturnLengthInBytes
        );        
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    public class Mscoree
    {
        private const string dllName = "mscoree.dll";

        [DllImport(dllName, SetLastError=true)]
        public static extern uint CreateInterface(
            [In] ref Guid clsid, 
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out object ppIUnk
        );

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    public class Ole32
    {
        private const string dllName = "ole32.dll";
        
        [DllImport(dllName, SetLastError=true)]
        public static extern uint CoInitialize(
            IntPtr pvReserved
        );
        
        [DllImport(dllName, SetLastError=true)]
        public static extern void CoUninitialize(
        );

        [DllImport(dllName, SetLastError=true)]
        public static extern uint CoCreateInstance(
           [In] ref Guid rclsid,
           [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, 
           ContextFlags dwClsContext,
           [In] ref Guid riid,
           [Out, MarshalAs(UnmanagedType.Interface)] out object ppIUnk           
        );


    }
}