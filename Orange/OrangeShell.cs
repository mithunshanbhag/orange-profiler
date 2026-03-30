///////////////////////////////////////////////////////////////////////////////
// OrangeShell.cs   : Implements the Orange shell. Handles user inputs,      //
//                    interprets and executes them. Also responsible for     // 
//                    logging output to console and to log file.             //
// Application      : CLR V4 DST Test Infrastructure                         //
// Author           : Mithun Shanbhag                                        //
//                    Some parts of this module have been lifted directly    //
//                    from the MDbg sources.                                 //
///////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.IO;


namespace OrangeClient
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// These logging levels can be used by extension writers.
    /// </summary>
    public enum OutputType
    {
        LOG,            // Anything written in this format will be displayed on console and in log file.
        WARNING,        
        ERROR,
        TRACE,          // Anything written in this format will be displayed only if mode "trace" is on.
        INTERNAL,       // For internal use only (by developers of the Orange tool). Enabled by "mode itrace on".
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////
    //----< interface IShell >----//
    ////////////////////////////////

    /// <summary>
    /// Interface for all Console/GUI shells.
    /// </summary>
    public interface IShell
    {
        /// <summary>
        /// Kick-starts the shell.
        /// </summary>
        /// <param name="initialCmds">list of initial cmds to execute first. Can be null.</param>
        void Run(List<string> initialCmds);

        /// <summary>
        /// Kills the shell, but restores it to the original state first.
        /// </summary>
        void Kill();

        /// <summary>
        /// Writes the specified string value to the output stream for the given output type.
        /// </summary>
        /// <param name="outputType">Specifies which OrangeOutputConstants output type to write to</param>
        /// <param name="output">The value to write.</param>
        void Write(OutputType outType, string output);

        /// <summary>
        /// Much like above method but allows for highlighting of parts of the string
        /// </summary>
        /// <param name="outputType">Specifies which OrangeOutputConstants output type to write to</param>
        /// <param name="message">The value to write.</param>
        /// <param name="highlightStart">The index to begin highlighting.</param>
        /// <param name="highlightLen">How many characters to highlight.</param>
        void Write(OutputType outType, string message, int highlightStart, int highlightLen);

        /// <summary>
        /// Writes the specified string value to the output stream for the given output type.
        /// </summary>
        /// <param name="outputType">Specifies which OrangeOutputConstants output type to write to</param>
        /// <param name="output">The value to write.</param>
        void WriteLine(OutputType outType, string output);

        /// <summary>
        /// Much like above method but allows for highlighting of parts of the string
        /// </summary>
        /// <param name="outputType">Specifies which OrangeOutputConstants output type to write to</param>
        /// <param name="message">The value to write.</param>
        /// <param name="highlightStart">The index to begin highlighting.</param>
        /// <param name="highlightLen">How many characters to highlight.</param>
        void WriteLine(OutputType outType, string message, int highlightStart, int highlightLen);

        /// <summary>
        /// Notifies the shell subscribers of any user input event.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        event EventHandler<ShellUserInputEventArgs> UserInputEvent;

        /// <summary>
        /// 
        /// </summary>
        ShellProperties Properties
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        ShellPolicies Policies
        {
            get;
            set;
        }
    }

    ///////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////
    //----< class OrangeShellException >----//
    //////////////////////////////////////////

    /// <summary>
    /// This is an exception for when the Orange! shell needs its own special type
    /// </summary>
    [Serializable()]
    public class OrangeShellException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the OrangeShellException class.
        /// </summary>
        public OrangeShellException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the OrangeShellException class with a 
        /// specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OrangeShellException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the OrangeShellException class with a 
        /// specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The inner exception for the new exception</param>
        public OrangeShellException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }    

    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////
    //----< OrangeShellPolicies >----//
    ///////////////////////////////////

    /// <summary>
    /// Tweakable policies of the Orange! Shell.
    /// - Repeating the last command: If this mode is set and the user presses 
    /// the return key, the shell will execute the last run command.
    /// - Echoing commands: If this mode is set, the commands will be echoed out
    /// again on the shell.
    /// - Highlighting warnings and errors: Self-Explanatory. 
    /// </summary>
    public sealed class ShellPolicies
    {
        /// <summary>
        /// Whether to repeat the last run command when the
        /// user presses the enter key?
        /// </summary>
        public bool RepeatLastCommand
        {
            get
            {
                return m_bRepeatLastCommand;
            }
            set
            {
                m_bRepeatLastCommand = value;
            }
        }

        /// <summary>
        /// Should the entered commands be echoed back to 
        /// console/shell?
        /// </summary>
        public bool EchoCommands
        {
            get
            {
                return m_bEchoCommands;
            }
            set
            {
                m_bEchoCommands = value;
            }
        }

        /// <summary>
        /// Should the shell assert on errors?
        /// This flag is used to Enable/Disable stopping at the point of failure.
        /// </summary>
        public bool AssertOnErrors
        {
            get
            {
                return m_bAssertOnErrors;
            }
            set
            {
                m_bAssertOnErrors = value;
            }
        }

        /// <summary>
        /// Should the warnings and errors be highlighted?
        /// </summary>
        public bool HighlightWarningsAndErrors
        {
            get
            {
                return m_bHighlightWarningsAndErrors;
            }
            set
            {
                m_bHighlightWarningsAndErrors = value;
            }
        }

        /// <summary>
        /// Should the developer traces be displayed?
        /// </summary>
        public bool DisplayTraces
        {
            get
            {
                return m_bDisplayTraces;
            }
            set
            {
                m_bDisplayTraces = value;
            }
        }


        /// <summary>
        /// Should we terminate on absolutely any exception? 
        /// This is useful only for running in the lab.
        /// </summary>
        public bool TerminateOnAnyException
        {
            get
            {
                return m_bTerminateOnAnyException;
            }
            set
            {
                m_bTerminateOnAnyException = value;
            }
        }


        // backing stores
        [ModeItemDescription(
            Name = "repeatlastcmd", Description = @"repeats the last executed command when the return key is pressed.",
            IsHidden = true, DefaultValue = true)]
        private static bool m_bRepeatLastCommand;

        [ModeItemDescription(
            Name = "echocmds", Description = @"commands entered are echoed back to shell.",
            IsHidden = true, DefaultValue = false)]
        private static bool m_bEchoCommands;

        [ModeItemDescription(
            Name = "assertonerr", Description = @"shell asserts on any error.",
            IsHidden = true, DefaultValue = false)]
        private static bool m_bAssertOnErrors;

        [ModeItemDescription(
            Name = "highlightwarningsanderrs", Description = @"Warnings and errors are highlighted?",
            IsHidden = true, DefaultValue = false)]
        private static bool m_bHighlightWarningsAndErrors;

        [ModeItemDescription(
            Name = "trace", Description = @"turns on/off diagnostic traces.",
            IsHidden = false, DefaultValue = false)]
        private static bool m_bDisplayTraces;

        [ModeItemDescription(
            Name = "itrace", Description = @"turns on/off verbose diagnostic info.",
            IsHidden = true, DefaultValue = false)]
        private static bool m_bDisplayDiagnosticInfo;

        [ModeItemDescription(
            Name = "lab", Description = @"causes shell to terminates on any error/exception.",
            IsHidden = true, DefaultValue = false)]
        private static bool m_bTerminateOnAnyException;
    }

    ///////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////
    //----< OrangeShellProperties >----//
    /////////////////////////////////////

    /// <summary>
    /// Tweakable properties applicable to the Orange! shell.
    /// - Foreground color.
    /// - Background color.
    /// - Window Title.
    /// - The shell prompt.
    /// </summary>
    public sealed class ShellProperties
    {
        /// <summary>
        /// Constructor. Captures the old state of the console.
        /// We'll need to restore these when we exit out.
        /// </summary>
        public ShellProperties()
        {
            m_bOldTreatControlCAsInput = System.Console.TreatControlCAsInput;
        }

        /// <summary>
        /// Restores the old state of the console. It is the responsibility
        /// of the shell implementation to call this method before exiting.
        /// </summary> 
        /// <returns></returns>
        public void RestoreOriginalState()
        {          
            System.Console.TreatControlCAsInput = m_bOldTreatControlCAsInput;
        }

        /// <summary>
        /// gets/sets the shell's console title.
        /// </summary>
        public String WindowTitle
        {
            get
            {
                return System.Console.Title;
            }
            set
            {
                System.Console.Title = value;
            }
        }

        /// <summary>
        /// gets/sets the shell's foreground color.
        /// </summary>
        public ConsoleColor ForegroundColor
        {
            get
            {
                return System.Console.ForegroundColor;
            }
            set
            {
                System.Console.ForegroundColor = value;
            }
        }

        /// <summary>
        /// gets/sets the shell's background color.
        /// </summary>
        public ConsoleColor BackgroundColor
        {
            get
            {
                return System.Console.BackgroundColor;
            }
            set
            {
                System.Console.BackgroundColor = value;
            }
        }

        /// <summary>
        /// gets/sets the shell's prompt.
        /// </summary>
        public String Prompt
        {
            get
            {
                return m_strPrompt;
            }
            set
            {
                if (null == value)
                    throw new NullReferenceException("The display prompt cannot be set to null!");

                m_strPrompt = value;
            }
        }

        /// <summary>
        /// gets/sets the Ctrl+C press behavior.
        /// </summary>       
        public bool TreatControlCAsInput
        {
            get
            {
                return System.Console.TreatControlCAsInput;
            }
            set
            {
                System.Console.TreatControlCAsInput = value;
            }
        }
    
        /// <summary>
        /// 
        /// </summary>
        public string Banner
        {
            get
            {
                return m_strBanner;
            }
            set
            {
                if (null == value)
                    throw new NullReferenceException("The banner cannot be set to null!");

                m_strBanner = value;
            }
        }


        // backing stores
        private string m_strPrompt;
        private string m_strBanner;
        private string m_strOldWindowTitle;
        private ConsoleColor m_ccOldForegroundColor;
        private ConsoleColor m_ccOldBackgroundColor;
        private bool m_bOldTreatControlCAsInput;
    }

    ///////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////
    //----< OrangeShellUserInputEventArgs >----//
    /////////////////////////////////////////////

    /// <summary>
    /// @todo - what if user input is "null", "return" or ctrl+C etc?
    /// Do we need some special case behavior to deal with this?
    /// </summary>
    public class ShellUserInputEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="strUserInput">Input typed in by the user.</param>
        public ShellUserInputEventArgs(string strUserInput)
        {
            m_strUserInput = strUserInput;
        }

        /// <summary>
        /// accessor property to retrieve the user input.
        /// </summary>
        public string UserInput
        {
            get
            {
                return m_strUserInput;
            }
        }

        private string m_strUserInput;
    }

    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////
    //----< OrangeShell >----//
    ///////////////////////////

    /// <summary>
    /// Module Responsibilities:
    /// 
    /// </summary>
    public sealed class OrangeShell : IShell
    {
        ///////////////////////////////////
        // Public Methods and Properties //
        ///////////////////////////////////

        /// <summary>
        /// Constructor. Initializes all shell properties and policies.
        /// </summary>
        public OrangeShell()
        {
            // set policies
            Policies.EchoCommands = false;
            Policies.RepeatLastCommand = true;

            // Initialize all the necessary properties
            Properties.Prompt = DEFAULT_PROMPT;
            Properties.Banner = DEFAULT_BANNER;

            // Define the Ctrl+C, Ctrl+Break handling behavior
            Properties.TreatControlCAsInput = false;
            System.Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
        }

        /// <summary>
        /// Implements the Run() method from the IShell interface.
        /// </summary>
        public void Run(List<string> initialCmds)
        {
            // Start the input loop ............
            while (!m_bIsExiting)
            {
                Write(OutputType.LOG, Properties.Prompt, 0, Properties.Prompt.Length); // displays the prompt

                if (null != initialCmds && 0 != initialCmds.Count)
                {
                    for (int idx = 0; idx < initialCmds.Count; idx++)
                    {
                        WaitForUserInput(initialCmds[idx]);

                        // if we are done with the initial commands, then time to exit
                        if (idx == initialCmds.Count - 1)
                            m_bIsExiting = true;

                        if (!m_bIsExiting)
                            Write(OutputType.LOG, Properties.Prompt, 0, Properties.Prompt.Length); 
                        else
                            break; 
                    }
                }
                
                else
                    WaitForUserInput(null);
            }
        }


        /// <summary>
        /// Implementing the Kill() method from the IShell interface.
        /// Restores the console to original state and kills the shell.
        /// </summary>
        public void Kill()
        {
            Properties.RestoreOriginalState();
            m_bIsExiting = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputType"></param>
        /// <param name="text"></param>
        public void Write(OutputType outtype, string text)
        {
            Write(outtype, text, 0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputType"></param>
        /// <param name="text"></param>
        /// <param name="hilightStart"></param>
        /// <param name="hilightLen"></param>
        public void Write(OutputType outtype, string text, int hilightStart, int hilightLen)
        {
            try
            {
                Monitor.Enter(m_lock);

                // pre-condition checks
                if (hilightStart < 0)
                    throw new ArgumentException("hilightStart < 0");
                if (hilightLen < 0)
                    throw new ArgumentException("hilightLen < 0");
                if (hilightStart > text.Length)
                    throw new ArgumentException("hilightStart > text.Length");
                if (hilightStart > text.Length)
                    throw new ArgumentException("hilightStart > text.Length");

                string s;

                switch (outtype)
                {
                    case OutputType.ERROR:
                        s = TEXT_ERROR + text;
                        WriteOut(s, OutputType.ERROR, 0, s.Length);
                        break;

                    case OutputType.WARNING:
                        s = TEXT_WARNING + text;
                        WriteOut(s, OutputType.WARNING, 0, s.Length);
                        break;

                    case OutputType.INTERNAL:
                    case OutputType.TRACE:
                        if (Policies.DisplayTraces)
                        {
                            s = TEXT_TRACE + text;
                            WriteOut(s, OutputType.TRACE, 0, s.Length);
                        }
                        break;

                    case OutputType.LOG:
                    default:
                        WriteOut(text, OutputType.LOG, hilightStart, hilightLen);
                        break;
                }
            }

            finally
            {
                Monitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputType"></param>
        /// <param name="text"></param>
        public void WriteLine(OutputType outtype, string text)
        {
            Write(outtype, text + "\r\n", 0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputType"></param>
        /// <param name="text"></param>
        /// <param name="hilightStart"></param>
        /// <param name="hilightLen"></param>
        public void WriteLine(OutputType outtype, string text, int hilightStart, int hilightLen)
        {
            Write(outtype, text + "\r\n", hilightStart, hilightLen);
        }

        /// <summary>
        /// Accessors for getting/setting the shell policies.
        /// </summary>
        public ShellPolicies Policies
        {
            get
            {
                return m_policies;
            }
            set
            {
                m_policies = value;
            }
        }

        /// <summary>
        /// Accessors for getting/setting the shell properties.
        /// </summary>
        public ShellProperties Properties
        {
            get
            {
                return m_properties;
            }
            set
            {
                m_properties = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ShellUserInputEventArgs> UserInputEvent;


        ////////////////////////////
        // Private helper methods //
        ////////////////////////////

        /// <summary>
        /// If Ctrl+Break is pressed, exit application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs args)
        {
            m_bBreakHandlerExecuted = true;

            switch (args.SpecialKey)
            {
                case ConsoleSpecialKey.ControlC:
                    Write(OutputType.WARNING, "\r\nCtrl + C event received. Exiting............");
                    break;

                case ConsoleSpecialKey.ControlBreak:
                    Write(OutputType.WARNING, "\r\nCtrl + Break event received. Exiting............");
                    break;

                default: // how the heck did we land here?
                    Write(OutputType.WARNING, "\r\nExiting............");
                    break;
            }

            m_bIsExiting = true;
            args.Cancel = false;
            Kill();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionalInput"></param>
        private void WaitForUserInput(string strInput)
        {
        retry:

            m_bBreakHandlerExecuted = false;

            if (null == strInput)
                strInput = Console.ReadLine();
            else
                Console.WriteLine(strInput);

            if (null == strInput)
            {
                Thread.Sleep(100);

                if (m_bBreakHandlerExecuted)
                    goto retry; // means we have not hit an EOF.
            }

            else if (0 == strInput.Length) 
            {
                if (true == Policies.RepeatLastCommand && 0 != m_strLastCmdExecuted.Length)
                {
                    if (true == Policies.EchoCommands)
                        Write(OutputType.LOG, "\r\n" + m_strLastCmdExecuted + "\r\n");
            
                    // notify subscribers
                    UserInputEvent(this, new ShellUserInputEventArgs(m_strLastCmdExecuted));
                }
                else
                    return;
            }

            else
            {
                if (true == Policies.EchoCommands)
                    Write(OutputType.LOG, "\r\n" + strInput + "\r\n");

                // record the user input and notify subscribers
                m_strLastCmdExecuted = strInput;

                WriteLine(OutputType.LOG, "");                 
                UserInputEvent(this, new ShellUserInputEventArgs(strInput));
                if (!m_bIsExiting)
                    WriteLine(OutputType.LOG, "");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ht"></param>
        /// <param name="highlightStart"></param>
        /// <param name="highlighLen"></param>
        private void WriteOut(string text, OutputType outtype, int highlightStart, int highlighLen)
        {
            if (highlighLen == 0 || highlightStart >= text.Length)
            {
                Console.Write(text);
            }
            else
            {
                Console.Write(text.Substring(0, highlightStart));

                ConsoleColor fc = Properties.ForegroundColor;
                ConsoleColor bc = Properties.BackgroundColor;                

                switch(outtype)
                {
                    case OutputType.ERROR:
                        Properties.ForegroundColor = ConsoleColor.Red;
                        break;

                    case OutputType.LOG:
                    case OutputType.WARNING:
                        Properties.ForegroundColor = ConsoleColor.Yellow;
                        break;

                    case OutputType.INTERNAL:
                    case OutputType.TRACE:
                        Properties.ForegroundColor = ConsoleColor.Cyan;
                        break;

                    default:
                        break;
                }

                int l = highlightStart + highlighLen;
                if (l > text.Length)
                {
                    highlighLen = text.Length - highlightStart;
                }

                Console.Write(text.Substring(highlightStart, highlighLen));

                Properties.ForegroundColor = fc;
                Properties.BackgroundColor = bc;
                if (highlightStart + highlighLen < text.Length)
                {
                    Console.Write(text.Substring(highlightStart + highlighLen));
                }
            }
        }


        /////////////////////////////////////
        // private data members and fields //
        /////////////////////////////////////

        private ShellPolicies m_policies = new ShellPolicies();
        private ShellProperties m_properties = new ShellProperties();

        private bool m_bIsExiting = false;
        private bool m_bBreakHandlerExecuted = false;
        private string m_strLastCmdExecuted = "";

        private object m_lock = new object();

        private readonly string TEXT_ERROR          = "Error: ";
        private readonly string TEXT_WARNING        = "Warning: ";
        private readonly string TEXT_TRACE          = "Trace: ";
        private readonly string TEXT_DIAGNOSTICINFO = "Diagnostic: ";

        private readonly string DEFAULT_PROMPT                  = "Orange!> ";
        private readonly string DEFAULT_WINDOW_TITLE            = "Orange! shell, Microsoft Corporation 2008.";
        private readonly ConsoleColor DEFAULT_FOREGROUND_COLOR  = ConsoleColor.White;
        private readonly ConsoleColor DEFAULT_BACKGROUND_COLOR  = ConsoleColor.Black;

        private readonly string DEFAULT_BANNER =
            "\r\n# The Orange! Shell [Version 0.9]" +
            "\r\n# Copyright (c) 2008 Microsoft Corporation. All rights reserved." +
            "\r\n# " + Assembly.GetExecutingAssembly().Location +
            "\r\n\r\n";            
    }
}
