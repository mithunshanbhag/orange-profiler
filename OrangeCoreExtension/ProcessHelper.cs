using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using MetaHostWrapper;
using Win32Wrapper;


namespace OrangeClient
{
    ///////////////////////////////////
    //----< class ProcessHelper >----//
    ///////////////////////////////////

    public static class ProcessHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="process"></param>
        public static void WaitForProfilerDetach(this Process process, string strProfDetachedAckEvent)
        {
            if (string.IsNullOrEmpty(strProfDetachedAckEvent))
                throw new ArgumentNullException("strProfDetachedAckEvent", "Cannot pass in NULL or Empty string");


            //Global\\OP_NAMED_EVENT_ACK_PROFILER_DETACHED

            // step 1: obtain handle for profiler-detached acknowledgement event

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to create handle for profiler-detached acknowledgement event...\r\n");

            IntPtr hProfDetachedAckEvent = Kernel32.CreateEvent(IntPtr.Zero, false, false, strProfDetachedAckEvent);

            if (IntPtr.Zero == hProfDetachedAckEvent)
                throw new OrangeShellException("Unable to retrieve the handle for the profiler-detached acknowledgement event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            SafeWaitHandle swhProfDetachedAckEvent = new SafeWaitHandle(hProfDetachedAckEvent, true);

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 2: 

            Orange.Shell.WriteLine(OutputType.TRACE, "Waiting for the profiler-detach acknowledgement event to get signaled...\r\n");

            uint retVal = Kernel32.WaitForSingleObject(hProfDetachedAckEvent, 30 * 1000); // @TODO - Completely arbitrary value. Change this line later.

            if (retVal == (uint)Win32Wrapper.WaitResults.WAIT_FAILED)
                Orange.Shell.WriteLine(OutputType.INTERNAL, "Failed to get acknowlegdement from profiler. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");
            else if (retVal == (uint)Win32Wrapper.WaitResults.WAIT_TIMEOUT)
                    Orange.Shell.WriteLine(OutputType.INTERNAL, "Failed to get acknowlegdement from profiler even after 30 secs. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 5: Close all handles

            Kernel32.CloseHandle(hProfDetachedAckEvent);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="process"></param>
        public static void WaitForProfilerInit(this Process process)
        {
            IntPtr hProfAttachedEvent = IntPtr.Zero;
            bool bHandleObtained = false;
            string strProfAttachedEvent = "Global\\OP_NAMED_EVENT_ACK_PROFILER_INITIALIZED"; // @TODO - change this later. 
            uint retVal = 0;

            // step: 9: Obtaining the handle to the profiler-initilization event.

            Orange.Shell.WriteLine(OutputType.LOG, "Waiting for acknowledgement from profiler...\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to obtain the handle for profiler-initialization event...\r\n");

            for (int i = 0; i < 10 && !bHandleObtained; i++)
            {
                Orange.Shell.WriteLine(OutputType.TRACE, "Attempt #" + i + "\r\n");

                if (IntPtr.Zero == (hProfAttachedEvent = Kernel32.OpenEvent((uint)Win32Wrapper.AccessFlags.EVENT_ALL_ACCESS, false, strProfAttachedEvent)))
                    Thread.Sleep(1000);
                else
                    bHandleObtained = true;
            }

            if (!bHandleObtained)
                throw new OrangeShellException("Unable to retrieve the handle for the profiler-initialization Event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            SafeWaitHandle swhProfAttachedEvent = new SafeWaitHandle(hProfAttachedEvent, true);

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step: 10: Waiting for the handle (of the profiler-initilization event) to get signaled.

            Orange.Shell.WriteLine(OutputType.TRACE, "Waiting for the handle to get signaled...\r\n");

            retVal = Kernel32.WaitForSingleObject(hProfAttachedEvent, 30 * 1000); // @TODO - Completely arbitrary value. Change this line later.

            if (retVal == (uint)Win32Wrapper.WaitResults.WAIT_FAILED)
                throw new OrangeShellException("Failed to get acknowlegdement from profiler. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");
            else if (retVal == (uint)Win32Wrapper.WaitResults.WAIT_TIMEOUT)
                throw new OrangeShellException("Failed to get acknowlegdement from profiler even after 30 secs. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");

            Orange.Shell.WriteLine(OutputType.LOG, "Profiler has successfully attached to process [" + process.Id + "]\r\n");


            // step: 11: Close the handle to the profiler-initilization event.

            Win32Wrapper.Kernel32.CloseHandle(hProfAttachedEvent);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="process"></param>
        public static void DetachProfiler(this Process process)
        {

            IntPtr hProfDetachReqEvent = IntPtr.Zero;
            IntPtr hProfDetachedAckEvent = IntPtr.Zero;

            string strProfDetachReqEvent = "Global\\OP_NAMED_EVENT_REQ_PROFILERDETACH";  // @TODO - change this later. 
            string strProfDetachedAckEvent = "Global\\OP_NAMED_EVENT_ACK_PROFILER_DETACHED"; // @TODO - change this later. 


            // step 1: obtain handle for profiler-detach request event

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to obtain handle for profiler-detach request event...\r\n");

            if (IntPtr.Zero == (hProfDetachReqEvent = Kernel32.OpenEvent((uint)Win32Wrapper.AccessFlags.EVENT_MODIFY_STATE, false, strProfDetachReqEvent)))
                throw new OrangeShellException("Unable to retrieve the handle for profiler-detach request Event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            SafeWaitHandle swhProfDetachReqEvent = new SafeWaitHandle(hProfDetachReqEvent, true);

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 2: obtain handle for profiler-detached acknowledgement event

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to obtain handle for profiler-detached acknowledgement event...\r\n");

            if (IntPtr.Zero == (hProfDetachedAckEvent = Kernel32.OpenEvent((uint)Win32Wrapper.AccessFlags.EVENT_ALL_ACCESS, false, strProfDetachedAckEvent)))
                throw new OrangeShellException("Unable to retrieve the handle for the profiler-detached acknowledgement event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            SafeWaitHandle swhProfDetachedAckEvent = new SafeWaitHandle(hProfDetachedAckEvent, true);

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 3: 

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to trigger the profiler-detach request event.\r\n");

            if (false == Kernel32.SetEvent(hProfDetachReqEvent))
                throw new OrangeShellException("Unable to trigger the profiler-detach request event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 4: 

            Orange.Shell.WriteLine(OutputType.TRACE, "Waiting for the profiler-detach acknowledgement event to get signaled...\r\n");

            uint retVal = Kernel32.WaitForSingleObject(hProfDetachedAckEvent, 30 * 1000); // @TODO - Completely arbitrary value. Change this line later.

            if (retVal == (uint)Win32Wrapper.WaitResults.WAIT_FAILED)
                throw new OrangeShellException("Failed to get acknowlegdement from profiler. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");
            else if (retVal == (uint)Win32Wrapper.WaitResults.WAIT_TIMEOUT)
                throw new OrangeShellException("Failed to get acknowlegdement from profiler even after 30 secs. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 5: Close all handles

            Kernel32.CloseHandle(hProfDetachReqEvent);
            Kernel32.CloseHandle(hProfDetachedAckEvent);
        }


        /// <summary>
        /// This Method does the work of calling into the MetaHost ICLRProfiling::AttachProfiler API.
        /// It is implemented as an extension method on System.Diagnostics.Process.
        /// </summary>
        public static void AttachProfiler(
            this Process process,
            uint dwMillisecondsMax,
            ref Guid pClsidProfiler,
            string profilerPath,
            IntPtr pvClientData,
            uint cbClientData)
        {
            // NOTE: We will not perform pre-condition checks since this is a private API. If you supply garbage, 
            // then you get garbage.

            Guid IID_ICLRMetaHost = new Guid(MetaHostWrapper.GUIDS.strIID_ICLRMetaHost);
            Guid CLSID_CLRMetaHost = new Guid(MetaHostWrapper.GUIDS.strCLSID_CLRMetaHost);
            Guid IID_ICLRProfiling = new Guid(MetaHostWrapper.GUIDS.strIID_ICLRProfiling);
            Guid CLSID_ICLRProfiling = new Guid(MetaHostWrapper.GUIDS.strCLSID_ICLRProfiling);

            Object objCLRMetaHost = null;
            Object objRuntimeInfo = null;
            Object objCLRProfiling = null;
            ICLRMetaHost CLRMetaHost = null;
            IEnumUnknown runTimeEnumeration = null;
            ICLRRuntimeInfo CLRRuntimeInfo = null;
            ICLRProfiling CLRProfiling = null;
            IntPtr hProcess = IntPtr.Zero;

            bool bAttached = false;
            const int MAX_PATH = 256;
            uint retVal = 0;

            // Note: We have put this sleep call to mitigate a race condition issue. We retrieve the MetaHost 
            // interface as soon as the process starts up. The runtime may or may not have initialized yet
            // at this point. If it has, then no problem. If it hasn't then ICLRMetaHost->EnumerateLoadedRuntimes
            // will return the following error code - 
            // 0x8007012B (Only part of a ReadProcessMemory or WriteProcessMemory request was completed.)
            // LadiPro (MetaHost Dev) is currently investigating this issue. For now, the Sleep call will 
            // cause us to wait until the runtime has has a chance to initialize.
            Thread.Sleep(2000); // 2000 mS is an arbitrary choice.


            // step 1: First and foremost we'll retrieve the ICLRMetaHost inteface.

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to retrieve the ICLRMetaHost interface...\r\n");

            if (0 != (retVal = Mscoree.CreateInterface(ref CLSID_CLRMetaHost, ref IID_ICLRMetaHost, out objCLRMetaHost)))
                throw new OrangeShellException("Unable to retrieve ICLRMetaHost interface. Error code = 0x" + Marshal.GetLastWin32Error() + "\r\n");
            if (null == objCLRMetaHost)
                throw new OrangeShellException("Unable to retrieve the ICLRMetaHost interface\r\n");

            CLRMetaHost = (ICLRMetaHost)objCLRMetaHost;
            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 2: retrieve the profilee's process handle

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to retrieve the profilee process handle...\r\n");

            if (IntPtr.Zero == (hProcess = Kernel32.OpenProcess(Win32Wrapper.AccessFlags.PROCESS_ALL_ACCESS, false, (uint)process.Id)))
                throw new OrangeShellException("Unable to retrieve profilee's process handle. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            SafeWaitHandle swhProcess = new SafeWaitHandle(hProcess, true);

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // step 3: Next we have to enumerate all the loaded runtimes in the profilee

            Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to retrieve the IEnumUnknown interface...\r\n");

            if (null == (runTimeEnumeration = CLRMetaHost.EnumerateLoadedRuntimes(hProcess)))
                throw new OrangeShellException("Unable to enumerate loaded runtimes in the profilee process.\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


            // Now for each runtime .............

            while (0 == runTimeEnumeration.Next(1, out objRuntimeInfo, 0) && !bAttached)
            {
                // step 4: retrieve the ICLRRuntimeInfo interface associated with each loaded runtime.

                Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to retrieve the ICLRRuntimeInfo interface...\r\n");

                if (null == objRuntimeInfo)
                    throw new OrangeShellException("Unable to retrieve the ICLRRuntimeInfo interface\r\n");

                CLRRuntimeInfo = (ICLRRuntimeInfo)objRuntimeInfo;
                Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


                // step 5: retrieve the runtime version string

                Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to retrieve the CLR version string...\r\n");

                StringBuilder sb = new StringBuilder(MAX_PATH);
                uint sbCapacity = MAX_PATH;

                if (0 != CLRRuntimeInfo.GetVersionString(sb, ref sbCapacity))
                    throw new OrangeShellException("Unable to retrieve runtime version string\r\n");

                Orange.Shell.WriteLine(OutputType.TRACE, "OK. (CLR version string = " + sb.ToString() + ")\r\n");


                // step 6: Time to attach profiler
                // Note: We need to make a policy decision here. The MetaHost AttachProfiler API will only work 
                // CLR-V4 onwards. Hence we clearly need to skip past any CLR V1, V1.1, v2.0 runtimes. 
                // It is not possible to get multiple V4 runtimes in the same process. So when we detect the first
                // instance of V4, we attach to it.

                if (false == sb.ToString().StartsWith("v4.", StringComparison.InvariantCultureIgnoreCase))
                    continue;


                // step 7: Retrieve the ICLRProfiling Interface

                Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to retrieve the ICLRProfiling interface...\r\n");

                if (null == (objCLRProfiling = CLRRuntimeInfo.GetInterface(ref CLSID_ICLRProfiling, ref IID_ICLRProfiling)))
                    throw new OrangeShellException("Unable to retrieve the ICLRProfilingInterface\r\n");

                CLRProfiling = (ICLRProfiling)objCLRProfiling;
                Orange.Shell.WriteLine(OutputType.TRACE, "OK.\r\n");


                // step 8: Sending the profiler attach request
                // NOTE: The AttachProfiler() API in ICLRProfiling is a misnomer. It should actually be called
                // RequestProfilerAttach().
                // NOTE: The process has just started up and may not have fully initialized by now. So an attempt to
                // attach a profiler might fail (potentially with 0x80070002 - file not found). Hence we shall
                // make two attempts to attach the profiler. On a successful profiler attach, AttachProfiler() returns 
                // zero. In some cases it might return 0x8013136A / CORPROF_E_PROFILER_ALREADY_ACTIVE, which is perfectly 
                // acceptable.

                Orange.Shell.WriteLine(OutputType.TRACE, "Attempting to request a profiler-attach to process [" + process.Id + "]\r\n");

                for (int i = 0; i < 2 && !bAttached; i++)
                {
                    retVal = CLRProfiling.AttachProfiler((uint)process.Id, dwMillisecondsMax, ref pClsidProfiler, profilerPath, pvClientData, (uint)cbClientData);
                    Orange.Shell.WriteLine(OutputType.TRACE, "Attach attempt #" + (i + 1) + ": mscoree!AttachProfiler returned " + retVal.ToString("X") + "\r\n");

                    bAttached = (0 == retVal || 0x8013136A == retVal) ? true : false;
                    Thread.Sleep(1000);
                }

                if (!bAttached)
                    throw new OrangeShellException("Failed to attach profiler to process [" + process.Id + "]\r\n");

                Orange.Shell.WriteLine(OutputType.LOG, "Successfully sent profiler-attach request to process [" + process.Id + "]\r\n");

            }
        }
    }

}
