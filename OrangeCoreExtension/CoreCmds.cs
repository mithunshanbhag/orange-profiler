///////////////////////////////////////////////////////////////////////////////
// HelpCmd.cs  : Implements basic commands like 'load', 'quit', 'help' etc.  //
// Application : CLR V4 Profiler Test Infrastructure                         //
// Author      : Mithun Shanbhag                                             //
///////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.Linq;
//using System.Linq.Parallel;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using OrangeUtil;


namespace OrangeClient
{
    //////////////////////////////////////////////////
    // ----< partial class OrangeCoreExtension >----//
    //////////////////////////////////////////////////
 
    public sealed partial class OrangeCoreExtension
    {

        ///////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// The "HELP" command.
        /// </summary>
        [
            CommandDescription(
                CommandName = "help",
                MinimumAbbrev = 1,
                ShortHelp = @"displays this help screen.",
                LongHelp = ""
                    + "\r\n\t?              - displays the list of available Orange! commmands."
                    + "\r\n\thelp           - displays the list of available Orange! commmands."
                    + "\r\n\thelp [command] - displays usage/help-info on selected Orange! command."
                    + "\r\n\t? [command]    - same as above."
                    + "\r\n"
            ),
            CommandDescription(
                CommandName = "?",
                MinimumAbbrev = 1,
                ShortHelp = @"same as the 'help' command.",
                LongHelp = @"see the 'help' command."
            )
        ]
        public static void HelpCmd(string arguments)
        {
            if (null == arguments)
                throw new ArgumentNullException("arguments");

            if (arguments.Length == 0)
            {
                HashSet<string> hashAssemblyNames = new HashSet<string>();                                 
                foreach (ICommand c in Orange.Commands)                
                    hashAssemblyNames.Add(c.Assembly.GetName().Name);

                Orange.Shell.WriteLine(OutputType.LOG, "\r\nThe following commands are available -\r\n" );
                                  
                foreach(string assemblyName in hashAssemblyNames)
                {
                    string strAssemblyName = string.Format("  [{0}]\r\n", assemblyName); 
                    Orange.Shell.WriteLine(OutputType.LOG, strAssemblyName, 0, strAssemblyName.Length);
                    
                    StringBuilder sb = new StringBuilder();
                    foreach(ICommand c in Orange.Commands)
                    {
                        if (c.Assembly.GetName().Name.Equals(assemblyName))
                        {
                            string cmdname;
                            if (c.MinimumAbbrev != c.CommandName.Length)
                                cmdname = c.CommandName.Substring(0, c.MinimumAbbrev) + "[" + c.CommandName.Substring(c.MinimumAbbrev) + "]";
                            else
                                cmdname = c.CommandName;

                            sb.AppendFormat("  {0,-14} {1} \r\n", "command:", cmdname);
                            sb.AppendFormat("  {0,-14} {1} \r\n\r\n", "description:", c.ShortHelp);
                        }   
                    }
                    
                    Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
                }
            }

            else
            {
                ICommand c = Orange.Commands.Lookup(arguments);

                string name;
                if (c.MinimumAbbrev != c.CommandName.Length)
                    name = c.CommandName.Substring(0, c.MinimumAbbrev) + "[" + c.CommandName.Substring(c.MinimumAbbrev) + "]";
                else
                    name = c.CommandName;

                string longHelp;
                if (string.IsNullOrEmpty(c.LongHelp))
                    longHelp = "<unavailable>";
                else
                    longHelp = c.LongHelp;

                StringBuilder sb = new StringBuilder("\r\n");
                sb.Append(string.Format("  {0}\r\n", name));
                sb.Append(string.Format("  {0}\r\n", longHelp));

                Orange.Shell.WriteLine(OutputType.LOG, sb.ToString());
            }
        }

        
        ///  <summary>
        ///  <summary>
        public static string QuickCmdUsageOptions(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
                throw new ArgumentException("cmd");
        
            return string.Format("\r\nType 'help {0}' to see usage options.", cmd);
        }
    
       
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The "CONFIG" command.
        /// </summary>
        [CommandDescription(
            CommandName = "config",
            MinimumAbbrev = 3,
            ShortHelp = @"display or modify current Orange! configurations",
            LongHelp = ""
                + "\r\n\tconfig                    - displays current configurations."
                + "\r\n\tconfig [property]         - displays value of selected configuration/property."
                + "\r\n\tconfig [property = value] - sets value of selected configuration/property."
                + "\r\n\tconfig [property =]       - clears value of selected configuration/property."
                + "\r\n" 
            )
        ]
        public static void configCmd(string arguments)
        {

            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");

            arguments = arguments.Trim();

            // Is the user simply querying all the configurations?
            if (0 == arguments.Length)
            {
                Orange.Shell.Write(OutputType.LOG, Orange.ConfigItems.ToStr());
            }

            // Is the user trying to reset all the configurations?
            else if (arguments.Equals("reset", StringComparison.InvariantCultureIgnoreCase))
            {
                Orange.ConfigItems.Reset();
                Orange.Shell.Write(OutputType.LOG, Orange.ConfigItems.ToStr());
            }

            else
            {
                // Is the user querying for a specific configuration or property?
                if (-1 == arguments.IndexOf('='))
                {
                    IConfigItem item = Orange.ConfigItems.Lookup(arguments);

                    if (null == item)
                        throw new OrangeShellException("Invalid configuration '" + arguments + "' specified." + QuickCmdUsageOptions("config"));

                    Orange.Shell.Write(OutputType.LOG, item.ToString());
                }

                // Or
                // Is the user trying to modify/set a particular property?
                else
                {
                    string[] args = arguments.Split(new char[] { '=' });
                    string strKey = args[0].Trim();
                    string strVal = args[args.Length - 1].Trim();

                    if (strKey.Equals("=") || strVal.Equals("=") || args.Length != 2)
                        throw new OrangeShellException("Input was passed incorrectly." + QuickCmdUsageOptions("Config"));

                    IConfigItem item = Orange.ConfigItems.Lookup(strKey);

                    if (null == item)
                        throw new OrangeShellException("Invalid configuration '" + strKey + "' specified." + QuickCmdUsageOptions("Config"));

                    item.Value = strVal;
                    Orange.Shell.Write(OutputType.LOG, item.ToString());
                }
            }            
        }

        ///////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "LOAD" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "load",
            MinimumAbbrev = 2,
            ShortHelp = @"loads an Orange! extension",
            LongHelp = @""
            )
        ]
        public static void LoadCmd(string arguments)
        {
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (0 == arguments.Length)
                throw new OrangeShellException("You did not specify which extension to load." + QuickCmdUsageOptions("Load"));

            arguments = Environment.ExpandEnvironmentVariables(arguments).ToLower().Trim().StripLeadingLaggingQuotes().AppendIfNeeded(".dll");

            if (File.Exists(arguments))
            {
                Orange.Extensions.LoadExtension(arguments);
                Orange.Shell.WriteLine(OutputType.LOG, "Extension loaded: " + arguments);
            }

            else
            {
                bool bFound = false;
                string[] paths = CONFIG_ExtPaths.Split(new char[]{Path.PathSeparator}, StringSplitOptions.RemoveEmptyEntries);

                foreach (string path in paths)
                {
                    string assemblypath = Environment.ExpandEnvironmentVariables(path).ToLower().Trim().StripLeadingLaggingQuotes().AppendIfNeeded("\\").AppendIfNeeded(arguments);

                    Orange.Shell.WriteLine(OutputType.TRACE, "Probing folder: \"" + path + "\" for extension \"" + arguments + "\"" + "\r\n");

                    if (File.Exists(assemblypath))
                    {
                        bFound = true;
                        Orange.Extensions.LoadExtension(assemblypath);
                        Orange.Shell.WriteLine(OutputType.LOG, "Extension loaded: " + assemblypath);
                        break;
                    }
                }

                if (!bFound)
                    throw new OrangeShellException("Could not locate the extension file.\r\n" +
                                                   "Are you sure you have passed in the correct filename or path?");
            }
        }
        
        
        /// <summary>
        ///
        /// <summary>
        [ConfigItemDescription(
            Name = "extpath",
            Description = @"Locations to search while loading Orange! extensions.",
            IsHidden = false,
            DefaultValue = "."
            )
        ]
        private static string CONFIG_ExtPaths;
        
        ///////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "MODE" command
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "mode",
            MinimumAbbrev = 2,
            ShortHelp = @"displays, modifies available modes",
            LongHelp = @""
                + "\r\n\tmode                 - displays all available modes."
                + "\r\n\tmode [item]          - displays selected mode."
                + "\r\n\tmode [item [on|off]] - toggles selected mode on/off."
                + "\r\n\tmode reset           - resets all modes to default."
                + "\r\n"
            )
        ]
        public static void ModeCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");

            string[] args = arguments.ToLower().Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length > 2)
                throw new OrangeShellException("Too many arguments specified!" + QuickCmdUsageOptions("Mode"));


            // Is the user simply querying all the modes?
            if (0 == args.Length)
            {
                Orange.Shell.Write(OutputType.LOG, Orange.ModeItems.ToStr());
            }

            // Is the user trying to reset all modes?
            else if (1 == args.Length && args[0].Equals("reset", StringComparison.InvariantCultureIgnoreCase))
            {
                Orange.ModeItems.Reset();
                Orange.Shell.Write(OutputType.LOG, Orange.ModeItems.ToStr());
            }

            else
            {
                OrangeModeItem item = null;
                foreach (OrangeModeItem mode in Orange.ModeItems)
                {
                    if (mode.Name == args[0])
                    {
                        item = mode;
                        break;
                    }
                }

                if (null == item)
                {
                    throw new OrangeShellException("Invalid mode '" + args[0] + "' specified." + QuickCmdUsageOptions("Mode"));
                }

                // Or
                // Is the user querying for a specific mode?
                if (1 == args.Length)
                {
                    Orange.Shell.Write(OutputType.LOG, item.ToString());
                }

                // Or
                // Is the user trying to modify/set a particular mode?
                else
                {
                    switch (args[1])
                    {
                        case "on":
                            item.Value = true;
                            break;

                        case "off":
                            item.Value = false;
                            break;

                        default:
                            throw new OrangeShellException("Invalid argument '" + args[1] + "' specified." + QuickCmdUsageOptions("Mode"));
                    }

                    Orange.Shell.Write(OutputType.LOG, item.ToString());
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        
        
        /// <summary>
        /// The "QUIT" / "EXIT" commands.
        /// </summary>
        /// <param name="arguments"></param>
        [
            CommandDescription(
                CommandName = "quit",
                MinimumAbbrev = 1,
                ShortHelp = @"Exits the Orange! shell.",
                LongHelp = ""
                    + "\r\n\tquit            - Exits the Orange! shell."
                    + "\r\n\texit            - Exits the Orange! shell."
                    + "\r\n\tquit [exitcode] - Terminates the Orange! shell with given exit code <not implemented!>."
                    + "\r\n\texit [exitcode] - Terminates the Orange! shell with given exit code <not implemented!>."
                    + "\r\n"
            ),
            CommandDescription(
                CommandName = "exit",
                MinimumAbbrev = 2,
                ShortHelp = @"same as the 'quit' command.",
                LongHelp = @"same as the'quit' command."
            )
        ]
        public static void QuitCmd(string arguments)
        {
            if (null != arguments && 0 != arguments.Length)
                throw new OrangeShellException("Currently the quit/exit command does not support arguments." + QuickCmdUsageOptions("Quit"));

            Orange.Shell.Kill();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        
        
        /// <summary>
        /// The "SCRIPT" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "script",
            MinimumAbbrev = 3,
            ShortHelp = @"loads and executes a script file containing Orange! commands",
            LongHelp = ""
                + "\r\n\tscript [script-file] - Executes Orange! commands contained in the script-file. Please"
                + "\r\n\t                       specify the full-path to the script file. The full path can   "
                + "\r\n\t                       contain environment variables.                                "
                + "\r\n"
            )
        ]
        public static void ScriptCmd(string arguments)
        {
            // pre-condition checks
            if (null == arguments)
                throw new ArgumentNullException("arguments");
            if (0 == arguments.Length)
                throw new OrangeShellException("Did you pass in location of the script-file?" + QuickCmdUsageOptions("Script"));

            arguments = Environment.ExpandEnvironmentVariables(arguments).ToLower().Trim().StripLeadingLaggingQuotes();

            if (!File.Exists(arguments))
                throw new OrangeShellException("Could not locate the file: " + arguments + ".\r\n" +
                                               "Are you sure you have specified the correct path and filename?");

            // Now load the script file.
            List<string> cmds = new List<string>();
            using (StreamReader file = new StreamReader(arguments))
            {
                string line;
                int lineno = 0; // one-based
                while (null != (line = file.ReadLine()))
                {
                    ++lineno;

                    if (0 == line.Length || line.StartsWith("#")) // all comments begin with the character '#'.
                        continue;

                    foreach (char c in line)
                    {
                        if (char.IsControl(c))
                            throw new OrangeShellException("Invalid character '" + c.ToString() + "' detected on line: " + lineno);
                    }

                    cmds.Add(line);
                }

                if (0 == cmds.Count)
                    throw new OrangeShellException("Could not locate any Orange! commands in the script file.");
                else
                    Orange.Shell.Run(cmds);
            }
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "run",
            MinimumAbbrev = 1,
            ShortHelp = @"@todo - coreext",
            LongHelp = @"" +
                "\r\nrun [path_to_exe] [arguments] - runs specified executable." +
                "\r\n"
            )
        ]
        public static void RunCmd(string arguments)
        {
            string strPathToExe = null;
            string strExeArgs = null;

            // retrieve the exe and args            
            RetrieveExeAndArgs(arguments, out strPathToExe, out strExeArgs);
            if (string.IsNullOrEmpty(strPathToExe))
                throw new ApplicationException("Could not retrieve the path to the executable!");

            // time to run the program
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Environment.ExpandEnvironmentVariables(strPathToExe);
            psi.Arguments = (null == strExeArgs) ? "" : Environment.ExpandEnvironmentVariables(strExeArgs);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;


            Orange.Shell.WriteLine(OutputType.TRACE, "Launching - " + psi.FileName + " " + psi.Arguments + "\r\n");

            try
            {
                Process p = Process.Start(psi);
                
                string stdOut = p.StandardOutput.ReadToEnd();
                string stdErr = p.StandardError.ReadToEnd();
                
                p.WaitForExit();

                Orange.Shell.WriteLine(OutputType.TRACE, "ExitCode = " + p.ExitCode + "\r\n");
                Orange.Shell.WriteLine(OutputType.TRACE, "StdOut =" + stdOut + "\r\n");
                Orange.Shell.WriteLine(OutputType.TRACE, "StdErr = " + stdErr + "\r\n");                
            }
            
            catch
            {
                throw new OrangeShellException("An error occured while running the executable - '" + psi.FileName + "'");
            }
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "SHELL" command.
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "shell",
            MinimumAbbrev = 2,
            ShortHelp = @"executes given DOS command",
            LongHelp = @""
            )
        ]
        public static void ShellCmd(string arguments)
        {
            if (null == arguments)
                throw new ArgumentNullException("arguments");

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");

            psi.Arguments = (0 == arguments.Length)
                                ? "/k prompt shell^> "
                                : "/c" + arguments;

            psi.UseShellExecute = false;
            psi.CreateNoWindow = false;

            Process p = Process.Start(psi);
            p.WaitForExit();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The "MemAnalyze" command
        /// </summary>
        /// <param name="arguments"></param>
        [CommandDescription(
            CommandName = "memanalyze",
            MinimumAbbrev = 3,
            ShortHelp = @"@todo - coreext",
            LongHelp = @"" +
                "\r\nmemanalyze [path_to_exe] [arguments] - Runs specified executable under memory profiler." +
                "\r\nmemanalyze -p [process id] - Attaches memory profiler to specified process. Please prepend with 0x if PID is hex." +
                "\r\nmemanalyze -pn [process name] - Attaches memory profielr to specified running process."
            )
        ]
        public static void MemAnalyzeCmd(string arguments)
        {
            bool bAttachMode = false;
            Process p = null;
            string strPathToExe = null;
            string strExeArgs = null;

            if (string.IsNullOrEmpty(arguments))
                throw new OrangeShellException("No arguments specified." + QuickCmdUsageOptions("MemAnalyze"));

            arguments = arguments.Trim();

            //
            // Detect user specified options
            //

            // Has a process name been supplied?
            if (arguments.StartsWith("-pn", StringComparison.InvariantCultureIgnoreCase))
            {
                arguments = arguments.Substring(3).Trim().ToLowerInvariant();

                // We have to strip off the ".exe" from the string. This is because of a limitation 
                // in Process.GetProcessesByName().
                if (arguments.EndsWith(".exe"))
                    arguments = arguments.Substring(0, arguments.Length - 4);

                if (string.IsNullOrEmpty(arguments))
                    throw new OrangeShellException("Could not detect the process name." + QuickCmdUsageOptions("MemAnalyze"));

                Process[] processes = Process.GetProcessesByName(arguments);

                if (0 == processes.Length)
                    throw new OrangeShellException("Could not detect process with name = " + arguments + "\r\n" +
                                                    "Are you sure you have specified the correct process name?");

                if (processes.Length > 1)
                    throw new OrangeShellException("Multiple processes detected with same name: " + arguments + "\r\n" +
                                                    "Please use the -p option to disambiguate.");
                
                p = processes[0];
                bAttachMode = true;
            }

            // Has a PID been supplied?
            else if (arguments.StartsWith("-p", StringComparison.InvariantCultureIgnoreCase))
            {
                arguments = arguments.Substring(2).Trim().ToLowerInvariant();

                int pid = 0;
                if (!CheckIfInteger(arguments, out pid))
                    throw new OrangeShellException("The Process Id seems to be invalid." + QuickCmdUsageOptions("MemAnalyze"));

                p = Process.GetProcessById(pid);
                if (null == p)
                {
                    throw new OrangeShellException("Could not detect process with PID = " + pid + "\r\n" +
                                                    "Are you sure you have specified the correct PID?");
                }

                bAttachMode = true;
            }

            // Has an executable (and options args) been supplied? 
            else 
            {
                RetrieveExeAndArgs(arguments, out strPathToExe, out strExeArgs);
                if (string.IsNullOrEmpty(strPathToExe))
                    throw new OrangeShellException("Could not retrieve the path to the executable." + QuickCmdUsageOptions("MemAnalyze"));
            }

            // pre-condtion check: Check if a trace file is currently loaded. This needs to be 
            // unloaded before user can run memanalysis again.
            if (!string.IsNullOrEmpty(traceNavigator.TraceFile))
                throw new OrangeShellException("Before running this command, please unload the trace file: " + traceNavigator.TraceFile + "\r\n" +
                                               "Please use the 'unload' command first.");

            CONFIG_OP_TRACE_FILE_NAME = string.Concat(
                                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).TrimEnd(new char[] { '\\' }),
                                            Path.DirectorySeparatorChar,
                                            "tracefile.xml");

            CONFIG_COR_PROFILER_PATH = string.Concat(
                                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).TrimEnd(new char[] { '\\' }), 
                                            Path.DirectorySeparatorChar, 
                                            "orangeprofiler.dll");

            if (!File.Exists(CONFIG_COR_PROFILER_PATH))
                throw new Exception("Unable to locate the profiler: " + CONFIG_COR_PROFILER_PATH + "\r\n");
 

            //try
            //{
                // If a trace file with same name, path exits then delete it.
                if (File.Exists(CONFIG_OP_TRACE_FILE_NAME))
                    File.Delete(CONFIG_OP_TRACE_FILE_NAME);

                // Time to profile the executable / process
                if (bAttachMode)
                    AttachProfilerToProcess(p);
                else
                    LaunchExecutableWithProfiler(strPathToExe, strExeArgs);

                // Has a trace file been generated?.
                if (!File.Exists(CONFIG_OP_TRACE_FILE_NAME))
                    throw new OrangeShellException("Could not detect trace file: " + CONFIG_OP_TRACE_FILE_NAME);
                else 
                {
                    Orange.Shell.WriteLine(OutputType.LOG, "\r\n");
                    Orange.Shell.WriteLine(OutputType.LOG, "  ==========================================================", 0, 60); 
                    Orange.Shell.WriteLine(OutputType.LOG, "  Trace file generated: " + CONFIG_OP_TRACE_FILE_NAME);
                    Orange.Shell.WriteLine(OutputType.LOG, "  ==========================================================", 0, 60); 
                }
            //}
            //catch (Exception ex)
            //{
            //    if (ex is IOException)
            //        throw new OrangeShellException("Orange profiler is unable to create the tracefile since it appears to be locked!");
            //    else
            //        throw new OrangeShellException("Unable to profile the specified process!");
            //}
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

    
    }
}
