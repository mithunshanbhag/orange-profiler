///////////////////////////////////////////////////////////////////////////////
// OrangeConfigs.cs : Introduces several tweakable configurations.           //
// Application      : CLR V4 DST Test Infrastructure                         //
// Author           : Mithun Shanbhag, mithuns@microsoft.com                 //
//                    Some parts of this module have been lifted directly    //
//                    from the MDbg sources.                                 //
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace OrangeClient
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////
    //----< class IConfigItem >----//
    /////////////////////////////////

    /// <summary>
    /// Interface for the configuration items.
    /// </summary>
    public interface IConfigItem : IComparable<IConfigItem>
    {
        /// <summary>
        /// Returns the name of the config item.
        /// </summary>
        /// <value>Name of the config item.</value>
        string Name
        {
            get;
        }

        /// <summary>
        /// Returns a description (help string) associated with the config item.
        /// </summary>
        /// <value>Description (help string) associated with the config item.</value>       
        string Description
        {
            get;
        }
        
        /// <summary>
        /// Is the config item hidden or publicly visible.
        /// </summary>
        /// <value>Visibility of the config item.</value>        
        bool IsHidden
        {
            get;
        }

        /// <summary>
        /// The value stored by the config item.
        /// </summary>
        /// <value>Value stored by the config item.</value>        
        string Value
        {
            get;
            set;
        }


        /// <summary>
        /// The extension in which the config item was defined
        /// </summary>
        /// <value>The extension's type</value>         
        Type Extension
        {
            get;
        }

        /// <summary>
        /// Assembly the config item was loaded from
        /// </summary>
        /// <value>The Assembly.</value>
        Assembly Assembly
        {
            get;
        }


        /// <summary>
        /// Resets the config item to its default value.
        /// </summary>        
        void Reset();        
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    
    //////////////////////////////////////////////
    //----< ConfigItemDescriptionAttribute >----//
    //////////////////////////////////////////////

    /// <summary>
    /// This attribute describes the config item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public sealed class ConfigItemDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Returns the name of the config item.
        /// </summary>
        /// <value>Name of the config item.</value>
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }
        
        /// <summary>
        /// Returns a description (help string) associated with the config item.
        /// </summary>
        /// <value>Description (help string) associated with the config item.</value>       
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
            }
        }
        
        /// <summary>
        /// Is the config item hidden or publicly visible.
        /// </summary>
        /// <value>Visibility of the config item.</value>        
        public bool IsHidden
        {
            get
            {
                return m_IsHidden;
            }
            set
            {
                m_IsHidden = value;
            }
        }

        /// <summary>
        /// Returns the default value of the config item.
        /// </summary>
        /// <value>Default value of the config item..</value>       
        public string DefaultValue
        {
            get
            {
                return m_DefaultValue;
            }
            set
            {
                m_DefaultValue = value;
            }
        }

        
        // backing stores
        private string m_Name;
        private string m_Description;
        private string m_DefaultValue;
        private bool m_IsHidden; 
    }
    
    //////////////////////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////
    //----< class OrangeConfigItem >----//
    //////////////////////////////////////

    /// <summary>
    /// 
    /// </summary>      
    public sealed class OrangeConfigItem : IConfigItem
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Constructor. Creates a new OrangeConfigItem object.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="descriptionAttribute"></param>
        public OrangeConfigItem(FieldInfo fieldInfo, ConfigItemDescriptionAttribute descriptionAttribute)
        {
            // pre-condition checks
            if (null == fieldInfo)
                throw new ArgumentNullException("fieldInfo");
            if (null == descriptionAttribute)
                throw new ArgumentNullException("descriptionAttribute");
            if (string.IsNullOrEmpty(descriptionAttribute.Name))
                throw new OrangeShellException("Cannot add config item'" + descriptionAttribute.Name + "'. No name provided");
            if (string.IsNullOrEmpty(descriptionAttribute.Description))
                throw new OrangeShellException("Cannot add config item'" + descriptionAttribute.Name + "'. No description provided");

            m_fi = fieldInfo;
            m_configItemDescr = descriptionAttribute;
            Value = descriptionAttribute.DefaultValue;
        }

        /// <value>
        /// Shortcut used by user to change the config.
        /// </value>
        public string Name
        {
            get
            {
                return m_configItemDescr.Name;
            }
        }

        /// <value>
        /// A description of the config.
        /// </value>
        public string Description
        {
            get
            {
                return m_configItemDescr.Description;
            }
        }

        /// <value>
        /// Visibility of the config item.
        /// </value>        
        public bool IsHidden
        {
            get 
            {
                return m_configItemDescr.IsHidden;
            }
        }

        /// <value>
        /// Gets/sets current setting of the config.
        /// </value>
        public string Value
        {
            get
            {
                return Environment.ExpandEnvironmentVariables((string) m_fi.GetValue(null));
            }
            set
            {
                m_fi.SetValue(null, Environment.ExpandEnvironmentVariables(value));
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {
            Value = m_configItemDescr.DefaultValue;
        }


        /// <summary>
        /// Assembly the config item was loaded from
        /// </summary>
        /// <value>The Assembly.</value>
        public Assembly Assembly
        {
            get
            {
                return m_fi.DeclaringType.Assembly;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public Type Extension
        {
            get
            {
                return m_fi.DeclaringType;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(IConfigItem ci)
        {
            // we'll sort the commands first by assembly, then by extension and finally by it's name.

            if (!this.Assembly.Equals(ci.Assembly))
                return string.Compare(this.Assembly.ToString(), ci.Assembly.ToString(), true);

            if (!this.Extension.Equals(ci.Extension))
                return string.Compare(this.Extension.ToString(), ci.Extension.ToString(), true);

            // commands are from same assembly and extension
            return String.Compare(Name, ci.Name, true);
        }

        
        /// <summary>
        /// Textual representation of the config item.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
    
            sb.Append(string.Format("  {0,-14} {1} = {2}\r\n", "config:", Name, Value));
            sb.Append(string.Format("  {0,-14} {1} \r\n", "description:", Description));

            return sb.ToString();
        }


        ///////////////////////////////////////
        // private helper methods and fields //
        ///////////////////////////////////////

        private readonly FieldInfo m_fi;
        private readonly ConfigItemDescriptionAttribute m_configItemDescr;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////
    //----< class ConfigsExtension >----//
    ////////////////////////////////////

    /// <summary>
    /// Provides an extension method to facilitate the 
    /// textual representation of a list of OrangeConfigItems.
    /// </summary>
    public static class ConfigsExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configs"></param>
        /// <returns></returns>
        public static string ToStr(this IConfigItemCollection configs)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("The following configurations are available - \r\n\r\n");
            foreach (OrangeConfigItem config in configs)
            {
                if (!config.IsHidden)
                {
                    sb.Append(string.Format("  {0,-14} {1} = {2}\r\n", "config:", config.Name, config.Value));
                    sb.Append(string.Format("  {0,-14} {1}  \r\n\r\n", "description:", config.Description));
                }
            }    

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configs"></param>
        public static void Reset(this IConfigItemCollection configs)
        {
            foreach (OrangeConfigItem config in configs)
                config.Reset();
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

}

