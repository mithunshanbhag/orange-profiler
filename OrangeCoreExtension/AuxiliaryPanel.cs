using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Win32Wrapper;


namespace OrangeClient
{
    public partial class AuxiliaryPanel : Form
    {
        private Process m_proc = null;

        public AuxiliaryPanel(Process p)
        {
            if (null == p)
                throw new ArgumentNullException("p", "Parameter passed in is null");

            m_proc = p;

            InitializeComponent();
        }

        private void SnapshotButton_Click(object sender, EventArgs e)
        {
            Orange.Shell.WriteLine(OutputType.TRACE, "Snapshot requested by user\r\n");

            string forceGCEvent = "Global\\OP_NAMED_EVENT_REQ_FORCEGC";
            IntPtr hEvent = IntPtr.Zero;

            if (IntPtr.Zero == (hEvent = Kernel32.OpenEvent((uint)Win32Wrapper.AccessFlags.EVENT_MODIFY_STATE, false, forceGCEvent)))  
                throw new OrangeShellException("Unable to retrieve the handle for the ForceGC Event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "Successfully retreived handle for ForceGC event.\r\n");

            if (false == Kernel32.SetEvent(hEvent))
                throw new OrangeShellException("Unable to trigger the ForceGC Event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            Orange.Shell.WriteLine(OutputType.TRACE, "Successfully triggered the ForceGC event.\r\n");

            Kernel32.CloseHandle(hEvent);
        }

        private void ProfilerDetachButton_Click(object sender, EventArgs e)
        {
            if (null != m_proc)
                m_proc.DetachProfiler();

            //Orange.Shell.WriteLine(OutputType.TRACE, "Profiler detach requested by user\r\n");

            //string profilerDetachGCEvent = "Global\\OP_NAMED_EVENT_REQ_PROFILERDETACH";
            //IntPtr hEvent = IntPtr.Zero;

            //if (IntPtr.Zero == (hEvent = Kernel32.OpenEvent((uint)Win32Wrapper.AccessFlags.EVENT_MODIFY_STATE, false, profilerDetachGCEvent)))
            //    throw new OrangeShellException("Unable to retrieve the handle for the Profiler-detach Event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            //Orange.Shell.WriteLine(OutputType.TRACE, "Successfully retreived handle for ForceGC event.\r\n");

            //if (false == Kernel32.SetEvent(hEvent))
            //    throw new OrangeShellException("Unable to trigger the ForceGC Event. Error Code= 0x" + Marshal.GetLastWin32Error() + "\r\n");

            //Orange.Shell.WriteLine(OutputType.TRACE, "Successfully triggered the ForceGC event.\r\n");

            //Kernel32.CloseHandle(hEvent);

        }
    }
}
