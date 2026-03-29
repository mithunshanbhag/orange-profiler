///////////////////////////////////////////////////////////////////////////////
// TraceNavigationCmds.cs : Implements commands to load/unload trace files   //
//                           and navigate through snapshots in the trace.    //
// Application            : CLR V4 Profiler Test Infrastructure              //
// Author                 : Mithun Shanbhag, mithuns@microsoft.com           //
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using OrangeUtil;

namespace OrangeClient
{
    //////////////////////////////////////////////////
    // ----< partial class OrangeCoreExtension >----//
    //////////////////////////////////////////////////

    public sealed partial class OrangeCoreExtension
    {
        private static TraceNavigator traceNavigator = new TraceNavigator();
        private static readonly string xsdSchemaFile = string.Concat(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Path.DirectorySeparatorChar, "orangetracefile.xsd");


        /// <summary>
        /// The "Analyze" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "analyze",
            MinimumAbbrev = 2,
            ShortHelp = @"@todo",
            LongHelp = ""
            )
        ]
        public static void AnalyzeTraceCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (0 == arguments.Length)
                throw new OrangeShellException("Did you pass in location of the trace-file?" + QuickCmdUsageOptions("Analyze"));

            arguments = Environment.ExpandEnvironmentVariables(arguments).ToLower().Trim().StripLeadingLaggingQuotes();

            if (!File.Exists(arguments))
                throw new OrangeShellException("Could not locate the file: " + arguments + ".\r\n" +
                                               "Are you sure you have specified the correct path and filename?");


            if (!traceNavigator.ResetAndLoadTrace(arguments, null/*xsdSchemaFile*/)) //TEMPORARILY COMMENTED OUT BECAUSE OF SCHEMA VALIDATION ISSUES
            {
                throw new OrangeShellException("Schema validation errors detected in trace file: " + arguments + ".\r\n" +
                                               "Are you sure you have specified a valid trace file?");
            }
            Orange.Shell.WriteLine(OutputType.LOG, "Trace file loaded: " + arguments + "\r\n");
            Orange.Shell.WriteLine(OutputType.LOG, traceNavigator.TotalSnapshots + " snapshots detected" + "\r\n");
         }

        ///////////////////////////////////////////////////////////////////////////
        

        /// <summary>
        /// The "Unload" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "unload",
            MinimumAbbrev = 2,
            ShortHelp = @"@todo",
            LongHelp = ""
            )
        ]
        public static void UnloadTraceCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("Currently the quit/exit command does not support arguments." + QuickCmdUsageOptions("Unload"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("Unload"));

            Orange.Shell.WriteLine(OutputType.LOG, "Trace file unloaded: " + traceNavigator.UnloadTrace() + "\r\n");
        }
        
        ///////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "next" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "next",
            MinimumAbbrev = 1,
            ShortHelp = @"@todo",
            LongHelp = ""
            )
        ]
        public static void NextSnapshotCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("Currently the next command does not support arguments." + QuickCmdUsageOptions("Next"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("Next"));

            if (0 == traceNavigator.TotalSnapshots)
                throw new OrangeShellException("No snapshots were detected in this trace.");

            if (null == traceNavigator.NextFullSnapshot()) // we are past the last snapshot. Return to snapshot #0.
                traceNavigator.GotoSnapshot(0);
                
            Orange.Shell.WriteLine(OutputType.LOG, "Snapshot #" + traceNavigator.CurrentSnapshotIndex + "\r\n");
        }

        ///////////////////////////////////////////////////////////////////////////
        
        
        /// <summary>
        /// The "prev" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "prev",
            MinimumAbbrev = 1,
            ShortHelp = @"@todo",
            LongHelp = ""
            )
        ]
        public static void PrevSnapshotCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("Currently the prev command does not support arguments." + QuickCmdUsageOptions("Prev"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("Prev"));

            if (0 == traceNavigator.TotalSnapshots)
                throw new OrangeShellException("No snapshots were detected in this trace.");

            if (traceNavigator.CurrentSnapshotIndex > 0)
                traceNavigator.GotoSnapshot(traceNavigator.CurrentSnapshotIndex - 1);
            else if (traceNavigator.CurrentSnapshotIndex == -1)
                traceNavigator.GotoSnapshot(0);

            Orange.Shell.WriteLine(OutputType.LOG, "Snapshot #" + traceNavigator.CurrentSnapshotIndex + "\r\n");                  
        }
        
        ///////////////////////////////////////////////////////////////////////////
        

        /// <summary>
        /// The "goto" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "goto",
            MinimumAbbrev = 1,
            ShortHelp = @"@todo",
            LongHelp = ""
            )
        ]
        public static void GotoSnapshotCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (0 == arguments.Length)
                throw new OrangeShellException("Did you pass in the index of the snapshot?" + QuickCmdUsageOptions("Goto"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("Goto"));
             
            int index = -1;
            if (false == CheckIfInteger(arguments, out index))
              throw new OrangeShellException("Did you pass in the index of the snapshot?" + QuickCmdUsageOptions("Goto"));
                      
            if (0 == traceNavigator.TotalSnapshots)
                throw new OrangeShellException("No snapshots were detected in this trace.");

            if (index < 0)
                throw new OrangeShellException("Please pass in a valid snapshot index.");
            
            traceNavigator.GotoSnapshot(index);
            
            Orange.Shell.WriteLine(OutputType.LOG, "Snapshot #" + traceNavigator.CurrentSnapshotIndex + "\r\n");                  
        }

        ///////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "out" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "out",
            MinimumAbbrev = 1,
            ShortHelp = @"@todo",
            LongHelp = ""
            )
        ]
        public static void OutOfSnapshotCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("Currently the out command does not support arguments." + QuickCmdUsageOptions("Out"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("Out"));

            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                traceNavigator.ResetAndLoadTrace(traceNavigator.TraceFile, null);
        }
        
        ///////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "where" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "where",
            MinimumAbbrev = 1,
            ShortHelp = @"@todo",
            LongHelp = ""
            )
        ]
        public static void WhichSnapshotCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("Currently the where command does not support arguments." + QuickCmdUsageOptions("Where"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("Where"));                

            Orange.Shell.WriteLine(OutputType.LOG, "Snapshot #" + traceNavigator.CurrentSnapshotIndex + "\r\n");                  
        }
        
    }

    ///////////////////////////////////////////////////////////////////////////

}
