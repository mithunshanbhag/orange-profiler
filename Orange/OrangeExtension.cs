//////////////////////////////////////////////////////////////////////////////
// OrangeExtension.cs :                                                     //
// Application        : CLR V4 Profiler Test Infrastructure                 //
// Author             : Mithun Shanbhag                                     //
//                      Some parts of this module have been lifted directly //
//                      from the MDbg sources.                              //
//////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace OrangeClient
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////
    //----< interface IExtension >----//
    ////////////////////////////////////

    /// <summary>
    /// 
    /// </summary>
    public interface IExtension : IComparable<IExtension>
    {
        /// <summary>
        /// Name of the extension.
        /// </summary>
        string ExtensionName
        {
            get;
        }

        /// <summary>
        /// Name of extension author.
        /// </summary>
        string AuthorName
        {
            get;
        }

        /// <summary>
        /// Email of extension author.
        /// </summary>
        string AuthorEmail
        {
            get;
        }

        /// <summary>
        /// Assembly in which this extension is described
        /// </summary>
        Assembly Assembly
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        Type Type
        {
            get;                
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ///////////////////////////////////////////////////
    //----< class ExtensionDescriptionAttribute >----//
    ///////////////////////////////////////////////////

    /// <summary>
    /// This attribute describes an Orange! extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ExtensionDescriptionAttribute : Attribute
    {

        /// <summary>
        /// 
        /// </summary>
        public string AuthorName
        {
            get { return m_authorName; }
            set { m_authorName = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AuthorEmail
        {
            get { return m_authorEmail; }
            set { m_authorEmail = value; }
        }
    
        ////////////////////////////////
        // private members and fields //
        ////////////////////////////////

        private string m_authorName;
        private string m_authorEmail;
                
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////
    //----< class OrangeExtension >----//
    /////////////////////////////////////

    public sealed class OrangeExtension : IExtension
    {
        public OrangeExtension(Type describingType, ExtensionDescriptionAttribute descriptionAttribute)
        {
            // pre-condition checks 
            if (null == describingType)
                throw new ArgumentNullException("describingType");
            if (null == descriptionAttribute)
                throw new ArgumentNullException("descriptionAttribute");

            m_describingType = describingType;
            m_descAttr = descriptionAttribute;
        }

        public string ExtensionName
        {
            get { return m_describingType.Name; }
        }

        public string AuthorName
        {
            get { return m_descAttr.AuthorName; }
        }

        public string AuthorEmail
        {
            get { return m_descAttr.AuthorEmail; }
        }

        public Assembly Assembly
        {
            get { return m_describingType.Assembly; }
        }

        public Type Type
        {
            get { return m_describingType; }
        }


        public int CompareTo(IExtension extension)
        {
             if (!this.Assembly.Equals(extension.Assembly))
                return string.Compare(this.Assembly.ToString(), extension.Assembly.ToString(), true);

            return string.Compare(this.ExtensionName, extension.ExtensionName, true);
        }

        ////////////////////////////////
        // private members and fields //
        ////////////////////////////////

        private Type m_describingType;
        private ExtensionDescriptionAttribute m_descAttr;
    }        
}
