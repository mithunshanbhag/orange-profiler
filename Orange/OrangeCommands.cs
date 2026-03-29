///////////////////////////////////////////////////////////////////////////////
// OrangeCommands.cs :                                                       //
// Application       : CLR V4 Profiler Test Infrastructure                   //
// Author            : Mithun Shanbhag, mithuns@microsoft.com                //
///////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace OrangeClient
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////
    //----< interface ICommandCollection >----//
    ////////////////////////////////////////////

    /// <summary>
    /// Interface for command collections.
    /// </summary>
    public interface ICommandCollection : IEnumerable
    {
        /// <summary>
        /// Adds a command to the collection.
        /// </summary>
        /// <param name="command">Command to add.</param>
        void Add(ICommand command);

        /// <summary>
        /// Looks up a command in the collection.
        /// </summary>
        /// <param name="cmd">The name of the command to look up.</param>
        /// <returns>The command corresponding to the given name.</returns>
        ICommand Lookup(string cmd);

        /// <summary>
        /// Parses a command.
        /// </summary>
        /// <param name="fullText">Raw text for the command and arguments all together.</param>
        /// <param name="command">Returns the command from the given text.</param>
        /// <param name="commandArguments">Returns the arguments from the given text.</param>
        void ParseCommand(string fullText, out ICommand command, out string commandArguments);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////
    //----< class OrangeCommandCollection >----//
    /////////////////////////////////////////////

    public class OrangeCommands : ICommandCollection
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Adds Orange! commands to the collection.
        /// </summary>
        /// <param name="command"></param>
        public void Add(ICommand command)
        {
            // pre-condition check
            if (command == null)
                throw new ArgumentNullException("command");

            // extensions are allowed to override a command, i.e. this means we will 
            // delete previously defined command with same name.
            foreach (ICommand c in m_commands)
            {
                if (c.CommandName.Equals(command.CommandName))
                {
                    m_commands.Remove(c);
                    break;
                }
            }

            m_commands.Add(command);
            m_needSorting = true;
        }

        /// <summary>
        /// Checks to see if specified Orange! command already exists in the collection.
        /// </summary>
        /// <param name="commandName">The name of the Orange! command.</param>
        /// <returns>The command object if it exists in the collection.</returns>
        public ICommand Lookup(string commandName)
        {
            // pre-condition check
            if (null == commandName)
                throw new ArgumentNullException("commandName");
            if (0 == commandName.Length)
                throw new ArgumentException("Cannot pass in zero length string as an argument", "commandName");

            List<ICommand> cmdList = new List<ICommand>();
            foreach (ICommand c in m_commands)
            {
                if (c.CommandName.Equals(commandName) || c.CommandName.Substring(0, c.MinimumAbbrev).Equals(commandName))
                    cmdList.Add(c);
            }

            string exMsg = "\r\nType 'help' to see a complete list of commands.";

            if (cmdList.Count == 0)
                throw new OrangeShellException("Command '" + commandName + "' not found." + exMsg);

            else if (cmdList.Count == 1)
                return (ICommand)cmdList[0];

            else
            {
                StringBuilder s = new StringBuilder("Command prefix too short.\r\n" + "Possible completitions:");
                foreach (ICommand c in cmdList)
                {
                    s.Append("\r\n").Append(c.CommandName);
                }
                throw new OrangeShellException(s.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            if (m_needSorting)
            {
                m_needSorting = false;
                m_commands.Sort();
            }

            return m_commands.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandLineText"></param>
        /// <param name="command"></param>
        /// <param name="commandArguments"></param>
        public void ParseCommand(string commandLineText, out ICommand command, out string commandArguments)
        {
            // pre-condition check
            if (null == commandLineText)
                throw new ArgumentNullException("commandLineText");
            if (0 == commandLineText.Length)
                throw new OrangeShellException("Cannot pass in a zero-length string as command");

            commandLineText = commandLineText.ToLower().Trim();
            int index = (commandLineText.IndexOf(' ') == -1) ? commandLineText.Length : commandLineText.IndexOf(' ');

            command = Lookup(commandLineText.Substring(0, index));
            commandArguments = (index == commandLineText.Length) ? "" : commandLineText.Substring(index + 1);
        }

        ////////////////////////////////
        // private members and fields //
        ////////////////////////////////

        private List<ICommand> m_commands = new List<ICommand>();
        private bool m_needSorting = false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
