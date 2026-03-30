///////////////////////////////////////////////////////////////////////////
// OrangeCommand.cs :                                                    //
// Application      : CLR V4 Profiler Test Infrastructure                //
// Author           : Mithun Shanbhag                                    //
///////////////////////////////////////////////////////////////////////////


using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;



namespace OrangeClient
{
    
    ///////////////////////////////////////////////////////////////////////////

    //////////////////////////////////
    //----< interface ICommand >----//
    //////////////////////////////////

    /// <summary>
    /// Interface for the commands.
    /// </summary>
    public interface ICommand : IComparable<ICommand>
    {
        /// <summary>
        /// Returns the command name.
        /// </summary>
        /// <value>Name of the command.</value>
        string CommandName
        {
            get;
        }

        /// <summary>
        /// Returns the minimum number of characters you must use to invoke this command.
        /// </summary>
        /// <value>The minimum number of characters.</value>
        int MinimumAbbrev
        {
            get;
        }

        /// <summary>
        /// Returns a brief help message for the command.
        /// </summary>
        /// <value>The help message.</value>
        string ShortHelp
        {
            get;
        }

        /// <summary>
        /// Returns a more detailed help message for the command.
        /// </summary>
        /// <value>The help message.</value>
        string LongHelp
        {
            get;
        }

        /// <summary>
        /// Assembly the command was loaded from
        /// </summary>
        /// <value>The Assembly.</value>
        Assembly Assembly
        {
            get;
        }

        /// <summary>
        /// The extension in which the command was defined
        /// </summary>
        /// <value>The extension's type</value> 
        Type Extension
        {
            get;
        } 

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="args">Arguments to pass to the command.</param>
        void Execute(string args);
    }

    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////////////
    //----< CommandDescriptionAttribute >----//
    ///////////////////////////////////////////

    /// <summary>
    /// This attribute describes the command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class CommandDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Returns the command name.
        /// </summary>
        /// <value>Name of the command.</value>
        public string CommandName
        {
            get
            {
                return m_commandName;
            }
            set
            {
                m_commandName = value;
                m_minimumAbbrev = m_commandName.Length;
            }
        }

        /// <summary>
        /// Returns the minimum number of characters you must use to invoke this command.
        /// </summary>
        /// <value>The minimum number of characters.</value>
        public int MinimumAbbrev
        {
            get
            {
                return m_minimumAbbrev;
            }
            set
            {
                m_minimumAbbrev = value;
            }
        }

        /// <summary>
        /// Returns a brief help message for the command.
        /// </summary>
        /// <value>The help message.</value>
        public string ShortHelp
        {
            get
            {
                return m_shortHelp;
            }
            set
            {
                m_shortHelp = value;
            }
        }

        /// <summary>
        /// Returns a more detailed help message for the command.
        /// </summary>
        /// <value>The help message.</value>
        public string LongHelp
        {
            get
            {
                return m_longHelp == null ? "usage: \r\n" + m_shortHelp : m_longHelp;
            }
            set
            {
                m_longHelp = value;
            }
        }

        // backing stores
        private string m_commandName;
        private int m_minimumAbbrev;
        private string m_shortHelp;
        private string m_longHelp;
    }

    ///////////////////////////////////////////////////////////////////////////

    /////////////////////////////
    //----< OrangeCommand >----//
    /////////////////////////////

    /// <summary>
    /// This class defines the "Orange!" commands.
    /// </summary>
    public sealed class OrangeCommand : ICommand
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="descriptionAttribute"></param>
        public OrangeCommand(MethodInfo methodInfo, CommandDescriptionAttribute descriptionAttribute)
        {
            // pre-condition checks 
            if (null == methodInfo)
                throw new ArgumentNullException("methodInfo");
            if (null == descriptionAttribute)
                throw new ArgumentNullException("descriptionAttribute");
            if (string.IsNullOrEmpty(descriptionAttribute.ShortHelp))
                throw new OrangeShellException("Cannot add command '" + descriptionAttribute.CommandName + "'. No help description provided");
            if (descriptionAttribute.MinimumAbbrev > descriptionAttribute.CommandName.Length)
                throw new OrangeShellException("Cannot add command '" + descriptionAttribute.CommandName + "'. Abbreviation is " +
                    descriptionAttribute.MinimumAbbrev + " characters. Can't be more than " + descriptionAttribute.CommandName.Length + ".");

            m_mi = methodInfo;
            m_cmdDescr = descriptionAttribute;
        }

        /// <summary>
        /// Returns the command name.
        /// </summary>
        /// <value>Name of the command.</value>
        public string CommandName
        {
            get
            {
                return m_cmdDescr.CommandName;
            }
        }

        /// <summary>
        /// Returns the minimum number of characters you must use to invoke this command.
        /// </summary>
        /// <value>The minimum number of characters.</value>
        public int MinimumAbbrev
        {
            get
            {
                return m_cmdDescr.MinimumAbbrev;
            }
        }

        /// <summary>
        /// Returns a brief help message for the command.
        /// </summary>
        /// <value>The help message.</value>
        public string ShortHelp
        {
            get
            {
                return m_cmdDescr.ShortHelp;
            }
        }

        /// <summary>
        /// Returns a more detailed help message for the command.
        /// </summary>
        /// <value>The help message.</value>
        public string LongHelp
        {
            get
            {
                return m_cmdDescr.LongHelp;
            }
        }

        /// <summary>
        /// Assembly the command was loaded from
        /// </summary>
        /// <value>The Assembly.</value>
        public Assembly Assembly
        {
            get
            {
                return m_mi.DeclaringType.Assembly;
            }
        }

        /// <summary>
        /// The extension in which the command was defined
        /// </summary>
        /// <value>The extension's type</value> 
        public Type Extension
        {
            get
            {
                return m_mi.DeclaringType;
            }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="arguments">Arguments to pass to the command.</param>
        public void Execute(string arguments)
        {
            m_mi.Invoke(null, new object[] { arguments });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(ICommand cmd)
        {
            // we'll sort the commands first by assembly, then by extension and finally by it's name.

            if (!this.Assembly.Equals(cmd.Assembly))
                return string.Compare(this.Assembly.ToString(), cmd.Assembly.ToString(), true);

            if (!this.Extension.Equals(cmd.Extension))
                return string.Compare(this.Extension.ToString(), cmd.Extension.ToString(), true);

            // commands are from same assembly and extension
            return String.Compare(CommandName, cmd.CommandName, true);
        }


        ////////////////////////////////
        // private methods and fields //
        ////////////////////////////////

        private readonly MethodInfo m_mi;
        private readonly CommandDescriptionAttribute m_cmdDescr;
    }

}
