///////////////////////////////////////////////////////////////////////////////
// OrangeModes.cs    :                                                       //
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

    ///////////////////////////////////////////////
    //----< interface IModeItemCollection >----//
    ///////////////////////////////////////////////

    /// <summary>
    /// Interface for mode-item collections.
    /// </summary>
    public interface IModeItemCollection : IEnumerable
    {
        /// <summary>
        /// Adds a mode item to the collection.
        /// </summary>
        /// <param name="modeItem">Mode item to add.</param>
        void Add(IModeItem modeItem);

        /// <summary>
        /// Looks up a mode in the collection.
        /// </summary>
        /// <param name="modeItem">The name of the mode item to look up.</param>
        /// <returns>The mode item corresponding to the given name.</returns>
        IModeItem Lookup(string modeItem);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////
    //----< class OrangeModeItemCollection >----//
    ////////////////////////////////////////////////

    public class OrangeModeItems : IModeItemCollection
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Adds Orange! mode items to the collection.
        /// </summary>
        /// <param name="modeItem"></param>
        public void Add(IModeItem modeItem)
        {
            // pre-condition check
            if (modeItem == null)
                throw new ArgumentNullException("modeItem");

            // extensions are allowed to override a mode item, i.e. this means we will 
            // delete previously defined mode items with same name.
            foreach (IModeItem mi in m_modeItems)
            {
                if (mi.Name.Equals(modeItem.Name))
                {
                    m_modeItems.Remove(mi);
                    break;
                }
            }

            m_modeItems.Add(modeItem);
            m_needSorting = true;
        }

        /// <summary>
        /// Checks to see if specified Orange! mode item already exists in the collection.
        /// </summary>
        /// <param name="modeItemName">The name of the Orange! mode item.</param>
        /// <returns>The Mode item object if it exists in the collection.</returns>
        public IModeItem Lookup(string modeItemName)
        {
            // pre-condition check
            if (null == modeItemName)
                throw new ArgumentNullException("modeItemName");
            if (0 == modeItemName.Length)
                throw new ArgumentException("Cannot pass in zero length string as an argument", "modeItemName");

            List<IModeItem> modeItemList = new List<IModeItem>();
            foreach (IModeItem mi in m_modeItems)
            {
                if (mi.Name.Equals(modeItemName, StringComparison.InvariantCultureIgnoreCase))
                    modeItemList.Add(mi);
            }

            string exMsg = "\r\nType 'help' to see a complete list of mode items.";

            if (modeItemList.Count == 0)
                throw new OrangeShellException("Mode Item '" + modeItemName + "' not found." + exMsg);

            else if (modeItemList.Count == 1)
                return (IModeItem)modeItemList[0];

            else
            {
                StringBuilder s = new StringBuilder("prefix too short.\r\n" + "Possible completitions:");
                foreach (IModeItem mi in modeItemList)
                {
                    s.Append("\r\n").Append(mi.Name);
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
                m_modeItems.Sort();
            }

            return m_modeItems.GetEnumerator();
        }

        /// <summary>
        /// Adds Orange! mode items (described in the specified type) to the collection.
        /// </summary>
        public void AddModeItemsFromType(Type type)
        {
            foreach (FieldInfo fi in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                if (fi.IsStatic && fi.FieldType == typeof(bool))
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



        ////////////////////////////////
        // private members and fields //
        ////////////////////////////////

        private List<IModeItem> m_modeItems = new List<IModeItem>();
        private bool m_needSorting = false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
