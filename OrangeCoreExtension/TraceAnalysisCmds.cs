///////////////////////////////////////////////////////////////////////////////
// TraceAnalysisCmds.cs : Implements commands to analyze snapshots in        //
//                        a given trace file.                                //
// Application          : CLR V4 Profiler Test Infrastructure                //
// Author               : Mithun Shanbhag                                    //
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic;
//using System.Linq.Parallel;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using Win32Wrapper;
using CorProfWrapper;
using OrangeUtil;

namespace OrangeClient
{
    //////////////////////////////////////////////////
    // ----< partial class OrangeCoreExtension >----//
    //////////////////////////////////////////////////

    public sealed partial class OrangeCoreExtension
    {

        // @TODO - push this into a helper class
        public static string GetMemoryStateString(UInt64 memstate)
        {
            switch(memstate)
            {
                case (UInt64)Win32Wrapper.MemoryState.MEM_COMMIT:   return "committed";
                case (UInt64)Win32Wrapper.MemoryState.MEM_FREE:     return "free";
                case (UInt64)Win32Wrapper.MemoryState.MEM_RESERVE:  return "reserved";
                default:                                            return "unknown";
            }
        }

        // @TODO - push this into a helper class
        public static string GetMemoryTypeString(UInt64 memtype)
        {
            switch(memtype)
            {
                case (UInt64)Win32Wrapper.MemoryType.MEM_IMAGE:     return "image";
                case (UInt64)Win32Wrapper.MemoryType.MEM_MAPPED:    return "mapped";
                case (UInt64)Win32Wrapper.MemoryType.MEM_PRIVATE:   return "private";
                default:                                            return "unknown";
            }
        }


        // @TODO - push this into a helper class
        public static string GetMemoryProtectionString(UInt64 memprot)
        {
            if (0 == memprot)
                return "unknown";

            StringBuilder sb = new  StringBuilder();

            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_EXECUTE))
                sb.Append("E ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_EXECUTE_READ))
                sb.Append("E R ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_EXECUTE_READWRITE))
                sb.Append("E R W ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_EXECUTE_WRITECOPY))
                sb.Append("E WC ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_GUARD))
                sb.Append("G ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_NOACCESS))
                sb.Append("NA ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_NOCACHE))
                sb.Append("NC ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_READONLY))
                sb.Append("RO ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_READWRITE))
                sb.Append("R W ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_WRITECOMBINE))
                sb.Append("WCOM ");
            if (0 != (memprot & (int)Win32Wrapper.MemoryProtection.PAGE_WRITECOPY))
                sb.Append("WC ");

            return sb.ToString();
        }


        // @TODO - push this into a helper class
        public static string GetRootTypeString(COR_PRF_GC_ROOT_KIND rootKind, COR_PRF_GC_ROOT_FLAGS rootFlags)
        {
            switch (rootKind)
            {
                case COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_STACK:
                    return "stack";
                case COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_FINALIZER:
                    return "f-reachable queue";
                case COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_HANDLE:
                    {
                        switch(rootFlags)
                        {
                            case COR_PRF_GC_ROOT_FLAGS.COR_PRF_GC_ROOT_PINNING:
                                return "Handle(pinning)";
                            case COR_PRF_GC_ROOT_FLAGS.COR_PRF_GC_ROOT_WEAKREF:
                                return "Handle(weakref)";
                            case COR_PRF_GC_ROOT_FLAGS.COR_PRF_GC_ROOT_INTERIOR:
                                return "Handle(interior)";
                            case COR_PRF_GC_ROOT_FLAGS.COR_PRF_GC_ROOT_REFCOUNTED:
                            default:
                                return "unknown";
                        }
                    }
                case COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_OTHER:
                default:
                    return "unknown";
            }
 
        }


        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The "gcheap" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "gcheap",
                MinimumAbbrev = 2,
                ShortHelp = @"Parses the xml report generated by the 'mem' command and displays GC generation ranges.",
                LongHelp = ""
            )
        ]
        public static void GCHeapCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("This command does not support arguments." + QuickCmdUsageOptions("gcheap"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("gcheap"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("gcheap"));


            Snapshot snapshot = traceNavigator.CurrentSnapshot;
            var generations = from item in snapshot.Generations
                              select item;

            StringBuilder sb = new StringBuilder();

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4,-20}{5,-24}{6}\r\n", "heap", "gen", "range-start", "range-end", "range-length", "range-length (reserved)", "Fragmentation");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4,-20}{5,-24}{6}\r\n", "====", "===", "===========", "=========", "============", "=======================", "=============");
            }
            else
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-13}{3,-11}{4,-14}{5,-24}{6}\r\n", "heap", "gen", "range-start", "range-end", "range-length", "range-length (reserved)", "Fragmentation");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-13}{3,-11}{4,-14}{5,-24}{6}\r\n", "====", "===", "===========", "=========", "============", "=======================", "=============");
            }

            foreach (var generation in generations)
            {
                if (traceNavigator.Header.Is64BitProcess)
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-18:X8}0x{3,-18:X8}0x{4,-18:X}0x{5,-22:X}~{6}%\r\n", generation.HeapId, generation.GenerationId, generation.RangeStart, generation.RangeStart + generation.RangeLength, generation.RangeLength, generation.RangeLengthReserved, generation.Fragmentation);
                }
                else
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-11:X8}0x{3,-9:X8}0x{4,-12:X}0x{5,-22:X}~{6}%\r\n", generation.HeapId, generation.GenerationId, generation.RangeStart, generation.RangeStart + generation.RangeLength, generation.RangeLength, generation.RangeLengthReserved, generation.Fragmentation);
                }
            }

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////

        # region vadump command

        /// <summary>
        /// The "vadump" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "vadump",
                MinimumAbbrev = 1,
                ShortHelp = @"Parses the xml report generated by the 'mem' command and displays heap objects.",
                LongHelp = ""
            )
        ]
        public static void VadumpCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("This command does not support arguments." + QuickCmdUsageOptions("vadump"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("vadump"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("vadump"));

            var ranges = from item in traceNavigator.CurrentSnapshot.VirtualAddressRanges
                         select item;

            if (0 == ranges.ToList().Count)
            {
                throw new OrangeShellException("Virtual address ranges not detected in the trace file.\r\n" +
                                               "Are you sure you enabled 'mode dump_virtual_address' before profiling?");
            }

            StringBuilder sb = new StringBuilder();

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-20}{1,-20}{2,-12}{3,-12}{4}\r\n", "base", "size", "state", "type", "protection");
                sb.AppendFormat("  {0,-20}{1,-20}{2,-12}{3,-12}{4}\r\n", "====", "====", "=====", "====", "==========");
            }
            else
            {
                sb.AppendFormat("  {0,-12}{1,-12}{2,-12}{3,-12}{4}\r\n", "base", "size", "state", "type", "protection");
                sb.AppendFormat("  {0,-12}{1,-12}{2,-12}{3,-12}{4}\r\n", "====", "====", "=====", "====", "==========");
            }


            foreach (var range in ranges)
            {
                if (traceNavigator.Header.Is64BitProcess)
                    sb.AppendFormat("  0x{0,-18:X8}0x{1,-18:X}{2,-12}{3,-12}{4}\r\n", 
                                       range.BaseAddress, range.RegionSize, GetMemoryStateString(range.MemoryState), GetMemoryTypeString(range.MemoryType), GetMemoryProtectionString(range.MemoryProtection));
                else
                    sb.AppendFormat("  0x{0,-10:X8}0x{1,-10:X}{2,-12}{3,-12}{4}\r\n",
                                       range.BaseAddress, range.RegionSize, GetMemoryStateString(range.MemoryState), GetMemoryTypeString(range.MemoryType), GetMemoryProtectionString(range.MemoryProtection));
            }

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }

        # endregion // vadump command

        ///////////////////////////////////////////////////////////////////////////

        #region objects command

        /// <summary>
        /// The "objects" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "objects",
                MinimumAbbrev = 3,
                ShortHelp = @"Parses the xml report generated by the 'mem' command and displays heap objects.",
                LongHelp = ""
                    + "\r\ndescription                                                                          "
                    + "\r\n===========                                                                          "
                    + "\r\nThis command displays all objects on the GC heap for current snapshot. For each of   "
                    + "\r\nthe objects, the following info is displayed:                                        "
                    + "\r\n- the heap to which the object belongs                                               "
                    + "\r\n- the GC generation that the object lives in                                         "
                    + "\r\n- The object address (ID).                                                           "
                    + "\r\n- The class ID of the object's type.                                                 "
                    + "\r\n- the size of the object.                                                            "
                    + "\r\n                                                                                     "
                    + "\r\nusage                                                                                "
                    + "\r\n=====                                                                                "
                    + "\r\nobjects -id [object-ID] -s [snapshot-index] -so [element] -d -t [number]             "
                    + "\r\n                                                                                     "
                    + "\r\n  -id: [Not yet implemented] This switch is optional. You can supply the ID of a     "
                    + "\r\n       object whose details are to be retrieved. The object ID must be in HEX format "
                    + "\r\n       with the '0x' prefix. If this switch is specified, then the '-so', '-d' and   "
                    + "\r\n       '-t' switches are ignored.                                                    "
                    + "\r\n   -s: This switch is optional. By default, this command is run on the currently     "
                    + "\r\n       loaded snapshot. However you can identify a specific snapshot to run this     "
                    + "\r\n       command on. All snapshots are uniquely identified by their index.             "
                    + "\r\n  -so: Sort on specified element This switch is optional. By default, the output is  "
                    + "\r\n       sorted on heap IDs.                                                           "
                    + "\r\n  [element]                                                                          "
                    + "\r\n        id: sort on object IDs.                                                      "
                    + "\r\n     class: sort on class IDs.                                                       "
                    + "\r\n      heap: sort on heap ID (default).                                               "
                    + "\r\n       gen: sort on object generation.                                               "
                    + "\r\n      size: sort on object size of .                                                 "
                    + "\r\n   -d: Conducts a descending sort. This switch is optional. Ascending sort is the    "
                    + "\r\n       default, if this switch is not specified.                                     "
                    + "\r\n   -t: Truncates output to desired number of elements. This switch is optional.      "
                    + "\r\n                                                                                     "
                    + "\r\nexamples                                                                             "
                    + "\r\n========                                                                             "
                    + "\r\nobjects              : Displays all GC heap objects for current snapshot.            "
                    + "\r\nobjects -id 0x263200 : Displays detailed info for object with object-ID = 0x263200.  "
                    + "\r\nobjects -s 5 -t 10   : Runs command on snapshot # 5 and truncates output to 10       "
                    + "\r\n                       entries.                                                      "
                    + "\r\nobjects -so size -d  : Runs command and sorts output by object size (in desending    "
                    + "\r\n                       order).                                                       "
                    + "\r\n"
            )
        ]
        public static void ObjectsCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("objects"));
            if (0 == traceNavigator.TotalSnapshots)
                throw new OrangeShellException("This trace file does not have any snapshots." + QuickCmdUsageOptions("objects"));

            // we'll need to parse the arguments
            const string option_snapshot_index = "s";
            const string option_sort_output = "so";
            const string option_descending_sort = "d";
            const string option_truncate_output = "t";

            Snapshot snapshot = null;
            int truncateTo = -1;
            string sortOn = "HeapId, GenerationId, ObjectId"; // default sort order
            string filter = string.Empty;

            OrangeArgParser ap = new OrangeArgParser(
                arguments.Trim(),
                string.Concat(option_snapshot_index, ":1", ";", option_sort_output, ":1", ";", option_descending_sort, ";", option_truncate_output, ":1")
                );


            // which snapshot should we use?
            if (ap.OptionPassed(option_snapshot_index))
            {
                int index = ap.GetOption(option_snapshot_index).AsInt;

                if (index < 0 || index >= traceNavigator.TotalSnapshots)
                    throw new OrangeShellException("An invalid snapshot has been specified: " + index + "." + QuickCmdUsageOptions("objects"));

                snapshot = traceNavigator.GotoSnapshot(index);
            }
            else
            {
                if (traceNavigator.CurrentSnapshotIndex < 0)
                    throw new OrangeShellException("No snapshot is currently load. Please use next, prev or goto commands." + QuickCmdUsageOptions("objects"));

                snapshot = traceNavigator.CurrentSnapshot;
            }

            // should the output be truncated?
            if (ap.OptionPassed(option_truncate_output))
                truncateTo = ap.GetOption(option_truncate_output).AsInt;

            // what element should the output be sorted on?
            if (ap.OptionPassed(option_sort_output))
            {
                sortOn = ap.GetOption(option_sort_output).AsString;

                if (sortOn.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ObjectId";
                else if (sortOn.Equals("class", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ClassId";
                else if (sortOn.Equals("heap", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "HeapId";
                else if (sortOn.Equals("gen", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "GenerationId";
                else if (sortOn.Equals("size", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "AlignedSize";
                else
                    throw new OrangeShellException("Invalid sort element specified." + QuickCmdUsageOptions("objects"));

                // should the output be sorted in descending order?
                if (ap.OptionPassed(option_descending_sort))
                    sortOn += " descending";
            }


            StringBuilder sb = new StringBuilder();

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4}\r\n", "heap", "gen", "address", "MT", "size");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4}\r\n", "====", "===", "=======", "==", "====");
            }
            else
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-12}{3,-12}{4}\r\n", "heap", "gen", "address", "MT", "size");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-12}{3,-12}{4}\r\n", "====", "===", "=======", "==", "====");
            }

            // retrieve the objects on the heap
            var validHeapObjs = snapshot.Objects.AsQueryable<ObjectInfo>();

            if (!string.IsNullOrEmpty(filter))
                validHeapObjs = validHeapObjs.Where(filter, null);

            if (!string.IsNullOrEmpty(sortOn))
                validHeapObjs = validHeapObjs.OrderBy(sortOn);

            if (-1 != truncateTo)
                validHeapObjs = validHeapObjs.Take(truncateTo);

            foreach (var heapObj in validHeapObjs)
            {
                if (traceNavigator.Header.Is64BitProcess)
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-18:X16}0x{3,-18:X16}0x{4:X}\r\n", heapObj.HeapId, heapObj.GenerationId, heapObj.ObjectId, heapObj.ClassId, heapObj.AlignedSize);
                }
                else
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-10:X8}0x{3,-10:X8}0x{4:X}\r\n", heapObj.HeapId, heapObj.GenerationId, heapObj.ObjectId, heapObj.ClassId, heapObj.AlignedSize); 
                }
            }

            sb.AppendFormat("\r\nTotal {0} objects. \r\n", validHeapObjs.Count());

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }



        /// <summary>
        /// The "PinnedObjects" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "pinnedobjects",
                MinimumAbbrev = 3,
                ShortHelp = @"Parses the xml report generated by the 'mem' command and displays pinned objects.",
                LongHelp = ""
                    + "\r\ndescription                                                                          "
                    + "\r\n===========                                                                          "
                    + "\r\nThis command displays all pinned objects on the GC heap for current snapshot. For    "
                    + "\r\n each of the pinned objects, the following info is displayed:                        "
                    + "\r\n- the heap to which the object belongs                                               "
                    + "\r\n- the GC generation that the object lives in                                         "
                    + "\r\n- The object address (ID).                                                           "
                    + "\r\n- The class ID of the object's type.                                                 "
                    + "\r\n- the size of the object.                                                            "
                    + "\r\n                                                                                     "
                    + "\r\nusage                                                                                "
                    + "\r\n=====                                                                                "
                    + "\r\nobjects -id [object-ID] -s [snapshot-index] -so [element] -d -t [number]             "
                    + "\r\n                                                                                     "
                    + "\r\n  -id: [Not yet implemented] This switch is optional. You can supply the ID of a     "
                    + "\r\n       object whose details are to be retrieved. The object ID must be in HEX format "
                    + "\r\n       with the '0x' prefix. If this switch is specified, then the '-so', '-d' and   "
                    + "\r\n       '-t' switches are ignored.                                                    "
                    + "\r\n   -s: This switch is optional. By default, this command is run on the currently     "
                    + "\r\n       loaded snapshot. However you can identify a specific snapshot to run this     "
                    + "\r\n       command on. All snapshots are uniquely identified by their index.             "
                    + "\r\n  -so: Sort on specified element This switch is optional. By default, the output is  "
                    + "\r\n       sorted on heap IDs.                                                           "
                    + "\r\n  [element]                                                                          "
                    + "\r\n        id: sort on object IDs.                                                      "
                    + "\r\n     class: sort on class IDs.                                                       "
                    + "\r\n      heap: sort on heap ID (default).                                               "
                    + "\r\n       gen: sort on object generation.                                               "
                    + "\r\n      size: sort on object size of .                                                 "
                    + "\r\n   -d: Conducts a descending sort. This switch is optional. Ascending sort is the    "
                    + "\r\n       default, if this switch is not specified.                                     "
                    + "\r\n   -t: Truncates output to desired number of elements. This switch is optional.      "
                    + "\r\n                                                                                     "
                    + "\r\nexamples                                                                             "
                    + "\r\n========                                                                             "
                    + "\r\npin              : Displays all pinned GC heap objects for current snapshot.         "
                    + "\r\npin -id 0x263200 : Displays detailed info for object with object-ID = 0x263200.      "
                    + "\r\npin -s 5 -t 10   : Runs command on snapshot # 5 and truncates output to 10           "
                    + "\r\n                   entries.                                                          "
                    + "\r\npin -so size -d  : Runs command and sorts output by object size (in desending        "
                    + "\r\n                   order).                                                           "
                    + "\r\n"
            )
        ]
        public static void PinnedObjectsCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("pinnedobjects"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("pinnedObjects"));


            // we'll need to parse the arguments
            const string option_snapshot_index = "s";
            const string option_sort_output = "so";
            const string option_descending_sort = "d";
            const string option_truncate_output = "t";

            Snapshot snapshot = null;
            int truncateTo = -1;
            string sortOn = "HeapId, GenerationId, ObjectId"; // default sort order
            string filter = string.Empty;

            OrangeArgParser ap = new OrangeArgParser(
                arguments.Trim(),
                string.Concat(option_snapshot_index, ":1", ";", option_sort_output, ":1", ";", option_descending_sort, ";", option_truncate_output, ":1")
                );


            // which snapshot should we use?
            if (ap.OptionPassed(option_snapshot_index))
            {
                int index = ap.GetOption(option_snapshot_index).AsInt;

                if (index < 0 || index >= traceNavigator.TotalSnapshots)
                    throw new OrangeShellException("An invalid snapshot has been specified: " + index + "." + QuickCmdUsageOptions("pinnedobjects"));

                snapshot = traceNavigator.GotoSnapshot(index);
            }
            else
            {
                if (traceNavigator.CurrentSnapshotIndex < 0)
                    throw new OrangeShellException("No snapshot is currently load. Please use next, prev or goto commands." + QuickCmdUsageOptions("pinnedobjects"));

                snapshot = traceNavigator.CurrentSnapshot;
            }

            // should the output be truncated?
            if (ap.OptionPassed(option_truncate_output))
                truncateTo = ap.GetOption(option_truncate_output).AsInt;

            // what element should the output be sorted on?
            if (ap.OptionPassed(option_sort_output))
            {
                sortOn = ap.GetOption(option_sort_output).AsString;

                if (sortOn.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ObjectId";
                else if (sortOn.Equals("class", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ClassId";
                else if (sortOn.Equals("heap", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "HeapId";
                else if (sortOn.Equals("gen", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "GenerationId";
                else if (sortOn.Equals("size", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "AlignedSize";
                else
                    throw new OrangeShellException("Invalid sort element specified." + QuickCmdUsageOptions("pinnedobjects"));

                // should the output be sorted in descending order?
                if (ap.OptionPassed(option_descending_sort))
                    sortOn += " descending";
            }


            StringBuilder sb = new StringBuilder();

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4}\r\n", "heap", "gen", "address", "MT", "size");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4}\r\n", "====", "===", "=======", "==", "====");
            }
            else
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-12}{3,-12}{4}\r\n", "heap", "gen", "address", "MT", "size");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-12}{3,-12}{4}\r\n", "====", "===", "=======", "==", "====");
            }

            // retrieve all the pinned objects on the heap
            var pinnedObjs = snapshot.PinnedObjects.AsQueryable<ObjectInfo>();

            if (!string.IsNullOrEmpty(filter))
                pinnedObjs = pinnedObjs.Where(filter, null);

            if (!string.IsNullOrEmpty(sortOn))
                pinnedObjs = pinnedObjs.OrderBy(sortOn);

            if (-1 != truncateTo)
                pinnedObjs = pinnedObjs.Take(truncateTo);

            foreach (var heapObj in pinnedObjs)
            {
                if (traceNavigator.Header.Is64BitProcess)
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-18:X16}0x{3,-18:X16}0x{4:X}\r\n", heapObj.HeapId, heapObj.GenerationId, heapObj.ObjectId, heapObj.ClassId, heapObj.AlignedSize);
                }
                else
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-10:X8}0x{3,-10:X8}0x{4:X}\r\n", heapObj.HeapId, heapObj.GenerationId, heapObj.ObjectId, heapObj.ClassId, heapObj.AlignedSize);
                }
            }

            sb.AppendFormat("\r\nTotal {0} pinned objects. \r\n", pinnedObjs.Count());

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }



        /// <summary>
        /// The "FReachableObjects" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "freachableobjects",
                MinimumAbbrev = 6,
                ShortHelp = @"Parses the xml report generated by the 'mem' command and displays objects rooted by f-reachable queue.",
                LongHelp = ""
                    + "\r\ndescription                                                                          "
                    + "\r\n===========                                                                          "
                    + "\r\nThis command displays all objects rooted by f-reachable queue (for current snapshot)."
                    + "\r\n For each of the objects, the following info is displayed:                           "
                    + "\r\n- the heap to which the object belongs                                               "
                    + "\r\n- the GC generation that the object lives in                                         "
                    + "\r\n- The object address (ID).                                                           "
                    + "\r\n- The class ID of the object's type.                                                 "
                    + "\r\n- the size of the object.                                                            "
                    + "\r\n                                                                                     "
                    + "\r\nusage                                                                                "
                    + "\r\n=====                                                                                "
                    + "\r\nobjects -id [object-ID] -s [snapshot-index] -so [element] -d -t [number]             "
                    + "\r\n                                                                                     "
                    + "\r\n  -id: [Not yet implemented] This switch is optional. You can supply the ID of a     "
                    + "\r\n       object whose details are to be retrieved. The object ID must be in HEX format "
                    + "\r\n       with the '0x' prefix. If this switch is specified, then the '-so', '-d' and   "
                    + "\r\n       '-t' switches are ignored.                                                    "
                    + "\r\n   -s: This switch is optional. By default, this command is run on the currently     "
                    + "\r\n       loaded snapshot. However you can identify a specific snapshot to run this     "
                    + "\r\n       command on. All snapshots are uniquely identified by their index.             "
                    + "\r\n  -so: Sort on specified element This switch is optional. By default, the output is  "
                    + "\r\n       sorted on heap IDs.                                                           "
                    + "\r\n  [element]                                                                          "
                    + "\r\n        id: sort on object IDs.                                                      "
                    + "\r\n     class: sort on class IDs.                                                       "
                    + "\r\n      heap: sort on heap ID (default).                                               "
                    + "\r\n       gen: sort on object generation.                                               "
                    + "\r\n      size: sort on object size of .                                                 "
                    + "\r\n   -d: Conducts a descending sort. This switch is optional. Ascending sort is the    "
                    + "\r\n       default, if this switch is not specified.                                     "
                    + "\r\n   -t: Truncates output to desired number of elements. This switch is optional.      "
                    + "\r\n                                                                                     "
                    + "\r\nexamples                                                                             "
                    + "\r\n========                                                                             "
                    + "\r\npin              : Displays all objects on f-reachable queue for current snapshot.   "
                    + "\r\npin -id 0x263200 : Displays detailed info for object with object-ID = 0x263200.      "
                    + "\r\npin -s 5 -t 10   : Runs command on snapshot # 5 and truncates output to 10           "
                    + "\r\n                   entries.                                                          "
                    + "\r\npin -so size -d  : Runs command and sorts output by object size (in desending        "
                    + "\r\n                   order).                                                           "
                    + "\r\n"
            )
        ]
        public static void FReachableObjectsCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("freachableobjects"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("freachableobjects"));


            // we'll need to parse the arguments
            const string option_snapshot_index = "s";
            const string option_sort_output = "so";
            const string option_descending_sort = "d";
            const string option_truncate_output = "t";

            Snapshot snapshot = null;
            int truncateTo = -1;
            string sortOn = "HeapId, GenerationId, ObjectId"; // default sort order
            string filter = string.Empty;

            OrangeArgParser ap = new OrangeArgParser(
                arguments.Trim(),
                string.Concat(option_snapshot_index, ":1", ";", option_sort_output, ":1", ";", option_descending_sort, ";", option_truncate_output, ":1")
                );


            // which snapshot should we use?
            if (ap.OptionPassed(option_snapshot_index))
            {
                int index = ap.GetOption(option_snapshot_index).AsInt;

                if (index < 0 || index >= traceNavigator.TotalSnapshots)
                    throw new OrangeShellException("An invalid snapshot has been specified: " + index + "." + QuickCmdUsageOptions("freachableobjects"));

                snapshot = traceNavigator.GotoSnapshot(index);
            }
            else
            {
                if (traceNavigator.CurrentSnapshotIndex < 0)
                    throw new OrangeShellException("No snapshot is currently load. Please use next, prev or goto commands." + QuickCmdUsageOptions("freachableobjects"));

                snapshot = traceNavigator.CurrentSnapshot;
            }

            // should the output be truncated?
            if (ap.OptionPassed(option_truncate_output))
                truncateTo = ap.GetOption(option_truncate_output).AsInt;

            // what element should the output be sorted on?
            if (ap.OptionPassed(option_sort_output))
            {
                sortOn = ap.GetOption(option_sort_output).AsString;

                if (sortOn.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ObjectId";
                else if (sortOn.Equals("class", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ClassId";
                else if (sortOn.Equals("heap", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "HeapId";
                else if (sortOn.Equals("gen", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "GenerationId";
                else if (sortOn.Equals("size", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "AlignedSize";
                else
                    throw new OrangeShellException("Invalid sort element specified." + QuickCmdUsageOptions("pinnedobjects"));

                // should the output be sorted in descending order?
                if (ap.OptionPassed(option_descending_sort))
                    sortOn += " descending";
            }


            StringBuilder sb = new StringBuilder();

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4}\r\n", "heap", "gen", "address", "MT", "size");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-20}{3,-20}{4}\r\n", "====", "===", "=======", "==", "====");
            }
            else
            {
                sb.AppendFormat("  {0,-6}{1,-5}{2,-12}{3,-12}{4}\r\n", "heap", "gen", "address", "MT", "size");
                sb.AppendFormat("  {0,-6}{1,-5}{2,-12}{3,-12}{4}\r\n", "====", "===", "=======", "==", "====");
            }

            // retrieve all the f-reachable objects on the heap
            var freachableObjs = snapshot.FReachableObjects.AsQueryable<ObjectInfo>();

            if (!string.IsNullOrEmpty(filter))
                freachableObjs = freachableObjs.Where(filter, null);

            if (!string.IsNullOrEmpty(sortOn))
                freachableObjs = freachableObjs.OrderBy(sortOn);

            if (-1 != truncateTo)
                freachableObjs = freachableObjs.Take(truncateTo);

            foreach (var heapObj in freachableObjs)
            {
                if (traceNavigator.Header.Is64BitProcess)
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-18:X16}0x{3,-18:X16}0x{4:X}\r\n", heapObj.HeapId, heapObj.GenerationId, heapObj.ObjectId, heapObj.ClassId, heapObj.AlignedSize);
                }
                else
                {
                    sb.AppendFormat("  {0,-6}{1,-5}0x{2,-10:X8}0x{3,-10:X8}0x{4:X}\r\n", heapObj.HeapId, heapObj.GenerationId, heapObj.ObjectId, heapObj.ClassId, heapObj.AlignedSize);
                }
            }

            sb.AppendFormat("\r\nTotal {0} f-reachable objects. \r\n", freachableObjs.Count());

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }


        #endregion // objects command

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The "Classes" command.
        /// Note: This command needs to be modified a bit. If a snapshot is specifed
        /// with the "-i" option, then that snapshot is loaded and the command is 
        /// executed. However at the end, the snapshot is never unloaded.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "classes",
                MinimumAbbrev = 1,
                ShortHelp = @"displays loaded types/classes in a snapshot.",
                LongHelp = ""
                    + "\r\ndescription                                                                          "
                    + "\r\n===========                                                                          "
                    + "\r\nThis command displays the following info for each of the loaded types/classes in     "
                    + "\r\nthe current snapshot:                                                                "
                    + "\r\n- The class ID.                                                                      "
                    + "\r\n- The name of the class.                                                             "
                    + "\r\n- total number of live instances (objects) of each type.                             "
                    + "\r\n- total bytes allocated (on GC heap) for all the objects of each type.               "
                    + "\r\n- whether or not the class is finalizable.                                           "
                    + "\r\n                                                                                     "
                    + "\r\nusage                                                                                "
                    + "\r\n=====                                                                                "
                    + "\r\nclasses -id [class-ID] -s [snapshot-index] -so [element] -d -t [number]              "
                    + "\r\n                                                                                     "
                    + "\r\n  -id: [Not yet implemented] This switch is optional. You can supply the ID of a     "
                    + "\r\n        class whose details will be retrieved. The class ID must be in HEX format    "
                    + "\r\n        with the '0x' prefix. If this switch is specified, then the '-so', '-d' and  "    
                    + "\r\n        '-t' switches are ignored.                                                   "
                    + "\r\n   -s: This switch is optional. By default, this command is run on the currently     "
                    + "\r\n       loaded snapshot. However you can identify a specific snapshot to run this     "
                    + "\r\n       command on. All snapshots are uniquely identified by their index.             "
                    + "\r\n  -so: Sort on specified element This switch is optional. By default, the output is  "
                    + "\r\n       sorted on class IDs.                                                          "
                    + "\r\n  [element]                                                                          "
                    + "\r\n           id: sort on class IDs (default).                                          "
                    + "\r\n        bytes: sort on total bytes.                                                  "
                    + "\r\n    instances: sort on number of live instances.                                     "
                    + "\r\n         name: sort on name of class.                                                "
                    + "\r\n   -d: Conducts a descending sort. This switch is optional. Ascending sort is the    "
                    + "\r\n       default, if this switch is not specified.                                     "
                    + "\r\n   -t: Truncates output to desired number of elements. This switch is optional.      "
                    + "\r\n                                                                                     "
                    + "\r\nexamples                                                                             "
                    + "\r\n========                                                                             "
                    + "\r\nclasses              : Displays all loaded types in current snapshot.                "
                    + "\r\nclasses -id 0x263200 : Displays detailed info for type with class-ID 0x263200.       "
                    + "\r\nclasses -s 5 -t 10   : Runs command on snapshot # 5 and truncates output to 10       "
                    + "\r\n                       entries.                                                      "
                    + "\r\nclasses -so bytes -d : Runs command and sorts output by bytes allocated for each     "
                    + "\r\n                       type in desending order.                                      "
                    + "\r\n"
            )
        ]
        public static void ClassesCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("classes"));
            if (0 == traceNavigator.TotalSnapshots)
                throw new OrangeShellException("This trace file does not have any snapshots." + QuickCmdUsageOptions("classes"));

            // we'll need to parse the arguments
            const string option_snapshot_index = "s";
            const string option_sort_output = "so";
            const string option_descending_sort = "d";
            const string option_truncate_output = "t";

            Snapshot snapshot = null;
            int truncateTo = -1;
            string sortOn = "ClassId";
            string filter = string.Empty;
       
            OrangeArgParser ap = new OrangeArgParser(
                arguments.Trim(),
                string.Concat(option_snapshot_index, ":1", ";", option_sort_output, ":1", ";", option_descending_sort, ";", option_truncate_output, ":1")
                );


            // which snapshot should we use?
            if (ap.OptionPassed(option_snapshot_index))
            {
                int index = ap.GetOption(option_snapshot_index).AsInt;

                if (index < 0 || index >= traceNavigator.TotalSnapshots)
                    throw new OrangeShellException("An invalid snapshot has been specified: " + index + "." + QuickCmdUsageOptions("classes"));

                snapshot = traceNavigator.GotoSnapshot(index);
            }
            else
            {
                if (traceNavigator.CurrentSnapshotIndex < 0)
                    throw new OrangeShellException("No snapshot is currently load. Please use next, prev or goto commands." + QuickCmdUsageOptions("classes"));

                snapshot = traceNavigator.CurrentSnapshot;
            }

            // should the output be truncated?
            if (ap.OptionPassed(option_truncate_output))
                truncateTo = ap.GetOption(option_truncate_output).AsInt;
            
            // what element should the output be sorted on?
            if (ap.OptionPassed(option_sort_output))
            {
                sortOn = ap.GetOption(option_sort_output).AsString;

                if (sortOn.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ClassId";
                else if (sortOn.Equals("name", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ClassName";
                else if (sortOn.Equals("bytes", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "TotalBytes";
                else if (sortOn.Equals("instances", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "TotalInstances";
                else
                    throw new OrangeShellException("Invalid sort element specified." + QuickCmdUsageOptions("classes"));

                // should the output be sorted in descending order?
                if (ap.OptionPassed(option_descending_sort))
                    sortOn += " descending";
            }


            // Displaying the output... 

            StringBuilder sb = new StringBuilder();

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-19}{1,-19}{2,-19}{3,-12}{4}\r\n", "class", "instances", "total bytes", "finalizable", "name");
                sb.AppendFormat("  {0,-19}{1,-19}{2,-19}{3,-12}{4}\r\n", "=====", "=========", "===========", "===========", "====");
            }
            else
            {
                sb.AppendFormat("  {0,-11}{1,-11}{2,-12}{3,-12}{4}\r\n", "class", "instances", "total bytes", "finalizable", "name");
                sb.AppendFormat("  {0,-11}{1,-11}{2,-12}{3,-12}{4}\r\n", "=====", "=========", "===========", "===========", "====");
            }

            // get all loaded types/classes
            var classes = snapshot.Classes.AsQueryable<Class>();

            if (!string.IsNullOrEmpty(filter))
                classes = classes.Where(filter, null);

            if (!string.IsNullOrEmpty(sortOn))
                classes = classes.OrderBy(sortOn);

            if (-1 != truncateTo)
                classes = classes.Take(truncateTo); 

            foreach (var c in classes)
            {
                if (traceNavigator.Header.Is64BitProcess)
                {
                    sb.AppendFormat("  0x{0,-17:X16}0x{1,-17:X8}0x{2,-17:X8}{3,-12}{4}\r\n", c.ClassId, c.TotalInstances, c.TotalBytes, (c.IsFinalizable) ?"yes" :"no", c.ClassName);
                }
                else
                {
                    sb.AppendFormat("  0x{0,-9:X8}0x{1,-9:X8}0x{2,-10:X8}{3,-12}{4}\r\n", c.ClassId, c.TotalInstances, c.TotalBytes, (c.IsFinalizable) ? "yes" : "no", c.ClassName);
                }
            }

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }


        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The "DiffClasses" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "diffclasses",
                MinimumAbbrev = 5,
                ShortHelp = @"diffs loaded types for any two specified snapshots.",
                LongHelp = ""
                    + "\r\ndescription                                                                          "
                    + "\r\n===========                                                                          "
                    + "\r\nThis command diffs two snapshots and compares the loaded types/classes.              "
                    + "\r\n                                                                                     "
                    + "\r\nusage                                                                                "
                    + "\r\n=====                                                                                "
                    + "\r\nclasses -b [snapshot-index] -n [snapshot-index] -so [element] -d -t [number]         "
                    + "\r\n                                                                                     "
                    + "\r\n  -b :   This switch is optional. By default, this command uses the currently loaded "
                    + "\r\n         snapshot as the baseline. However you can identify a specific snapshot as   "
                    + "\r\n         the baseline. All snapshots are uniquely identified by their index.         "
                    + "\r\n  -n :   This switch is mandatory and is used to specify the snapshot to be compared "
                    + "\r\n         against the baseline snapshot.                                              "
                    + "\r\n  -so:   Sort on specified element This switch is optional. By default, the output   "
                    + "\r\n         is sorted on class IDs.                                                     "
                    + "\r\n  [element]                                                                          "
                    + "\r\n    id        : sort on class IDs (default).                                         "
                    + "\r\n    bytes     : sort on difference between the total bytes for a type.               "
                    + "\r\n    instances : sort on difference between number of live instances for a type.      "
                    + "\r\n    name      : sort on name of class.                                               "
                    + "\r\n  -d :   Conducts a descending sort. This switch is optional. Ascending sort is the  "
                    + "\r\n         default, if this switch is not specified.                                   "
                    + "\r\n  -t :   Truncates output to desired number of elements. This switch is optional.    "
                    + "\r\n                                                                                     "
                    + "\r\nexamples                                                                             "
                    + "\r\n========                                                                             "
                    + "\r\ndiffc -b 0 -n 2 -t 5 : compares loaded types/classes in snaphot 0 to snapshot 2 and  "
                    + "\r\n                       truncates output to 5 entries. Snapshot 0 is used as the      "
                    + "\r\n                       baseline snapshot.                                            "
                    + "\r\ndiffc -n 5 -s bytes  : compares loaded types/classes in snaphot 0 to those in the    "
                    + "\r\n                       currrently loaded snapshot (baseline) and sorts the output    "
                    + "\r\n                       on difference in bytes.                                       "
                    + "\r\n"
                    )
        ]
        public static void DiffClassesCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("diffclasses"));
            if (0 == traceNavigator.TotalSnapshots)
                throw new OrangeShellException("No snapshots were detected in this trace.");

            // we'll need to parse the arguments
            const string option_baseline_index = "b";
            const string option_othersnapshot_index = "n";
            const string option_sort_output = "so";
            const string option_descending_sort = "d";
            const string option_truncate_output = "t";

            Snapshot baseline_snapshot = null;
            Snapshot other_snapshot = null;
            int index_baseline_snapshot = -1;
            int index_other_snapshot = -1;
            int truncateTo = -1;
            string sortOn = "ClassId";
            
            OrangeArgParser ap = new OrangeArgParser(
                arguments.Trim(),
                string.Concat(option_baseline_index, ":1", ";", option_othersnapshot_index, ":1", ";", option_sort_output, ":1", ";", option_descending_sort, ";", option_truncate_output, ":1")
                );


            // which snapshot should we use as the baseline?
            if (ap.OptionPassed(option_baseline_index))
            {
                index_baseline_snapshot = ap.GetOption(option_baseline_index).AsInt;

                if (index_baseline_snapshot < 0 || index_baseline_snapshot >= traceNavigator.TotalSnapshots)
                    throw new OrangeShellException("An invalid snapshot has been specified: " + index_baseline_snapshot + "." + QuickCmdUsageOptions("diffclasses"));

                baseline_snapshot = traceNavigator.GotoSnapshot(index_baseline_snapshot);
            }
            else
            {
                if (traceNavigator.CurrentSnapshotIndex < 0)
                    throw new OrangeShellException("No snapshot is currently load. Please use next, prev or goto commands." + QuickCmdUsageOptions("diffclasses"));

                baseline_snapshot = traceNavigator.CurrentSnapshot;
            }

            // which is the second snapshot?
            if (ap.OptionPassed(option_othersnapshot_index))
            {
                index_other_snapshot = ap.GetOption(option_othersnapshot_index).AsInt;

                if (index_other_snapshot < 0 || index_other_snapshot >= traceNavigator.TotalSnapshots)
                    throw new OrangeShellException("An invalid snapshot has been specified: " + index_other_snapshot + "." + QuickCmdUsageOptions("classes"));

                other_snapshot = traceNavigator.GotoSnapshot(index_other_snapshot);
            }
            else
            {
                throw new OrangeShellException("Please specify the snapshot to be compared against the baseline." + QuickCmdUsageOptions("diffclasses"));
            }

            // should the output be truncated?
            if (ap.OptionPassed(option_truncate_output))
                truncateTo = ap.GetOption(option_truncate_output).AsInt;

            // what element should the output be sorted on?
            if (ap.OptionPassed(option_sort_output))
            {
                sortOn = ap.GetOption(option_sort_output).AsString;

                if (sortOn.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ClassId";
                else if (sortOn.Equals("name", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "ClassName";
                else if (sortOn.Equals("bytes", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "TotalBytesDiff";
                else if (sortOn.Equals("instances", StringComparison.InvariantCultureIgnoreCase))
                    sortOn = "TotalInstancesDiff";
                else
                    throw new OrangeShellException("Invalid sort element specified." + QuickCmdUsageOptions("diffclasses"));

                // should the output be sorted in descending order?
                if (ap.OptionPassed(option_descending_sort))
                    sortOn += " descending";
            }

                
            // display the title/header ......

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Comparing snapshot #{0} to snapshot #{1}\r\n\r\n", index_baseline_snapshot, index_other_snapshot);

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-19}{1,-19}{2,-19}{3,-19}{4,-19}{5}\r\n", "class", "instances", "diff", "total bytes", "diff", "name");
                sb.AppendFormat("  {0,-19}{1,-19}{2,-19}{3,-19}{4,-19}{5}\r\n", "=====", "=========", "====", "===========", "====", "====");
            }
            else
            {
                sb.AppendFormat("  {0,-11}{1,-33}{2,-33}{3}\r\n", "class", "instances", "total bytes", "name");
                sb.AppendFormat("  {0,-11}#{1,-10}#{2,-10}{3,-11}#{4,-10}#{5,-10}{6,-11}{7}\r\n", "", index_baseline_snapshot, index_other_snapshot, "diff", index_baseline_snapshot, index_other_snapshot, "diff", "");
                sb.AppendFormat("  {0,-11}{1,-11}{2,-11}{3,-11}{4,-11}{5,-11}{6,-11}{7}\r\n", "=====", "==========", "==========", "==========", "==========", "==========", "==========", "====");
            }


            // now let us get the list of all class-ids

            var ids_baseline = from c in baseline_snapshot.Classes
                               select c.ClassId;

            var ids_other = from c in other_snapshot.Classes
                            select c.ClassId;

            var classIds = ids_baseline.Union(ids_other).Distinct(); // we'll stash all classIds for later use


            // The LINQ queries

            var diff_basics = from classId in classIds
                              select new
                              {
                                    classId,

                                    L = (from c in baseline_snapshot.Classes
                                            where c.ClassId == classId
                                            select c
                                        ).SingleOrDefault(),


                                    R = (from c in other_snapshot.Classes
                                            where c.ClassId == classId
                                            select c
                                        ).SingleOrDefault(),
                              };


            var diff_details = (
                                   from diff_basic in diff_basics.AsParallel().AsQueryable()
                                   select new
                                   {
                                       Id = diff_basic.classId,

                                       ClassName = (null == diff_basic.L) ?diff_basic.R.ClassName :diff_basic.L.ClassName,

                                       LTotalInstances = (null == diff_basic.L) ? 0 : diff_basic.L.TotalInstances,

                                       RTotalInstances = (null == diff_basic.R) ? 0 : diff_basic.R.TotalInstances,

                                       TotalInstancesDiff = (null == diff_basic.R)
                                                            ? (null == diff_basic.L)
                                                                ? 0
                                                                : (0 - diff_basic.L.TotalInstances)
                                                            : (null == diff_basic.L)
                                                                ? diff_basic.R.TotalInstances
                                                                : (diff_basic.R.TotalInstances - diff_basic.L.TotalInstances),

                                       LTotalBytes = (null == diff_basic.L) ? 0 : diff_basic.L.TotalBytes,

                                       RTotalBytes = (null == diff_basic.R) ? 0 : diff_basic.R.TotalBytes,

                                       TotalBytesDiff = (null == diff_basic.R)
                                                            ? (null == diff_basic.L)
                                                                ? 0
                                                                : (0 - diff_basic.L.TotalBytes)
                                                            : (null == diff_basic.L)
                                                                ? diff_basic.R.TotalBytes
                                                                : (diff_basic.R.TotalBytes - diff_basic.L.TotalBytes),
                                   }
                                ).OrderBy(sortOn);

            if (-1 != truncateTo)
                diff_details = diff_details.Take(truncateTo);


            // finally display the diffs ......

            foreach (var diff in diff_details)
            {
                if (traceNavigator.Header.Is64BitProcess)
                {
                }
                else 
                {
                    sb.AppendFormat("  0x{0,-9:X8}0x{1,-9:X8}0x{2,-9:X8}{3,-11}0x{4,-9:X8}0x{5,-9:x8}{6,-11}{7}\r\n", diff.Id, diff.LTotalInstances, diff.RTotalInstances, diff.TotalInstancesDiff, diff.LTotalBytes, diff.RTotalBytes, diff.TotalBytesDiff, diff.ClassName);
                }
            }

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The "modules" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "modules",
                MinimumAbbrev = 1,
                ShortHelp = @"Parses the xml report generated by the 'mem' command and displays loaded modules.",
                LongHelp = ""
            )
        ]
        public static void ModulesCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("modules"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("modules"));

            //            if (0 == arguments.Length)
            //                throw new OrangeShellException("Did you pass in location of the xml report file?" + tagCallbackStatsHelp);

            Snapshot snaphot = traceNavigator.CurrentSnapshot;
            var modules = from module in snaphot.Modules
                                     orderby module.ModuleId
                                     select module;


            StringBuilder sb = new StringBuilder();

            if (traceNavigator.Header.Is64BitProcess)
            {
                sb.AppendFormat("  {0,-20}{1,-20}{2}\r\n", "Module", "Assemby", "Module Name");
                sb.AppendFormat("  {0,-20}{1,-20}{2}\r\n", "======", "=======", "===========");
            }
            else
            {
                sb.AppendFormat("  {0,-12}{1,-12}{2}\r\n", "Module", "Assembly", "Module Name");
                sb.AppendFormat("  {0,-12}{1,-12}{2}\r\n", "======", "========", "===========");
            }

            foreach (var m in modules)
            {

                if (traceNavigator.Header.Is64BitProcess)
                {
                    sb.AppendFormat("  0x{0,-18:X16}0x{1,-18:X16}{2}\r\n", m.ModuleId, m.AssemblyId, m.ModuleFullPath);
                }
                else
                {
                    sb.AppendFormat("  0x{0,-10:X8}0x{1,-10:X8}{2}\r\n", m.ModuleId, m.AssemblyId, m.ModuleFullPath);
                }
            }

            Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////





        /// <summary>
        /// The "diagnose" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "wtf",
                MinimumAbbrev = 3,
                ShortHelp = @"Displays all gc related issues in current snapshot.",
                LongHelp = ""
            ),
            CommandDescription(
                CommandName = "diagnose",
                MinimumAbbrev = 3,
                ShortHelp = @"Displays all gc related issues in current snapshot.",
                LongHelp = ""
            )

        ]
        public static void DiagnoseCmd(string arguments)
        {
            // pre-condition checks
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("This command does not support arguments." + QuickCmdUsageOptions("diagnose"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("diagnose"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("diagnose"));


            Orange.Shell.WriteLine(OutputType.LOG, "Scanning gc heap for excessive fragmentation ......");

            Snapshot snapshot = traceNavigator.CurrentSnapshot;
            var generations = from item in snapshot.Generations
                              select item;


            DetectFragmentationIssues(ref snapshot);

           // topobjs(ref snapshot);



        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="snapshot"></param>
        private static void DetectFragmentationIssues(ref Snapshot snapshot)
        {
            // for each generation range ....
            foreach (var generation in snapshot.Generations)
            {
                // if fragmentation is greater than some threshold (say 30%) .......
                var fragmentation = generation.Fragmentation;
                if (fragmentation > 30)
                {
                    Orange.Shell.WriteLine(OutputType.LOG, "Fragmentation in heap:" + generation.HeapId + ", generation:" + generation.GenerationId + " is " + fragmentation + "%.");

                    var pinnedObjs = generation.PinnedObjects;

                    Orange.Shell.WriteLine(OutputType.LOG, "  Scanning for pinned objects...");

                    Orange.Shell.WriteLine(OutputType.LOG, "  " + pinnedObjs.Count() + " pinned objects found.");

                    if (0 == pinnedObjs.Count())
                    {
                        continue;
                    }

                    foreach (var pinnedObj in pinnedObjs)
                    {
                        Orange.ExecuteCommand(string.Format("roots 0x{0:X}", pinnedObj.ObjectId));
                    }

 


                }


            }

        }



        private static void topobjs(ref Snapshot snapshot)
        {
            //Snapshot _snapshot = snapshot;

            //var topClassesByBytes = GetClasses(ref snapshot, "TotalInstances descending", 5, string.Empty);

            //foreach (var c in topClassesByBytes)
            //{
            //    Console.WriteLine("0x{0:X8} 0x{1:X8}", c.ClassId, c.TotalInstances);


            //    var objs = GetFReachableObjects(ref snapshot, string.Empty, -1, "ClassId == @0", c.ClassId);
            //    foreach (var obj in objs)
            //        Console.WriteLine("\tfreach: 0x{0:X8}", obj.ObjectId);

            //    var objs2 = GetPinnedObjects(ref snapshot, string.Empty, -1, "ClassId == @0", c.ClassId);
            //    foreach (var obj in objs2)
            //        Console.WriteLine("\tpin: 0x{0:X8}", obj.ObjectId);

            //}


        }

        ///////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "htrace" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "htrace",
                MinimumAbbrev = 3,
                ShortHelp = @"displays allocation call-stack for specified gc-handle.",
                LongHelp = ""
            )
        ]
        public static void HTraceCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (0 == arguments.Length)
                throw new OrangeShellException("Did you pass in the handle ID to lookup?" + QuickCmdUsageOptions("htrace"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("htrace"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("htrace"));

            OrangeArgParser ap = new OrangeArgParser(arguments.Trim());

            if (0 == ap.Count)
            {
                throw new OrangeShellException("No handle ID has been specified." + QuickCmdUsageOptions("htrace"));
            }
            if (ap.Count > 1)
            {
                throw new OrangeShellException("Too many arguments specified." + QuickCmdUsageOptions("htrace"));
            }


            UInt64 handleId = 0;

            try
            {
                handleId = (UInt64)ap.GetArgument(0).AsHexOrDecInt;
            }
            catch (FormatException)
            {
                throw new OrangeShellException("The handle ID is in an invalid format." + QuickCmdUsageOptions("htrace"));
            }

            Snapshot snapshot = traceNavigator.CurrentSnapshot;

            var handles = from handleInfo in snapshot.HandleInfos
                        where (handleId == handleInfo.HandleId)
                        select handleInfo;


            if (handles.Count() > 1)
            {
                throw new OrangeShellException("Multiple stacks for Handle ID =" + handleId + " were detected.\r\n" +
                               "Please contact the developer for support");
            }

            if (0 == handles.Count())
            {
                throw new OrangeShellException("Allocation stack could not be detected for Handle ID =" + handleId + ".\r\n" +
                               "Please contact the developer for support");
            }


            var stacktrace = handles.Single().RawStackTrace;


            if (0 == stacktrace.Count())
            {
                Orange.Shell.WriteLine(OutputType.LOG, "  [CLR Implementation]");
            }
            else 
            {
                StringBuilder sb = new StringBuilder();

                foreach (var frame in stacktrace)
                {
                    if (0 == frame)
                    {
                        sb.AppendLine("  [Native Code]");
                    }
                    else
                    {
                        var functions = from functionInfo in snapshot.FunctionInfos
                                        where (functionInfo.FunctionId == frame)
                                        select functionInfo;

                        if (functions.Count() > 1)
                        {
                            throw new OrangeShellException("Multiple functions for function ID =" + frame + " were detected.\r\n" +
                                           "Please contact the developer for support");
                        }

                        if (0 == functions.Count())
                        {
                            throw new OrangeShellException("FunctionInfo could not be detected for function ID =" + frame + ".\r\n" +
                                           "Please contact the developer for support");
                        }

                        var function = functions.Single();

                        sb.AppendFormat("  {0}\r\n", function.ResolvedFunctionName);
                    }
                }

                Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
            }

        }


        ///////////////////////////////////////////////////////////////////////////

        # region roots command

        /// <summary>
        /// The "Roots" command.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "roots",
                MinimumAbbrev = 3,
                ShortHelp = @"Tracks and displays object heirarchy all the way to the roots.",
                LongHelp = ""
            )
        ]
        public static void RootsCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (0 == arguments.Length)
                throw new OrangeShellException("Did you pass in the object address to lookup?" + QuickCmdUsageOptions("Roots"));
            if (string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("To use this command, a trace file has to be loaded first." + QuickCmdUsageOptions("finalizedobjects"));
            if (traceNavigator.CurrentSnapshotIndex < 0)
                throw new OrangeShellException("This command can only by used on snapshots. Please use next, prev or goto commands." + QuickCmdUsageOptions("finalizedObjects"));

            OrangeArgParser ap = new OrangeArgParser(arguments.Trim());

            if (0 == ap.Count)
            {
                throw new OrangeShellException("No object address has been specified." + QuickCmdUsageOptions("Roots"));
            }
            if (ap.Count > 1)
            {
                throw new OrangeShellException("Too many arguments specified." + QuickCmdUsageOptions("Roots"));
            }


            UInt64 objectId = 0;

            try
            {
                objectId = (UInt64)ap.GetArgument(0).AsHexOrDecInt;
            }
            catch (FormatException)
            {
                throw new OrangeShellException("The object address is in an invalid format." + QuickCmdUsageOptions("Roots"));
            }


            Dictionary<UInt64, bool> heapObjLookupTable = new Dictionary<UInt64, bool>();
            Snapshot snapshot = traceNavigator.CurrentSnapshot;
            IEnumerable<ObjectInfo> cachedObjectList = snapshot.Objects;
            IEnumerable<RootReference> cachedRootReferenceList = snapshot.RootReferences;
            IEnumerable<Class> cachedClassList = snapshot.Classes;

            foreach (var heapObj in cachedObjectList)
                heapObjLookupTable[heapObj.ObjectId] = false;

            var heapObjQuery = from obj in cachedObjectList
                               where (obj.ObjectId == objectId)
                               select obj;

            if (0 == heapObjQuery.ToList().Count)
            {
                throw new OrangeShellException("Could not locate object with address =" + arguments + "\r\n" +
                                               "Are you sure you have passed in the correct address?");
            }

            else if (heapObjQuery.ToList().Count > 1)
            {
                throw new OrangeShellException("Multiple objects with id =" + arguments + " were detected.\r\n" +
                                               "Please contact the developer for support");
            }

            ScanForRoots(heapObjQuery.ElementAt(0), 0, ref heapObjLookupTable, ref cachedObjectList, ref cachedRootReferenceList, ref cachedClassList);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="heapObj"></param>
        /// <param name="indentLevel"></param>
        /// <param name="heapObjLookupTable"></param>
        /// <param name="cachedObjectList"></param>
        /// <param name="cachedRootReferenceList"></param>
        private static void ScanForRoots(
            ObjectInfo heapObj,
            int indentLevel,
            ref Dictionary<UInt64, bool> heapObjLookupTable,
            ref IEnumerable<ObjectInfo> cachedObjectList,
            ref IEnumerable<RootReference> cachedRootReferenceList,
            ref IEnumerable<Class> cachedClassList)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < indentLevel; i++)
                sb.Append("  ");
            sb.AppendFormat("0x{0:x}  {1}\r\n", heapObj.ObjectId, FindClass(heapObj, ref cachedClassList).ClassName);

            // Mark the object as "visited" in the lookup table.
            heapObjLookupTable[heapObj.ObjectId] = true;
            Orange.Shell.Write(OutputType.LOG, sb.ToString());

            sb = new StringBuilder(); // @todo - replace with sb.Clear() later.
            var roots = FindRoots(heapObj, ref cachedRootReferenceList);
            foreach (var root in roots)
            {
                for (int i = 0; i < indentLevel + 1; i++)
                    sb.Append("  ");
                sb.AppendFormat("[ROOT:{0}] 0x{1:x}\r\n", GetRootTypeString((COR_PRF_GC_ROOT_KIND)root.RootKind, (COR_PRF_GC_ROOT_FLAGS)root.RootFlag), root.RootId);
            }
            Orange.Shell.Write(OutputType.LOG, sb.ToString(), 0, sb.ToString().Length); // highlight the roots.

            var parents = FindParentObjects(heapObj, ref cachedObjectList/*, ref cachedObjRefList*/);
            foreach (var parent in parents)
                if (false == heapObjLookupTable[parent.ObjectId]) // if object has not been visited previously
                    ScanForRoots(parent, indentLevel + 1, ref heapObjLookupTable, ref cachedObjectList/*, ref cachedObjRefList*/, ref cachedRootReferenceList, ref cachedClassList);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="heapObj"></param>
        /// <param name="cachedObjectList"></param>
        /// <returns></returns>
        private static IEnumerable<ObjectInfo> FindParentObjects(ObjectInfo heapObj, ref IEnumerable<ObjectInfo> cachedObjectList /*, ref IEnumerable<ObjectReference> cachedObjRefList*/)
        {
            return (from obj in cachedObjectList
                    from refObj in obj.ReferencedObjects
                    where (refObj == heapObj.ObjectId)
                    select obj);            

            //var objRefQuery = from objRef in cachedObjRefList
            //                  from objId in objRef.ReferencedObjects
            //                  where (objId == heapObj.Id)
            //                  select objRef;

            //return (from obj in cachedObjectList
            //        from refObj in objRefQuery
            //        where (refObj.ObjectId == obj.Id)
            //        select obj);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="heapObj"></param>
        /// <param name="cachedRootReferenceList"></param>
        /// <returns></returns>
        private static IEnumerable<RootReference> FindRoots(ObjectInfo heapObj, ref IEnumerable<RootReference> cachedRootReferenceList)
        {
            return (from rootRef in cachedRootReferenceList
                    where (rootRef.ReferencedObjectId == heapObj.ObjectId)
                    select rootRef);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="heapObj"></param>
        /// <param name="cachedClassList"></param>
        /// <returns></returns>
        private static Class FindClass(ObjectInfo heapObj, ref IEnumerable<Class> cachedClassList)
        {
            var classQuery = from c in cachedClassList
                                 where (c.ClassId == heapObj.ClassId)
                                 select c;

            if (1 != classQuery.ToList().Count)
            {
                throw new OrangeShellException("Could not locate class information for with address = " + heapObj.ObjectId + "\r\n" +
                                               "Please contact the developer for support");
            }

            return classQuery.ElementAt(0);
        }

        # endregion // roots command


        ///////////////////////////////////////////////////////////////////////////

    }

    ///////////////////////////////////////////////////////////////////////////

}
