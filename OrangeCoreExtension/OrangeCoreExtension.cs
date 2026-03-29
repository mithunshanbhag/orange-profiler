///////////////////////////////////////////////////////////////////////////////////////////////////
// OrangeCoreExtension.cs   : This is the core extension which is always loaded by Orange!       //
//                            when it starts up. It defines some very basic commands like -      //
//                            "help", "load", "mode", "config", "script" etc.                    //
// Application              : CLR V4 Profiler Test Infrastructure                                //
// Author                   : Mithun Shanbhag, mithuns@microsoft.com                             //
///////////////////////////////////////////////////////////////////////////////////////////////////


/*
 * Version History
 * ===============
 *  Version 0.90 - Early Prototype.
 * 
 * Planned Bug-fixes
 * =================
 *  - Nicely format the output of all commands in the core-extension.
 *  - Sometimes it may be valid to have control characters like 'tab' in a orange script.
 *    The script command needs to be modified to handle this.
 *  
 * Planned Modifications
 * =====================
 *  - Have the help command display extension information + commands added by that extension.
 *  - If my current directory is c:\ and I run d:\Orange.exe, then then extpaths gets set to
 *    c:\. It would be a good idea in this case to have extpaths set to "c:\;d:\;"
 *  - Add support for extpaths, extpaths+ inside load command.
 * 
 * Planned Features
 * ================
 *  - N/A
 * 
 */



using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Security.Permissions;
using MetaHostWrapper;
using Win32Wrapper;


namespace OrangeClient
{
    ///////////////////////////////////////////////
    //----< struct AttachingProfilerOptions >----//
    ///////////////////////////////////////////////

    [Serializable()]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct AttachingProfilerOptions
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string wszDiagnosticInfo;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string wszEnableVadump;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string wszEnableObjectLifetimeTracking;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string wszTraceFileName;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string wszCustomSnapshotAction;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string wszSnapshotInterval;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string wszProfilerDetachInterval;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
	    public string wszProfilerInitAckEvent;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
	    public string wszProfilerDetachAckEvent;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////
    //----< class OrangeCoreExtension >----//
    /////////////////////////////////////////

    [ExtensionDescription(AuthorEmail="mithuns@microsoft.com", AuthorName="Mithun Shanbhag")]
    public sealed partial class OrangeCoreExtension
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Type constructor
        /// </summary>
        static OrangeCoreExtension()
        {
            // Do all initialization here (if any)
        }


        /// <summary>
        /// When supplied a string containing the full-path to the executable 
        /// and its arguments, this method separates them both out.
        /// 
        /// E.g. - when the original string looks something like this - 
        ///     "c:\program files\microsoft office\winword.exe" mystuff.doc
        /// The exe's full path will be - c:\program files\microsoft office\winword.exe
        /// The exe's arguments will be - mystuff.doc
        /// </summary>
        /// <param name="arguments">The original string containing both the executable's fullpath and its args.</param>
        /// <param name="strPathToExe">An 'out' param that returns the executable's full path.</param>
        /// <param name="strExeArgs">An 'out' param that returns the executable's arguments.</param>            
        private static void RetrieveExeAndArgs(string arguments, out string strPathToExe, out string strExeArgs)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (0 == arguments.Length)
                throw new OrangeShellException("Could not detect which executable to run!" + QuickCmdUsageOptions("Run"));

            arguments = arguments.ToLower().Trim();
            if (arguments.StartsWith("\""))
            {
                int idx = 1;
                while (arguments[idx] != '\"')
                    idx++;

                strPathToExe = arguments.Substring(0, idx + 1);
                strExeArgs = arguments.Substring(idx + 1).Trim();
            }
            else
            {
                int exe_end_index = (-1 == arguments.IndexOf(' ')) ? arguments.Length - 1 : arguments.IndexOf(' ') - 1;
                strPathToExe = arguments.Substring(0, exe_end_index + 1).Trim();
                strExeArgs = arguments.Substring(exe_end_index + 1).Trim();
            }
        }


        /// <summary>
        /// <summary>        
        private static bool CheckIfInteger(string arguments, out int number)
        {
            // pre-condition checks
            if (null == arguments || 0 == arguments.Length)
                throw new ArgumentNullException("arguments");

            NumberStyles style = NumberStyles.Integer;
            
            // Now to check if we have been supplied a PID. 
            if (arguments.StartsWith("0x", true, CultureInfo.InvariantCulture))
            {
                arguments = arguments.Substring(2);
                style = NumberStyles.HexNumber;
            }

            return (Int32.TryParse(arguments, style, CultureInfo.InvariantCulture, out number)) ? true : false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strProfDetachedAckEvent"></param>
        /// <returns></returns>
        public static IntPtr CreateProfilerDetachAckEvent(string strProfDetachedAckEvent)
        {
            if (string.IsNullOrEmpty(strProfDetachedAckEvent))
                throw new ArgumentNullException("strProfDetachedAckEvent", "Cannot pass in NULL or Empty string");

            // step 1: obtain handle for profiler-detached acknowledgement event

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to create handle for profiler-detached acknowledgement event...\r\n");

            IntPtr hProfDetachedAckEvent = Kernel32.CreateEvent(IntPtr.Zero, false, false, strProfDetachedAckEvent);

            if (IntPtr.Zero == hProfDetachedAckEvent)
                throw new OrangeShellException("Unable to retrieve the handle for the profiler-detached acknowledgement event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");

            return hProfDetachedAckEvent;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public static void WaitForProfilerDetachAckEvent(object t)
        {
            IntPtr hProfilerDetachAckEvent = (t as Tuple<IntPtr, ManualResetEvent>).Item1;
            ManualResetEvent evt = (t as Tuple<IntPtr, ManualResetEvent>).Item2;


            // wait for the detach acknowledgement
            Orange.Shell.WriteLine(OutputType.TRACE, "Waiting for the profiler-detach acknowledgement event to get signaled...\r\n");

            uint retVal = Kernel32.WaitForSingleObject(hProfilerDetachAckEvent, (uint)Win32Wrapper.WinNTDefines.INFINITE);
            if (retVal == (uint)Win32Wrapper.WaitResults.WAIT_FAILED)
                Orange.Shell.WriteLine(OutputType.INTERNAL, "Failed to get acknowlegdement from profiler. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "Received profiler-detach acknowledgement.\r\n");

            // time to exit
            Orange.Shell.WriteLine(OutputType.TRACE, "Setting exit event.\r\n");
            evt.Set();
            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");
        }


        /// <summary>
        /// 
        /// </summary>
        public static void WaitForProcessExit(object t)
        {
            Process proc = (t as Tuple<Process, ManualResetEvent>).Item1;
            ManualResetEvent evt = (t as Tuple<Process, ManualResetEvent>).Item2;

            // waiting for process exit
            Orange.Shell.WriteLine(OutputType.TRACE, "Waiting for Process [" + proc.Id + "] to exit...");
            proc.WaitForExit();
            Orange.Shell.WriteLine(OutputType.TRACE, "Process [" + proc.Id + "] has exited.");

            // time to exit
            Orange.Shell.WriteLine(OutputType.TRACE, "Setting exit event.\r\n");
            evt.Set();
            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");
        }

        /// <summary>
        /// Launches a process with the specified arguments. We are assuming that the
        /// executable's name, path are valid.
        ///
        /// NOTE: Some caveats while setting COR_PROFILER_PATH -
        ///  - It only takes full-path to the profiler DLL (i.e. no relative paths).
        ///  - If there are any env-vars contained in the profiler path, we have to expand it ourselves.
        /// Generally the shell is kind enough to do this on our behalf. But in this case, we are not using
        /// the shell, but calling ProcessStartInfo.EnvironmentVariables.Add(). We need to expand the env-vars
        /// ourselves in this case.
        ///
        /// NOTE:Setting the V2 Compat switch - COMPLUS_PROFAPI_PROFILERCOMPATIBILITYSETTING.
        /// Here is a quick note on how the switch affects V2 and V4 profiler loading behavior
        /// under V4+ runtimes: 
        ///  - EnableV2Profiler: Enables V4 runtime to load V2 profiler. No effect on V4 profilers.
        ///  - DisableV2Profiler: Prevents V4 runtime from loading V2 profiler. This is the default. No effect on V4 profilers.
        ///  - PreventLoad: Prevents V4 runtime from loading both V2 and V4 profilers.
        ///  - env-var not set or set to some random value: Same as DisableV2Profiler. 
        /// Please note that this is not required for attach since V2 profiler are not attach capable. Also
        /// this compat switch has not general effect on V4 profiler startup and attach loading (except if
        /// set to "PreventLoad". Then it prevents loading of both V2 and V4 profilers).
        ///
        /// </summary>
        /// <param name="executable">Executable to be launched.</param>
        /// <param name="args">Arguments to be supplied to the executable. Can be null or empty.</param>
        /// <returns>The process's exit code.</returns>
        public static void LaunchExecutableWithProfiler(string filename, string args)
        {
            // pre-condition check
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Cannot pass in a zero-length string as argument.", "filename");

            Process p = null;
            ManualResetEvent ExitOrDetachEvt = new ManualResetEvent(false);
            IntPtr hDetachAckEvt = IntPtr.Zero;

            //AuxiliaryPanel auxPanel = null;
            //Thread auxThread = null;

            try
            {
                string strFileName = Environment.ExpandEnvironmentVariables(filename);
                string strArgs = (null == args) ? "" : Environment.ExpandEnvironmentVariables(args);

                ProcessStartInfo psi = new ProcessStartInfo(strFileName, strArgs);
                psi.UseShellExecute = false;

                //
                // set profiling API related env-vars.
                //

                // cor_enable_profiling
                Orange.Shell.WriteLine(OutputType.TRACE, "set COR_ENABLE_PROFILING=1" + "\r\n");
                psi.EnvironmentVariables.Add("COR_ENABLE_PROFILING", "1");

                // cor_profiler
                Orange.Shell.WriteLine(OutputType.TRACE, "set COR_PROFILER=" + CONFIG_COR_PROFILER + "\r\n");
                psi.EnvironmentVariables.Add("COR_PROFILER", CONFIG_COR_PROFILER);

                // cor_profiler_path
                if (MODE_RegistryActivation)
                {
                    Orange.Commands.Lookup("run").Execute("regsvr32.exe" + " /s " + CONFIG_COR_PROFILER_PATH); // register the profiler
                }
                else
                {
                    Orange.Shell.WriteLine(OutputType.TRACE, "set COR_PROFILER_PATH=" + CONFIG_COR_PROFILER_PATH + "\r\n");
                    psi.EnvironmentVariables.Add("COR_PROFILER_PATH", CONFIG_COR_PROFILER_PATH);
                }

                // complus_profapi_profilercompatibilitysetting
                Orange.Shell.WriteLine(OutputType.TRACE, "set COMPLUS_PROFAPI_PROFILERCOMPATIBILITYSETTING=" + CONFIG_COMPLUS_PROFAPI_PROFILERCOMPATIBILITYSETTING + "\r\n");
                psi.EnvironmentVariables.Add("COMPLUS_PROFAPI_PROFILERCOMPATIBILITYSETTING", CONFIG_COMPLUS_PROFAPI_PROFILERCOMPATIBILITYSETTING);

                //
                // Set env-vars related to the Orange profiler.
                //

                // op_trace_file_name
                Orange.Shell.WriteLine(OutputType.TRACE, "set OP_TRACE_FILE_NAME=" + CONFIG_OP_TRACE_FILE_NAME + "\r\n");
                psi.EnvironmentVariables.Add("OP_TRACE_FILE_NAME", CONFIG_OP_TRACE_FILE_NAME);

                // op_enable_diagnostic_info
                if (Orange.Shell.Policies.DisplayTraces)
                {
                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_ENABLE_DIAGNOSTIC_INFO=1" + "\r\n");
                    psi.EnvironmentVariables.Add("OP_ENABLE_DIAGNOSTIC_INFO", "1");                    
                }

                // op_enable_vadump
                if (MODE_ENABLE_VADUMP)
                {
                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_ENABLE_VADUMP=1" + "\r\n");
                    psi.EnvironmentVariables.Add("OP_ENABLE_VADUMP", "1");                    
                }

                // op_enable_object_lifetime_tracking
                if (MODE_ENABLE_OBJECT_LIFETIME_TRACKING)
                {
                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_ENABLE_OBJECT_LIFETIME_TRACKING=1" + "\r\n");
                    psi.EnvironmentVariables.Add("OP_ENABLE_OBJECT_LIFETIME_TRACKING", "1");                    
                }

                // op_enable_handle_allocation_callstacks
                if (MODE_ENABLE_HANDLE_ALLOCATION_CALLSTACKS)
                {
                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_ENABLE_HANDLE_ALLOCATION_CALLSTACKS=1" + "\r\n");
                    psi.EnvironmentVariables.Add("OP_ENABLE_HANDLE_ALLOCATION_CALLSTACKS", "1");
                }

                // op_enable_object_allocation_callstacks
                if (MODE_ENABLE_OBJECT_ALLOCATION_CALLSTACKS)
                {
                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_ENABLE_OBJECT_ALLOCATION_CALLSTACKS=1" + "\r\n");
                    psi.EnvironmentVariables.Add("OP_ENABLE_OBJECT_ALLOCATION_CALLSTACKS", "1");
                }    

                // op_custom_snapshot_action 
                if (! string.IsNullOrEmpty(CONFIG_OP_CUSTOM_SNAPSHOT_ACTION))
                {
                    CONFIG_OP_CUSTOM_SNAPSHOT_ACTION = CONFIG_OP_CUSTOM_SNAPSHOT_ACTION.ToLower();

                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_CUSTOM_SNAPSHOT_ACTION=" + CONFIG_OP_CUSTOM_SNAPSHOT_ACTION + "\r\n");
                    psi.EnvironmentVariables.Add("OP_CUSTOM_SNAPSHOT_ACTION", CONFIG_OP_CUSTOM_SNAPSHOT_ACTION);                    
                }

                // op_snapshot_interval
                if (!string.IsNullOrEmpty(CONFIG_OP_SNAPSHOT_INTERVAL))
                {
                    CONFIG_OP_SNAPSHOT_INTERVAL = CONFIG_OP_SNAPSHOT_INTERVAL.ToLower();

                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_SNAPSHOT_INTERVAL=" + CONFIG_OP_SNAPSHOT_INTERVAL + "\r\n");
                    psi.EnvironmentVariables.Add("OP_SNAPSHOT_INTERVAL", CONFIG_OP_SNAPSHOT_INTERVAL);
                }

                // op_detach_interval
                if (!string.IsNullOrEmpty(CONFIG_OP_DETACH_INTERVAL))
                {
                    CONFIG_OP_DETACH_INTERVAL = CONFIG_OP_DETACH_INTERVAL.ToLower();

                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_DETACH_INTERVAL=" + CONFIG_OP_DETACH_INTERVAL + "\r\n");
                    psi.EnvironmentVariables.Add("OP_DETACH_INTERVAL", CONFIG_OP_DETACH_INTERVAL);
                }

                // op_initialization_ack_event
                if (!string.IsNullOrEmpty(CONFIG_OP_INITIALIZATION_ACK_EVENT))
                {
                    CONFIG_OP_INITIALIZATION_ACK_EVENT = CONFIG_OP_INITIALIZATION_ACK_EVENT.ToLower();

                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_INITIALIZATION_ACK_EVENT=" + CONFIG_OP_INITIALIZATION_ACK_EVENT + "\r\n");
                    psi.EnvironmentVariables.Add("OP_INITIALIZATION_ACK_EVENT", CONFIG_OP_CUSTOM_SNAPSHOT_ACTION);
                }

                // op_detach_ack_event
                if (!string.IsNullOrEmpty(CONFIG_OP_DETACH_ACK_EVENT))
                {
                    string evtName = string.Concat("Global\\", CONFIG_OP_DETACH_ACK_EVENT.ToLower());

                    Orange.Shell.WriteLine(OutputType.TRACE, "set OP_DETACH_ACK_EVENT=" + evtName + "\r\n");
                    psi.EnvironmentVariables.Add("OP_DETACH_ACK_EVENT", evtName);

                    // Now we shall spawn a new thread to listen for the detach acknowledgement 

                    hDetachAckEvt = CreateProfilerDetachAckEvent(evtName);

                    Thread threadProcDetachListener = new Thread(WaitForProfilerDetachAckEvent);  
                    threadProcDetachListener.IsBackground = true;
                    threadProcDetachListener.Start(new Tuple<IntPtr, ManualResetEvent>(hDetachAckEvt, ExitOrDetachEvt)); 
                }


                Thread threadProcExitListener = new Thread(WaitForProcessExit);
                threadProcExitListener.IsBackground = true;

                Orange.Shell.WriteLine(OutputType.LOG, "Launching - " + psi.FileName + " " + psi.Arguments + "\r\n");
                p = Process.Start(psi);

                //p.WaitForProfilerInit(); // waiting for an acknowledgement from the profiler after it has initialized itself

                threadProcExitListener.Start(new Tuple<Process, ManualResetEvent>(p, ExitOrDetachEvt));

                // @TODO - temporarily commented out
                //// Start the auxiliary panel now.
                //auxPanel = new AuxiliaryPanel(p);
                //auxThread = new Thread(delegate() { System.Windows.Forms.Application.Run(auxPanel); });
                //auxThread.IsBackground = true;
                //auxThread.Start();


                ExitOrDetachEvt.WaitOne();
            }

            finally
            {
                // @TODO - temporarily commented out
                //if (null != auxPanel)
                //    auxPanel.Close();
                //if (null != auxThread)
                //    auxThread.Join();

                // We shall terminate processes that we launched.
                //if (p != null)
                //    if (!p.HasExited)
                //    {
                //        p.Kill();
                //        p = null;
                //    }

                if (IntPtr.Zero != hDetachAckEvt) 
                {
                    Orange.Shell.WriteLine(OutputType.TRACE, "Closing handle of profiler-detach acknowledgement event...\r\n");

                    Kernel32.CloseHandle(hDetachAckEvt);
                    hDetachAckEvt = IntPtr.Zero;

                    Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");
                }

                // unregister profiler 
                if (MODE_RegistryActivation)
                    Orange.Commands.Lookup("run").Execute("regsvr32.exe" + " /s /u " + CONFIG_COR_PROFILER_PATH);
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public static void AttachProfilerToProcess(Process p)
        {
            if (null == p)
                throw new ArgumentNullException("Null argument passed in", "p");

            ManualResetEvent ExitOrDetachEvt = new ManualResetEvent(false);
            IntPtr hDetachAckEvt = IntPtr.Zero;

            //AuxiliaryPanel auxPanel = null;
            //Thread auxThread = null;

            try
            {
                if (MODE_RegistryActivation)
                    Orange.Commands.Lookup("run").Execute("regsvr32.exe" + " /s " + CONFIG_COR_PROFILER_PATH); // register the profiler

                Guid clsid = new Guid(CONFIG_COR_PROFILER);

                AttachingProfilerOptions opts = new AttachingProfilerOptions();
                opts.wszDiagnosticInfo = (Orange.Shell.Policies.DisplayTraces) ? "1" : "0";
                opts.wszEnableVadump = (MODE_ENABLE_VADUMP) ? "1" : "0";
                opts.wszEnableObjectLifetimeTracking = (MODE_ENABLE_OBJECT_LIFETIME_TRACKING) ? "1" : "0";
                opts.wszTraceFileName = CONFIG_OP_TRACE_FILE_NAME;
                opts.wszCustomSnapshotAction = (!string.IsNullOrEmpty(CONFIG_OP_CUSTOM_SNAPSHOT_ACTION)) ? CONFIG_OP_CUSTOM_SNAPSHOT_ACTION.ToLower() : null;
                opts.wszSnapshotInterval = (!string.IsNullOrEmpty(CONFIG_OP_SNAPSHOT_INTERVAL)) ? CONFIG_OP_SNAPSHOT_INTERVAL.ToLower() : null;
                opts.wszProfilerDetachInterval = (!string.IsNullOrEmpty(CONFIG_OP_DETACH_INTERVAL)) ? CONFIG_OP_DETACH_INTERVAL.ToLower() : null;
                opts.wszProfilerInitAckEvent = (!string.IsNullOrEmpty(CONFIG_OP_INITIALIZATION_ACK_EVENT)) ? CONFIG_OP_INITIALIZATION_ACK_EVENT.ToLower() : null;
                opts.wszProfilerDetachAckEvent = (!string.IsNullOrEmpty(CONFIG_OP_DETACH_ACK_EVENT)) ? CONFIG_OP_DETACH_ACK_EVENT.ToLower() : null;

                // @TODO - the following two options require ELT hooks which are not available on attach?
                // What is the right thing to do? Should we spit out a warning if users set this mode and
                // then attempt to attach a profiler to running process?
                // TEMPORARILY COMMENTED OUT.
                //opts.wszEnableHandleAllocationCallstacks = (MODE_ENABLE_HANDLE_ALLOCATION_CALLSTACKS) ? "1" : "0";
                //opts.wszEnableObjectAllocationCallstacks = (MODE_ENABLE_OBJECT_ALLOCATION_CALLSTACKS) ? "1" : "0";

                // op_detach_ack_event
                if (!string.IsNullOrEmpty(CONFIG_OP_DETACH_ACK_EVENT))
                {
                    string evtName = string.Concat("Global\\", CONFIG_OP_DETACH_ACK_EVENT.ToLower());

                    opts.wszProfilerDetachAckEvent = (!string.IsNullOrEmpty(evtName)) ? evtName : null;

                    // Now we shall spawn a new thread to listen for the detach acknowledgement 

                    hDetachAckEvt = CreateProfilerDetachAckEvent(evtName);

                    Thread threadProcDetachListener = new Thread(WaitForProfilerDetachAckEvent);
                    threadProcDetachListener.IsBackground = true;
                    threadProcDetachListener.Start(new Tuple<IntPtr, ManualResetEvent>(hDetachAckEvt, ExitOrDetachEvt));
                }



                int nSize = Marshal.SizeOf(opts);
                IntPtr handle = Marshal.AllocHGlobal(nSize);
                Marshal.StructureToPtr(opts, handle, false);


                Thread threadProcExitListener = new Thread(WaitForProcessExit);
                threadProcExitListener.IsBackground = true;

                p.AttachProfiler(
                            30000, // a 30 sec timeout is more than reasonable.
                            ref clsid,
                            (MODE_RegistryActivation) ? String.Empty : CONFIG_COR_PROFILER_PATH,
                            handle,
                            (uint)nSize);

                threadProcExitListener.Start(new Tuple<Process, ManualResetEvent>(p, ExitOrDetachEvt));


                //p.WaitForProfilerInit(); // waiting for an acknowledgement from the profiler after it has initialized itself


                // @TODO - temporarily commented out
                //// Start the auxiliary panel now.
                //auxPanel = new AuxiliaryPanel(p);
                //auxThread = new Thread(delegate() { System.Windows.Forms.Application.Run(auxPanel); });
                //auxThread.IsBackground = true;
                //auxThread.Start();

                //p.WaitForExit();
                ExitOrDetachEvt.WaitOne();

                Marshal.FreeHGlobal(handle);
            }

            finally
            {
                // @TODO - temporarily commented out
                //if (null != auxPanel)
                //    auxPanel.Close();
                //if (null != auxThread)
                //    auxThread.Join();

                // unregister profiler 
                if (MODE_RegistryActivation)
                    Orange.Commands.Lookup("run").Execute("regsvr32.exe" + " /s /u " + CONFIG_COR_PROFILER_PATH);
            }
        }



        ////////////////////
        // private fields //
        ////////////////////

        [ConfigItemDescription(
            Name = "COR_PROFILER", IsHidden = true, DefaultValue = @"{D0FC589D-BBEB-4ee2-A198-EDDBDA993DD4}",
            Description = "The Class-ID of the orange profiler component")]
        private static string CONFIG_COR_PROFILER;

        [ConfigItemDescription(
            Name = "COR_PROFILER_PATH", IsHidden = true, DefaultValue = @"orangeprofiler.dll",
            Description = "Full path to the profiler DLL")]
        private static string CONFIG_COR_PROFILER_PATH;

        [ConfigItemDescription(
            Name = "COMPLUS_PROFAPI_PROFILERCOMPATIBILITYSETTING", IsHidden = true, DefaultValue = @"DisableV2Profiler",
            Description = "Compatibility switch for V2 profilers")]
        private static string CONFIG_COMPLUS_PROFAPI_PROFILERCOMPATIBILITYSETTING;

        [ConfigItemDescription(
            Name = "trace_file_name", IsHidden = true, DefaultValue = @"orangetracefile.xml",
            Description = "Use this setting to override the default name of the trace file generated by Orange profiler (tracefile.xml).")]
        private static string CONFIG_OP_TRACE_FILE_NAME;

        [ConfigItemDescription(
            Name = "custom_snapshot_action", IsHidden = false, DefaultValue = "",
            Description = "Informs orange profiler to conduct custom actions while grabbing snapshots.")]
        private static string CONFIG_OP_CUSTOM_SNAPSHOT_ACTION;

        [ConfigItemDescription(
            Name = "initialization_ack_event", IsHidden = true, DefaultValue = "",
            Description = "Specifies the name of the global event that the profiler will signal upon successful initialization.")]
        private static string CONFIG_OP_INITIALIZATION_ACK_EVENT;

        [ConfigItemDescription(
            Name = "detach_ack_event", IsHidden = false, DefaultValue = "",
            Description = "Specifies the name of the global event that the profiler will signal upon successful detach.")]
        private static string CONFIG_OP_DETACH_ACK_EVENT;

        [ConfigItemDescription(
            Name = "snapshot_interval", IsHidden = false, DefaultValue = "",
            Description = "Informs orange profiler to take snapshots at specified intervals (in seconds).")]
        private static string CONFIG_OP_SNAPSHOT_INTERVAL;

        [ConfigItemDescription(
            Name = "detach_interval", IsHidden = false, DefaultValue = "",
            Description = "Informs orange profiler to take request detach at specified intervals (in seconds).")]
        private static string CONFIG_OP_DETACH_INTERVAL;



        [ModeItemDescription(
            Name = "registryactivation", IsHidden = true, DefaultValue = false,
            Description = "uses registry-free profiler activation.")]
        private static bool MODE_RegistryActivation;

        [ModeItemDescription(
            Name = "vadump", IsHidden = false, DefaultValue = false,
            Description = "If set, orange profiler will capture the virtual address information (required by 'vadump' command).")]
        private static bool MODE_ENABLE_VADUMP;

        [ModeItemDescription(
            Name = "object_lifetime_tracking", IsHidden = false, DefaultValue = false,
            Description = "If set, orange profiler will track object movement across generations (required by 'objtrack' command).")]
        private static bool MODE_ENABLE_OBJECT_LIFETIME_TRACKING;

        [ModeItemDescription(
            Name = "handle_allocation_callstacks", IsHidden = false, DefaultValue = false,
            Description = "If set, orange profiler will record callstacks for handle allocations (required by '@todo' command).")]
        private static bool MODE_ENABLE_HANDLE_ALLOCATION_CALLSTACKS;

        [ModeItemDescription(
            Name = "object_allocation_callstacks", IsHidden = false, DefaultValue = false,
            Description = "If set, orange profiler will record callstacks for object allocations (required by '@todo' command).")]
        private static bool MODE_ENABLE_OBJECT_ALLOCATION_CALLSTACKS;
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////

}
