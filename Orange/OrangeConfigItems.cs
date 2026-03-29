///////////////////////////////////////////////////////////////////////////////
// OrangeConfigs.cs  :                                                       //
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
    //----< interface IConfigItemCollection >----//
    ///////////////////////////////////////////////

    /// <summary>
    /// Interface for config-item collections.
    /// </summary>
    public interface IConfigItemCollection : IEnumerable
    {
        /// <summary>
        /// Adds a config item to the collection.
        /// </summary>
        /// <param name="configItem">Config item to add.</param>
        void Add(IConfigItem configItem);

        /// <summary>
        /// Looks up a config in the collection.
        /// </summary>
        /// <param name="configItem">The name of the config item to look up.</param>
        /// <returns>The config item corresponding to the given name.</returns>
        IConfigItem Lookup(string configItem);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////
    //----< class OrangeConfigItemCollection >----//
    ////////////////////////////////////////////////

    public class OrangeConfigItems : IConfigItemCollection
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// Adds Orange! config items to the collection.
        /// </summary>
        /// <param name="configItem"></param>
        public void Add(IConfigItem configItem)
        {
            // pre-condition check
            if (configItem == null)
                throw new ArgumentNullException("configItem");

            // extensions are allowed to override a config item, i.e. this means we will 
            // delete previously defined config items with same name.
            foreach (IConfigItem ci in m_configItems)
            {
                if (ci.Name.Equals(configItem.Name))
                {
                    m_configItems.Remove(ci);
                    break;
                }
            }

            m_configItems.Add(configItem);
            m_needSorting = true;
        }

        /// <summary>
        /// Checks to see if specified Orange! config item already exists in the collection.
        /// </summary>
        /// <param name="configItemName">The name of the Orange! config item.</param>
        /// <returns>The Config item object if it exists in the collection.</returns>
        public IConfigItem Lookup(string configItemName)
        {
            // pre-condition check
            if (null == configItemName)
                throw new ArgumentNullException("configItemName");
            if (0 == configItemName.Length)
                throw new ArgumentException("Cannot pass in zero length string as an argument", "configItemName");

            List<IConfigItem> configItemList = new List<IConfigItem>();
            foreach (IConfigItem ci in m_configItems)
            {
                if (ci.Name.Equals(configItemName, StringComparison.InvariantCultureIgnoreCase))
                    configItemList.Add(ci);
            }

            string exMsg = "\r\nType 'help' to see a complete list of config items.";

            if (configItemList.Count == 0)
                throw new OrangeShellException("Config Item '" + configItemName + "' not found." + exMsg);

            else if (configItemList.Count == 1)
                return (IConfigItem)configItemList[0];

            else
            {
                StringBuilder s = new StringBuilder("prefix too short.\r\n" + "Possible completitions:");
                foreach (IConfigItem ci in configItemList)
                {
                    s.Append("\r\n").Append(ci.Name);
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
                m_configItems.Sort();
            }

            return m_configItems.GetEnumerator();
        }


        ////////////////////////////////
        // private members and fields //
        ////////////////////////////////

        private List<IConfigItem> m_configItems = new List<IConfigItem>();
        private bool m_needSorting = false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
