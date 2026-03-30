///////////////////////////////////////////////////////////////////////////////
// OrangeModes.cs   : Introduces several tweakable modes.                    //
// Application      : CLR V4 DST Test Infrastructure                         //
// Author           : Mithun Shanbhag                                        //
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
    //----< class IModeItem >----//
    /////////////////////////////////

    /// <summary>
    /// Interface for the mode items.
    /// </summary>
    public interface IModeItem : IComparable<IModeItem>
    {
        /// <summary>
        /// Returns the name of the mode item.
        /// </summary>
        /// <value>Name of the mode item.</value>
        string Name
        {
            get;
        }

        /// <summary>
        /// Returns a description (help string) associated with the mode item.
        /// </summary>
        /// <value>Description (help string) associated with the mode item.</value>       
        string Description
        {
            get;
        }
        
        /// <summary>
        /// Is the mode item hidden or publicly visible.
        /// </summary>
        /// <value>Visibility of the mode item.</value>        
        bool IsHidden
        {
            get;
        }

        /// <summary>
        /// The value stored by the mode item.
        /// </summary>
        /// <value>Value stored by the mode item.</value>        
        bool Value
        {
            get;
            set;
        }


        /// <summary>
        /// The extension in which the mode item was defined
        /// </summary>
        /// <value>The extension's type</value>         
        Type Extension
        {
            get;
        }

        /// <summary>
        /// Assembly the mode item was loaded from
        /// </summary>
        /// <value>The Assembly.</value>
        Assembly Assembly
        {
            get;
        }


        /// <summary>
        /// Resets the mode item to its default value.
        /// </summary>        
        void Reset();        
    }

    //////////////////////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////////
    //----< ModeItemDescriptionAttribute >----//
    //////////////////////////////////////////////

    /// <summary>
    /// This attribute describes the mode item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public sealed class ModeItemDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Returns the name of the mode item.
        /// </summary>
        /// <value>Name of the mode item.</value>
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
        /// Returns a description (help string) associated with the mode item.
        /// </summary>
        /// <value>Description (help string) associated with the mode item.</value>       
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
        /// Is the mode item hidden or publicly visible.
        /// </summary>
        /// <value>Visibility of the mode item.</value>        
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
        /// Returns the default value of the mode item.
        /// </summary>
        /// <value>Default value of the mode item..</value>       
        public bool DefaultValue
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
        private bool m_DefaultValue;
        private bool m_IsHidden; 
    }
    
    //////////////////////////////////////////////////////////////////////////////////////////////


    ////////////////////////////////////
    //----< class OrangeModeItem >----//
    ////////////////////////////////////

    /// <summary>
    /// 
    /// </summary>      
    public sealed class OrangeModeItem : IModeItem
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Constructor. Creates a new OrangeModeItem object.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="descriptionAttribute"></param>
        public OrangeModeItem(FieldInfo fieldInfo, ModeItemDescriptionAttribute descriptionAttribute)
        
        {
            // pre-condition checks
            if (null == fieldInfo)
                throw new ArgumentNullException("fieldInfo");
            if (null == descriptionAttribute)
                throw new ArgumentNullException("descriptionAttribute");
            if (string.IsNullOrEmpty(descriptionAttribute.Name))
                throw new OrangeShellException("Cannot add mode item'" + descriptionAttribute.Name + "'. No name provided");
            if (string.IsNullOrEmpty(descriptionAttribute.Description))
                throw new OrangeShellException("Cannot add mode item'" + descriptionAttribute.Name + "'. No description provided");

            m_fi = fieldInfo;
            m_modeItemDescr = descriptionAttribute;
            Value = descriptionAttribute.DefaultValue;
        }

        /// <value>
        /// Shortcut used by user to change the mode.
        /// </value>
        public string Name
        { 
            get 
            { 
                return m_modeItemDescr.Name; 
            } 
        }

        /// <value>
        /// A description of the mode.
        /// </value>
        public string Description
        { 
            get 
            { 
                return m_modeItemDescr.Description; 
            } 
        }


        /// <summary>
        /// Is this a hidden mode item?
        /// </summary>
        public bool IsHidden
        {
            get
            {
                return m_modeItemDescr.IsHidden;
            }
        }


        /// <value>
        /// Gets/sets current setting of the mode.
        /// </value>
        public bool Value
        {
            get
            {
                return (bool) m_fi.GetValue(null);
            }
            set
            {
                m_fi.SetValue(null, value);
            }
        }
    

        /// <summary>
        /// Resets the mode to original state.
        /// </summary>
        public void Reset()
        {
            Value = m_modeItemDescr.DefaultValue;
        }
        
        
        /// <summary>
        /// Assembly the mode item was loaded from
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
        public int CompareTo(IModeItem mi)
        {
            // we'll sort the commands first by assembly, then by extension and finally by it's name.

            if (!this.Assembly.Equals(mi.Assembly))
                return string.Compare(this.Assembly.ToString(), mi.Assembly.ToString(), true);

            if (!this.Extension.Equals(mi.Extension))
                return string.Compare(this.Extension.ToString(), mi.Extension.ToString(), true);

            // commands are from same assembly and extension
            return String.Compare(Name, mi.Name, true);
        }


        /// <summary>
        /// Textual representation of the mode item.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format("  {0,-14} {1} [{2}]\r\n", "mode:", Name, (true == Value) ? "ON" : "OFF"));
            sb.Append(string.Format("  {0,-14} {1} \r\n", "description:", Description));

            return sb.ToString();
        }

        ///////////////////////////////////////
        // private helper methods and fields //
        ///////////////////////////////////////
        
        private readonly FieldInfo m_fi;
        private readonly ModeItemDescriptionAttribute m_modeItemDescr;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////
    //----< class ModesExtension >----//
    ////////////////////////////////////

    /// <summary>
    /// Provides an extension method to facilitate the 
    /// textual representation of a list of OrangeModeItems.
    /// </summary>
    public static class ModesExtension
    {
        public static string ToStr(this IModeItemCollection modes)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("The following modes are available : \r\n\r\n");

            foreach (OrangeModeItem mode in modes)
            {
                if (!mode.IsHidden)
                {
                    sb.Append(string.Format("  {0,-14} {1} [{2}]\r\n", "mode:", mode.Name, (true == mode.Value) ? "ON" : "OFF"));
                    sb.Append(string.Format("  {0,-14} {1} \r\n\r\n", "description:", mode.Description));
                }
            }

            return sb.ToString();            
        }

        public static void Reset(this IModeItemCollection modes)
        {
            foreach (OrangeModeItem mode in modes)
                mode.Reset();
        }
    }        

    ///////////////////////////////////////////////////////////////////////////////////////////////

}
