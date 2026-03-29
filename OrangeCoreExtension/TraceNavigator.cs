using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using OrangeUtil;

namespace OrangeClient
{
    //////////////////////////////////
    // ----< class TraceHeader >----//
    //////////////////////////////////

    /// <summary>
    /// 
    /// </summary>
    public sealed class TraceHeader
    {
        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        public bool Initialize(XElement header)
        {
            try
            {
                m_bIs64BitProcess = (0 == Convert.ToInt32(header.Element("system").Attribute("is64bit").Value))
                                        ? false
                                        : true;

                m_dwPID = Convert.ToInt32(header.Element("process").Attribute("pid").Value);
                m_strProcessName = header.Element("process").Attribute("name").Value;
                m_strProcessPath = header.Element("process").Attribute("path").Value;
                m_clrInstanceId = Convert.ToInt32(header.Element("runtime").Attribute("instanceid").Value);
                m_clrRuntimeType = header.Element("runtime").Attribute("type").Value;
                m_clrVersionString = header.Element("runtime").Attribute("versionstring").Value;
                m_clrMajorVersion = Convert.ToInt32(header.Element("runtime").Attribute("majorversion").Value);
                m_clrMinorVersion = Convert.ToInt32(header.Element("runtime").Attribute("minorversion").Value);
                m_clrBuildNumber = Convert.ToInt32(header.Element("runtime").Attribute("build").Value);
                m_clrQFEVersion = Convert.ToInt32(header.Element("runtime").Attribute("qfeversion").Value);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Is64BitProcess
        {
            get { return m_bIs64BitProcess; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int PID
        {
            get { return m_dwPID; }
        }

        /// <summary>
        /// 
        /// </summary>        
        public string ProcessName
        {
            get { return m_strProcessName; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ProcessPath
        {
            get { return m_strProcessPath; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ClrInstanceId
        {
            get { return m_clrInstanceId; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ClrRuntimeType
        {
            get { return m_clrRuntimeType; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ClrVersionString
        {
            get { return m_clrVersionString; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ClrMajorVersion
        {
            get { return m_clrMajorVersion; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ClrMinorVersion
        {
            get { return m_clrMinorVersion; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ClrBuildNumber
        {
            get { return m_clrBuildNumber; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ClrQFEVersion
        {
            get { return m_clrQFEVersion; }
        }


        ////////////////////////////////
        // private helpers and fields //
        ////////////////////////////////

        private bool m_bIs64BitProcess;
        private int m_dwPID;
        private string m_strProcessName;
        private string m_strProcessPath;
        private int m_clrInstanceId;
        private string m_clrRuntimeType;
        private string m_clrVersionString;
        private int m_clrMajorVersion;
        private int m_clrMinorVersion;
        private int m_clrBuildNumber;
        private int m_clrQFEVersion;
    }

    ///////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////
    // ----< class TraceNavigator >----//
    /////////////////////////////////////

    /// <summary>
    /// This class is responsible for implementing methods that can be used to navigate through a 
    /// trace file/report. 
    /// NOTE: The traceNavigator class does not conduct much validation on inputs or conduct many 
    /// pre-condition checks. It makes an assumption that valid inputs are being passed in by the caller.
    /// </summary>
    public sealed class TraceNavigator
    {

        ///////////////////////////////////
        // public methods and properties //
        ///////////////////////////////////

        /// <summary>
        /// This command loads the specified trace file for analysis. If a trace file is already loaded when
        /// this command is called, the old trace gets unloaded and the new one is loaded.
        /// 
        /// Note: This method conducts two passes over the xml file. This is because we use an XmlReader (which is 
        /// a forward only iterator) we cannot deduce total number of "snapshot" nodes simply by loading the file.
        /// 
        /// Assumption: All pre-condition checks have been done at this point. Trace file is assumed to be valid. 
        /// </summary>
        /// <param name="file">XML trace file to load.</param>
        /// <param name="xsdfile">Optional xsd file for schema validation. You can pass in null if you 
        /// choose not to validate the schema.</param>
        /// <returns>Returns false if schema validation errors were detected. The trace is not loaded in this 
        /// case. If there are no schema validation errors, then the trace is successfully loaded and the method
        /// returns true.</returns>
        public bool ResetAndLoadTrace(string file, string xsdfile)
        {
            Reset(true);

            try
            {
                LoadTrace(file, xsdfile);
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }

            if (!ReadHeader())
                return false;

            while (null != NextFullSnapshot())
                ++m_cSnapshots;

            if (m_bSchemaValidationErrors)
                return false;

            Reset(false);
            LoadTrace(file, null);

            return true;
        }


        /// <summary>
        /// Unloads any loaded trace file.
        /// <summary>        
        public string UnloadTrace()
        {
            return Reset(true); // name of trace file that was unloaded
        }


        /// <summary>
        ///
        /// <summary>        
        public string TraceFile
        {
            get
            {
                return m_strTraceFileName;
            }
        }


        /// <summary>
        ///
        /// <summary>        
        public int TotalSnapshots
        {
            get
            {
                return m_cSnapshots;
            }
        }


        /// <summary>
        ///
        /// <summary>                
        public int CurrentSnapshotIndex
        {
            get
            {
                return m_iCurrIndex;
            }
        }


        public TraceHeader Header
        {
            get
            {
                return m_traceHeader;
            }
        }

        /// <summary>
        ///
        /// <summary>        
        public /*XElement*/ Snapshot NextFullSnapshot()
        {
            while (m_xmlTraceReader.Read())
            {
                if (m_xmlTraceReader.NodeType == XmlNodeType.Element &&
                    m_xmlTraceReader.Name.Equals("gc") &&
                    m_xmlTraceReader.GetAttribute("cg").Equals("3") &&
                    m_xmlTraceReader.GetAttribute("nl").Equals("1"))
                {
                    ++m_iCurrIndex;

                    m_xmlCurrentSnapshot = (XElement)XElement.ReadFrom(m_xmlTraceReader);

                    return new Snapshot(m_xmlCurrentSnapshot, m_GCHandleTrackingInfo);
                }

                else if (m_xmlTraceReader.NodeType == XmlNodeType.Element &&
                         m_xmlTraceReader.Name.Equals("hc"))
                {
                    XElement xelem = (XElement)XElement.ReadFrom(m_xmlTraceReader);

                    List<UInt64> stack = new List<UInt64>();

                    foreach (var elem in xelem.Elements("f"))
                        stack.Add(Convert.ToUInt64(elem.Attribute("i").Value, 16));

                    Handle hndInfo = new Handle(
                        Convert.ToUInt64((xelem.Attribute("i").Value), 16),
                        Convert.ToUInt64((xelem.Attribute("o").Value), 16),
                        stack
                        );

                    m_GCHandleTrackingInfo.Add(hndInfo);
                }

                else if (m_xmlTraceReader.NodeType == XmlNodeType.Element &&
                         m_xmlTraceReader.Name.Equals("hd"))
                {
                    XElement xelem = (XElement)XElement.ReadFrom(m_xmlTraceReader);

                    for (int idx = 0; idx < m_GCHandleTrackingInfo.Count; idx++)
                    {
                        if (Convert.ToUInt64((xelem.Attribute("i").Value), 16) == m_GCHandleTrackingInfo[idx].HandleId)
                        {
                            m_GCHandleTrackingInfo.RemoveAt(idx);
                            break;
                        }
                    }
                }
            }

            return null;
        }


        /// <summary>
        ///
        /// <summary>        
        public /*XElement*/ Snapshot NextPartialSnapshot()
        {
            while (m_xmlTraceReader.Read())
            {
                if (m_xmlTraceReader.NodeType == XmlNodeType.Element && m_xmlTraceReader.Name.Equals("gc"))
                {
                    ++m_iCurrIndex;

                    m_xmlCurrentSnapshot = (XElement)XElement.ReadFrom(m_xmlTraceReader);

                    return new Snapshot(m_xmlCurrentSnapshot, m_GCHandleTrackingInfo);
                }

                else if (m_xmlTraceReader.NodeType == XmlNodeType.Element &&
                         m_xmlTraceReader.Name.Equals("hc"))
                {
                    XElement xelem = (XElement)XElement.ReadFrom(m_xmlTraceReader);

                    List<UInt64> stack = new List<UInt64>();

                    foreach (var elem in xelem.Elements("f"))
                        stack.Add(Convert.ToUInt64(elem.Value, 16));

                    Handle hndInfo = new Handle(
                        Convert.ToUInt64((xelem.Attribute("i").Value), 16),
                        Convert.ToUInt64((xelem.Attribute("o").Value), 16),
                        stack
                        );

                    m_GCHandleTrackingInfo.Add(hndInfo);
                }

                else if (m_xmlTraceReader.NodeType == XmlNodeType.Element &&
                         m_xmlTraceReader.Name.Equals("hd"))
                {
                    XElement xelem = (XElement)XElement.ReadFrom(m_xmlTraceReader);

                    for (int idx = 0; idx < m_GCHandleTrackingInfo.Count; idx++)
                    {
                        if (Convert.ToUInt64((xelem.Attribute("i").Value), 16) == m_GCHandleTrackingInfo[idx].HandleId)
                        {
                            m_GCHandleTrackingInfo.RemoveAt(idx);
                            break;
                        }
                    }
                }
            }

            return null;
        }



        /// <summary>
        ///
        /// <summary>                
        public /*XElement*/ Snapshot CurrentSnapshot
        {
            get
            {
                if (m_iCurrIndex < 0)
                    return null;


                List<Handle> handleInfos = new List<Handle>();

                return new Snapshot(m_xmlCurrentSnapshot, m_GCHandleTrackingInfo);
            }
        }


        /// <summary>
        ///
        /// <summary>                
        public List<Handle> CurrentHandleTrackingInfo
        {
            get
            {
                return m_GCHandleTrackingInfo;
            }
        }


        /// <summary>
        ///
        /// <summary>                
        public /*XElement*/ Snapshot GotoSnapshot(int index)
        {
            if (index >= m_cSnapshots || index < 0)
                return null;

            m_strTraceFileName = Reset(false);

            LoadTrace(m_strTraceFileName, null);

            while (m_iCurrIndex < index - 1)
                NextFullSnapshot();

            return NextFullSnapshot();
        }


        ///////////////////////////////////////
        // private fields and helper methods //
        ///////////////////////////////////////

        private enum TraceType
        {
            MEMORY_TRACE,
            RESERVED
        }

        private string m_strTraceFileName;
        private XmlReader m_xmlTraceReader;
        private XmlReaderSettings m_xmlReaderSettings;
        private TraceHeader m_traceHeader;
        private bool m_bSchemaValidationErrors;
        private XElement m_xmlCurrentSnapshot;
        private int m_cSnapshots;
        private int m_iCurrIndex;
        private List<Handle> m_GCHandleTrackingInfo;


        /// <summary>
        /// Reloads the trace file and resets the iterator. Returns the names of the trace 
        /// file (on which the xmlreader was just closed) and the xsd schema file in a tuple.
        /// </summary>
        private string Reset(bool bFullReset)
        {
            if (null != m_xmlTraceReader)
            {
                m_xmlTraceReader.Close();
                m_xmlTraceReader = null;
            }

            m_xmlCurrentSnapshot = null;
            m_iCurrIndex = -1;

            if (bFullReset)
            {
                m_cSnapshots = 0;
                m_xmlReaderSettings = null;
                m_bSchemaValidationErrors = false;
                m_traceHeader = new TraceHeader();
            }

            m_GCHandleTrackingInfo = null;

            string tempholder_TraceFileName = m_strTraceFileName;
            m_strTraceFileName = string.Empty;

            return tempholder_TraceFileName;
        }


        /// <summary>
        /// <summary>
        private void LoadTrace(string xmlfile, string xsdfile)
        {
            if (null == m_xmlReaderSettings && !string.IsNullOrEmpty(xsdfile))
            {
                m_xmlReaderSettings = new XmlReaderSettings();
                m_xmlReaderSettings.Schemas.Add(null, xsdfile);
                m_xmlReaderSettings.ValidationType = ValidationType.Schema;
                m_xmlReaderSettings.ValidationEventHandler += new ValidationEventHandler(ValidationHandler);
            }

            m_xmlTraceReader = (null == xsdfile) ? XmlReader.Create(xmlfile) : XmlReader.Create(xmlfile, m_xmlReaderSettings);
            m_xmlTraceReader.MoveToContent();
            m_strTraceFileName = xmlfile;

            m_GCHandleTrackingInfo = new List<Handle>();
        }


        /// <summary>
        ///
        /// <summary>        
        private bool ReadHeader()
        {
            while (m_xmlTraceReader.Read())
            {
                if ((m_xmlTraceReader.NodeType == XmlNodeType.Element) && (m_xmlTraceReader.Name.Equals("header")))
                {
                    return m_traceHeader.Initialize((XElement)XElement.ReadFrom(m_xmlTraceReader));
                }
            }
            return false;
        }


        /// <summary>
        /// Displays any warnings or errors while validating xml trace file against xsd schema.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ValidationHandler(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
                m_bSchemaValidationErrors = true;
        }

    }

    ///////////////////////////////////////////////////////////////////////////

}