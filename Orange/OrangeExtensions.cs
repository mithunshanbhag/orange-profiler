/////////////////////////////////////////////////////////////////////////////
// OrangeExtensions.cs:                                                    //
// Application        : CLR V4 Profiler Test Infrastructure                //
// Author             : Mithun Shanbhag                                    //
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace OrangeClient
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////
    //----< interface ICommandCollection >----//
    ////////////////////////////////////////////

    /// <summary>
    /// Interface for extension collections.
    /// </summary>
    public interface IExtensionCollection : IEnumerable
    {
        /// <summary>
        /// Adds an extension to the collection.
        /// </summary>
        /// <param name="command">Extension to add.</param>
        void Add(IExtension extension);

        /// <summary>
        /// Looks up an extension in the collection.
        /// </summary>
        /// <param name="extension">The name of the extension to look up.</param>
        /// <returns>The specified extension, if found.</returns>
        IExtension Lookup(string extension);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyPath"></param>
        void LoadExtension(string extensionPath);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////
    //----< class OrangeExtensions >----//
    //////////////////////////////////////

    public class OrangeExtensions : IExtensionCollection
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Adds Orange! extensions to the collection.
        /// </summary>
        /// <param name="command"></param>
        public void Add(IExtension extension)
        {
            // pre-condition check
            if (extension == null)
                throw new ArgumentNullException("extension");

            // check if the extension has already been loaded.
            bool bFound = false;
            foreach (IExtension ext in m_extensions)
            {
                if (0 == ext.CompareTo(extension))
                    bFound = true;
            }

            if (!bFound)
            {
                m_extensions.Add(extension);
                m_needSorting = true;
            
                AddCommandsFromType(extension.Type);
            }
            else
                throw new OrangeShellException("Extension has already been loaded!"); 
        }

        /// <summary>
        /// Checks to see if specified Orange! extension already exists in the collection.
        /// </summary>
        /// <param name="extensionName">The name of the Orange! extension.</param>
        /// <returns>The extension object if it exists in the collection.</returns>
        public IExtension Lookup(string extensionName)
        {
            // pre-condition check
            if (null == extensionName)
                throw new ArgumentNullException("extensionName");
            if (0 == extensionName.Length)
                throw new ArgumentException("Cannot pass in zero length string as an argument", "extensionName");

            List<IExtension> extList = new List<IExtension>();
            foreach (IExtension ext in m_extensions)
            {
                if (ext.ExtensionName.Equals(extensionName))
                    extList.Add(ext);
            }

            string exMsg = "\r\nType 'help' to see a complete list of extensions.";

            if (extList.Count == 0)
                throw new OrangeShellException("Extension '" + extensionName + "' not found." + exMsg);

            else if (extList.Count == 1)
                return (IExtension)extList[0];

            else
            {
                StringBuilder s = new StringBuilder("Several possible extensions found:");
                foreach (IExtension e in extList)
                {
                    s.Append("\r\n").Append(e.ExtensionName);
                }
                throw new OrangeShellException(s.ToString());
            }
        }


        /// <summary>
        /// Assumptions: caller is passing in a the correct path to a valid assembly.
        /// </summary>
        /// <param name="assembly"></param>
        public void LoadExtension(string assembly)
        {
            if (string.IsNullOrEmpty(assembly))
                throw new ArgumentException("Cannot pass in a null or zero-length string as argument.", "assembly");

            Assembly extension = Assembly.LoadFrom(assembly);
            foreach (Type t in extension.GetTypes())
            {
                // If any type describes a ExtensionDesc attribute, we'll scan it for commands.
                object[] attribs = t.GetCustomAttributes(typeof(ExtensionDescriptionAttribute), false);
                if (attribs != null)
                {
                    foreach (object o in attribs)
                    {
                        if (o is ExtensionDescriptionAttribute)
                        {
                            OrangeExtension ext = new OrangeExtension(t, (ExtensionDescriptionAttribute)o);
                            Orange.Extensions.Add(ext);
                            ext.Type.GetConstructor(new Type[]{}).Invoke(null);                            
                        }
                    }
                }
                
                // Now we'll scan the type for any modes & configs
                AddConfigAndModeItemsFromType(t);
            }
        }

        /// <summary>
        /// Adds Orange! commands (described in the specified type) to the collection.
        /// </summary>
        /// <param name="commandSet">Command Set to add.</param>
        /// <param name="type">Type to add commands for.</param>
        public void AddCommandsFromType(Type type)
        {
            foreach (MethodInfo mi in type.GetMethods())
            {
                object[] attribs = mi.GetCustomAttributes(typeof(CommandDescriptionAttribute), false);
                if (attribs != null)
                {
                    foreach (object o in attribs)
                    {
                        if (o is CommandDescriptionAttribute)
                        {
                            Orange.Commands.Add(new OrangeCommand(mi, (CommandDescriptionAttribute)o));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds Orange! config & mode items (described in the specified type) to the collection.
        /// </summary>
        public void AddConfigAndModeItemsFromType(Type type)
        {
            foreach (FieldInfo fi in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                if (fi.IsStatic)
                {
                    if (fi.FieldType == typeof(string))
                    {
                        object[] attribs = fi.GetCustomAttributes(typeof(ConfigItemDescriptionAttribute), false);
                        if (null != attribs)
                        {
                            foreach (object o in attribs)
                            {
                                if (o is ConfigItemDescriptionAttribute)
                                {
                                    Orange.ConfigItems.Add(new OrangeConfigItem(fi, (ConfigItemDescriptionAttribute)o));
                                }
                            }
                        }
                    }

                    else if (fi.FieldType == typeof(bool))
                    {
                        object[] attribs = fi.GetCustomAttributes(typeof(ModeItemDescriptionAttribute), false);
                        if (null != attribs)
                        {
                            foreach (object o in attribs)
                            {
                                if (o is ModeItemDescriptionAttribute)
                                {
                                    Orange.ModeItems.Add(new OrangeModeItem(fi, (ModeItemDescriptionAttribute)o));
                                }
                            }
                        }
                    }
                }
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
                m_extensions.Sort();
            }

            return m_extensions.GetEnumerator();
        }

        ////////////////////////////////
        // private members and fields //
        ////////////////////////////////

        private List<IExtension> m_extensions = new List<IExtension>();
        private bool m_needSorting = false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////




}
