///////////////////////////////////////////////////////////////////////////////
// Orange.cs   : @TODO - write something cool here.                          //
// Application : CLR V4 Profiler Test Infrastructure                         //
// Author      : Mithun Shanbhag, mithuns@microsoft.com                      //
///////////////////////////////////////////////////////////////////////////////


/*
 * Version History
 * ===============
 *  Version 0.90 - Early Prototype.
 *  Version 0.91 - Corrected a spelling mistake. Mode "trce" is now "trace". 
                 - The Orange! log files are now being generated in the form
                   "orange.[date].[time].log" in the "logs" sub-folder.
 *  Version 0.92 - The Orange shell component no longer writes to a log file.
 *                 The log files were completely unnecessary and not being used
 *                 anyways. Plus they were cumbersome to maintain, and we've had 
 *                 truncation issues with logs in the past. So best to do away 
 *                 with them.
 * 
 * Planned Bug-fixes
 * =================
 *  - A bug in orange! causes the shell to terminate after it finishes executing the 
 *    initial commands (!command). 
 *  - There exists a bug wherein if you executed the "script" command, after launching 
 *    Orange!, then the shell terminated after the last command in the script file was 
 *    executed.
 *  - Add support for reseting modes and configs.
 *  - Get rid of "tag" messagees.
 *  - Create a new Argument Parsing class to handle logic for arg-parsing.
 *  
 * Planned Modifications
 * =====================
 *  - N/A
 * 
 * Planned Features
 * ================
 *  - Custom assembly versioning for Orange! and ProfilerTestExt.
 *  - Try to load extensions in separate app-domains. This would make it possible to unload them
 *    later.
 * 
 */



using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.IO;


namespace OrangeClient
{

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////
    //----< class Orange >----//
    ////////////////////////////

    /// <summary>
    /// The engine of Orange!
    /// </summary>
    public class Orange
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="commands"></param>
        public Orange(IShell shell, ICommandCollection commands, IExtensionCollection extensions)
        {
            m_shell = shell;
            m_commands = commands;
            m_extensions = extensions;

            m_configs = new OrangeConfigItems();
            m_modes = new OrangeModeItems();

            m_shell.UserInputEvent += ProcessUserInput;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IShell Shell
        {
            get
            {
                return m_shell;
            }
            set
            {
                m_shell = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static ICommandCollection Commands
        {
            get
            {
                return m_commands;
            }
            set
            {
                m_commands = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static IExtensionCollection Extensions
        {
            get
            {
                return m_extensions;
            }
            set
            {
                m_extensions = value;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public static IConfigItemCollection ConfigItems
        {
            get
            {
                return m_configs;
            }
            set
            {
                m_configs = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static IModeItemCollection ModeItems
        {
            get
            {
                return m_modes;
            }
            set
            {
                m_modes = value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandText"></param>
        public static void ExecuteCommand(string commandText)
        {
            // pre-condition check
            if (null == commandText)
                throw new ArgumentNullException("commandText");
            if (0 == commandText.Length)
                throw new ArgumentException("Invalid argument passed in", "args.UserInput");

            ICommand cmd;
            string cmdArgs;

            Commands.ParseCommand(commandText, out cmd, out cmdArgs);
            cmd.Execute(cmdArgs); // execute the command
        }


        /// <summary>
        /// The program entry point.
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            // pre-condition check
            if (null == args)
                throw new ArgumentNullException("args");

            IShell shell = new OrangeShell();
            ICommandCollection commands = new OrangeCommands();
            IExtensionCollection extensions = new OrangeExtensions();
            Orange engine = new Orange(shell, commands, extensions);

            // We need to now publish the shell policies (as mode items) and the default extension
            // search path (as a config item).
            (extensions as OrangeExtensions).AddConfigAndModeItemsFromType(typeof(ShellPolicies));
            (extensions as OrangeExtensions).AddConfigAndModeItemsFromType(typeof(OrangeExtensions));

            // Now we'll add the commands from the coreExtension to the collection
            string coreExt = "orangecoreextension.dll";
            coreExt = string.Concat(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).TrimEnd(new char[] { '\\' }), Path.DirectorySeparatorChar, coreExt);

            if (!File.Exists(coreExt))
                throw new OrangeShellException("Unable to locate the core extension: " + coreExt + "\r\n");
            
            extensions.LoadExtension(coreExt);

            // Execute initial commands (if any) and then kickstart the shell.
            List<string> cmds = new List<string>();
            if (0 != args.Length)
                cmds = Orange.ProcessCmdLine(args);

            Orange.Shell.Write(OutputType.LOG, Orange.Shell.Properties.Banner);
            shell.Run(cmds);

            return 100;
        }

        ///////////////////////////////////////
        // private fields and helper methods //
        ///////////////////////////////////////

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void ProcessUserInput(object sender, ShellUserInputEventArgs args)
        {
            // pre-condition check
            if (null == args)
                throw new ArgumentNullException("args");
            if (null == args.UserInput || 0 == args.UserInput.Length)
                throw new ArgumentException("Invalid argument passed in", "args.UserInput");

            try
            {
                ExecuteCommand(args.UserInput);
            }
            catch (Exception e)
            {
                bool bExceptionReported = false;
                while (e != null && !bExceptionReported)
                {
                    if (e is OrangeShellException) 
                    {
                        Shell.WriteLine(OutputType.ERROR, e.Message);
                        bExceptionReported = true;
                    }    
                    else
                        e = e.InnerException;
                }
                if (!bExceptionReported || Shell.Policies.TerminateOnAnyException)
                    throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static List<string> ProcessCmdLine(string[] args)
        {
            // pre-condition check
            if (null == args)
                throw new ArgumentNullException("args");

            if (0 != args.Length)
            {
                string cmdline = "";
                foreach (string arg in args)
                {
                    cmdline = cmdline + " " + arg.ToLower();
                }

                if (-1 != cmdline.IndexOf('!'))
                {
                    args = cmdline.Trim().Split(new char[] {'!'}, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                { 
                    throw new OrangeShellException(
                                "Invalid arguments specified on command line.\r\n" +
                                "All Orange! commands should be space-delimited and begin with '!' prefix.");
                } 
            }

            List<string> cmds = new List<string>();
            foreach(string str in args)
                cmds.Add(str);

            return cmds;
        }



        private static IShell m_shell;
        private static ICommandCollection m_commands;
        private static IExtensionCollection m_extensions;
        private static IConfigItemCollection m_configs;
        private static IModeItemCollection m_modes;

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////



}

