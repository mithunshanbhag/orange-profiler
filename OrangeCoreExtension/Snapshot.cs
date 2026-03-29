using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic;
//using System.Linq.Parallel;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using Win32Wrapper;
using CorProfWrapper;
using OrangeUtil;


namespace OrangeClient
{
    ////////////////////////////////////
    // ----< class ClassComparer >----//
    ////////////////////////////////////

    public class ClassComparer : IEqualityComparer<XElement>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(XElement x, XElement y)
        {
            return (x.Attribute("i").Value.Equals(y.Attribute("i").Value)
                    && x.Attribute("m").Value.Equals(y.Attribute("m").Value)
                    && x.Attribute("n").Value.Equals(y.Attribute("n").Value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public int GetHashCode(XElement c)
        {
            return c.Attribute("i").Value.GetHashCode();
        }
    }

    ////////////////////////////
    // ----< class Class >----//
    ////////////////////////////

    #region Class

    public class Class : IEquatable<Class>, IComparable<Class>
    {
        /// <summary>
        /// /
        /// </summary>
        /// <param name="c"></param>
        public Class(XElement c, Snapshot snapshot)
        {
            m_classId = Convert.ToUInt64((c.Attribute("i").Value), 16);
            m_moduleId = Convert.ToUInt64((c.Attribute("m").Value), 16);
            m_size = Convert.ToUInt64((c.Attribute("s").Value), 16);
            m_ClassName = c.Attribute("n").Value;
            m_bIsFinalizable = (0 == Convert.ToUInt64((c.Attribute("f").Value), 16)) ? false : true;

            m_snapshot = snapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ClassId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_classId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ModuleId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_moduleId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 Size
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_size;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string ClassName
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_ClassName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Int64 TotalBytes
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (Int64)(from objInfo in Objects.AsParallel<ObjectInfo>()
                               select (Int64)objInfo.AlignedSize).Sum();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Int64 TotalInstances
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (Int64)Objects.Count();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> Objects
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in m_snapshot.Objects.AsParallel<ObjectInfo>()
                        where objInfo.ClassId == m_classId
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> PinnedObjects
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        from rootref in m_snapshot.RootReferences.AsParallel<RootReference>()
                        where (objInfo.ObjectId == rootref.ReferencedObjectId
                                  && rootref.RootKind == (UInt64)COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_HANDLE
                                  && rootref.RootFlag == (UInt64)COR_PRF_GC_ROOT_FLAGS.COR_PRF_GC_ROOT_PINNING)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> FReachableObjects
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        from rootref in m_snapshot.RootReferences.AsParallel<RootReference>()
                        where (objInfo.ObjectId == rootref.ReferencedObjectId
                                && rootref.RootKind == (UInt64)COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_FINALIZER)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Module Module
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from module in m_snapshot.Modules
                        where module.ModuleId == m_moduleId
                        select module).Single();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Function> JittedFunctions  
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from function in m_snapshot.FunctionInfos
                        where function.ClassId == m_classId
                        select function);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsFinalizable
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_bIsFinalizable;
            }
        }


        /// <summary>
        /// Implementing the IEquatable<T> interface
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        public bool Equals(Class ci)
        {
            return (null == ci)
                ? false
                : (this.ClassId == ci.ClassId
                  && this.ModuleId == ci.ModuleId
                  && this.ClassName.Equals(ci.ClassName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Overriding the Object.Equals while implementing the IEquatable<T> interface
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (null == obj)
                return false;

            return (obj is Class)
                ? this.Equals(obj)
                : false;
        }

        /// <summary>
        /// Overriding Object.GetHashCode() while implementing the IEquatable<T> interface
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.ClassName.GetHashCode();
        }

        /// <summary>
        /// Implementing the IComparable<T> interface
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        public int CompareTo(Class ci)
        {
            if (!(this.ClassId == ci.ClassId))
                return this.ClassId.CompareTo(ci.ClassId);

            if (!(this.ClassName.Equals(ci.ClassName)))
                return string.Compare(this.ClassName, ci.ClassName, StringComparison.InvariantCultureIgnoreCase);

            return this.ModuleId.CompareTo(ci.ModuleId);
        }


        private UInt64 m_classId;
        private UInt64 m_moduleId;
        private UInt64 m_size;
        private string m_ClassName;
        private bool m_bIsFinalizable;
        private Snapshot m_snapshot;
    }

    #endregion // Class

    ///////////////////////////////////////////////////////////////////////////

    ////////////////////////////
    // ----< class Module>----//
    ////////////////////////////

    #region Module

    public class Module
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleinfo"></param>
        public Module(XElement module, Snapshot snapshot)
        {
            m_moduleId = Convert.ToUInt64((module.Attribute("i").Value), 16);
            m_moduleFullPath = module.Attribute("n").Value;
            m_assemblyId = Convert.ToUInt64((module.Attribute("as").Value), 16);

            m_snapshot = snapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ModuleId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_moduleId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public String ModuleFullPath
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_moduleFullPath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public String ModuleName
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return System.IO.Path.GetFileName(m_moduleFullPath);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 AssemblyId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_assemblyId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Class> Classes
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from c in m_snapshot.Classes
                        where c.ModuleId == m_moduleId
                        select c);
            } 
        }


        private UInt64 m_moduleId;
        private string m_moduleFullPath;
        private UInt64 m_assemblyId;
        private Snapshot m_snapshot;
    }

    #endregion // Module

    ///////////////////////////////////////////////////////////////////////////


    ////////////////////////////////////
    // ----< class RootReference >----//
    ////////////////////////////////////

    #region RootReference

    public class RootReference
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootref"></param>
        public RootReference(XElement rootref, Snapshot snapshot)
        {
            m_rootId = Convert.ToUInt64((rootref.Attribute("ro").Value), 16);
            m_refId = Convert.ToUInt64((rootref.Attribute("re").Value), 16);
            m_rootKind = Convert.ToUInt64((rootref.Attribute("k").Value), 16);
            m_rootFlag = Convert.ToUInt64((rootref.Attribute("f").Value), 16);

            m_snapshot = snapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 RootId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_rootId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ReferencedObjectId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_refId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 RootKind
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_rootKind;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 RootFlag
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_rootFlag;
            }
        }


        private UInt64 m_rootId;
        private UInt64 m_refId;
        private UInt64 m_rootKind;
        private UInt64 m_rootFlag;
        private Snapshot m_snapshot;
    }

    #endregion // RootReference

    ///////////////////////////////////////////////////////////////////////////

    //////////////////////////////
    // ----< class VARange >----//
    //////////////////////////////

    #region VARange

    public class VARange
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="range"></param>
        public VARange(XElement range, Snapshot snapshot)
        {
            m_baseAddress = Convert.ToUInt64((range.Attribute("b").Value), 16);
            m_regionSize = Convert.ToUInt64((range.Attribute("r").Value), 16);
            m_memoryState = Convert.ToUInt64((range.Attribute("s").Value), 16);
            m_memoryProtection = Convert.ToUInt64((range.Attribute("p").Value), 16);
            m_memoryType = Convert.ToUInt64((range.Attribute("t").Value), 16);

            m_snapshot = snapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 BaseAddress
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_baseAddress;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 RegionSize
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_regionSize;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 MemoryState
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_memoryState;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 MemoryProtection
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_memoryProtection;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 MemoryType
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_memoryType;
            }
        }


        private UInt64 m_baseAddress;
        private UInt64 m_regionSize;
        private UInt64 m_memoryState;
        private UInt64 m_memoryProtection;
        private UInt64 m_memoryType;
        private Snapshot m_snapshot;
    }

    #endregion // VARange

    ///////////////////////////////////////////////////////////////////////////

    /////////////////////////////////
    // ----< class ObjectInfo >----//
    /////////////////////////////////

    #region ObjectInfo

    public class ObjectInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objInfo"></param>
        /// <param name="snapshot"></param>
        public ObjectInfo(XElement objInfo, Snapshot snapshot)
        {
            m_objectId = Convert.ToUInt64(objInfo.Attribute("i").Value, 16);
            m_classId = Convert.ToUInt64(objInfo.Attribute("c").Value, 16);
            m_size = Convert.ToUInt64(objInfo.Attribute("s").Value, 16);
            m_generationId = Convert.ToUInt64(objInfo.Attribute("g").Value, 16);

            foreach (var refObj in objInfo.Elements("r"))
            {
                m_refObjs.Add(Convert.ToUInt64(refObj.Value, 16));
            }

            m_snapshot = snapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ObjectId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_objectId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ClassId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_classId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Class Class
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from c in m_snapshot.Classes
                        where (c.ClassId == m_classId)
                        select c).Single();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 HeapId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from generation in m_snapshot.Generations
                        where (m_generationId == generation.GenerationId 
                               && m_objectId >= generation.RangeStart 
                               && m_objectId < generation.RangeStart + generation.RangeLength)
                        select generation.HeapId).Single();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 AlignedSize
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_size;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 GenerationId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_generationId;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public Generation Generation
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from generation in m_snapshot.Generations
                        where (generation.GenerationId == m_generationId
                                && m_objectId >= generation.RangeStart 
                                && m_objectId + AlignedSize <= generation.RangeStart + generation.RangeLength)
                        select generation).Single();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public bool IsPinned
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsFree
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (0 == m_classId);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsFReachable
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                throw new NotImplementedException();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public List<UInt64> ReferencedObjects
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_refObjs;
            }
        }


        private UInt64 m_objectId;
        private UInt64 m_classId;
        private UInt64 m_size;
        private UInt64 m_generationId;
        private List<UInt64> m_refObjs = new List<UInt64>();
        private Snapshot m_snapshot;
    }

    #endregion // ObjectInfo

    ///////////////////////////////////////////////////////////////////////////

    //////////////////////////////
    //----< class Function >----//
    //////////////////////////////

    public class Function
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleinfo"></param>
        public Function(XElement function, Snapshot snapshot)
        {
            m_classId = Convert.ToUInt64((function.Attribute("c").Value), 16);
            m_moduleId = Convert.ToUInt64((function.Attribute("m").Value), 16);
            m_functionId = Convert.ToUInt64((function.Attribute("i").Value), 16);
            m_functionName = function.Attribute("n").Value;

            m_snapshot = snapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 FunctionId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_functionId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ClassId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_classId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ModuleId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_moduleId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RawFunctionName
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_functionName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ResolvedFunctionName
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("{0}!{1}::{2}", Module.ModuleName, Class.ClassName, m_functionName);

                return sb.ToString();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public Module Module
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from module in m_snapshot.Modules
                                       where (module.ModuleId == m_moduleId)
                                       select module).Single();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Class Class
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from c in m_snapshot.Classes
                               where (c.ClassId == m_classId)
                               select c).Single();
            }
        }

        private UInt64 m_functionId;
        private UInt64 m_classId;
        private UInt64 m_moduleId;
        private string m_functionName;
        private Snapshot m_snapshot;
    }

    ///////////////////////////////////////////////////////////////////////////

    ////////////////////////////
    //----< class Handle >----//
    ////////////////////////////

    public class Handle
    {
        public Handle(UInt64 handleId, UInt64 ObjectId, List<UInt64> stacktrace)
        {
            m_handleId = handleId;
            m_objectId = ObjectId;
            m_rawstackTrace = stacktrace;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 HandleId
        {
            get { return m_handleId; }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ObjectId
        {
            get { return m_objectId; }
        }

        public ObjectInfo Object
        {
            get { throw new NotImplementedException();  } 
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<UInt64> RawStackTrace
        {
            get { return m_rawstackTrace; }
        }

        private UInt64 m_handleId;
        private UInt64 m_objectId;
        private List<UInt64> m_rawstackTrace;
    }

    ///////////////////////////////////////////////////////////////////////////

    ////////////////////////////
    //// ----< class Obj >----//
    ////////////////////////////

    //#region Obj

    //public class Obj : IEquatable<Obj>, IComparable<Obj>
    //{ 
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    public Obj(XElement obj)
    //    {
    //        m_id = Convert.ToUInt64(obj.Attribute("i").Value, 16);
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public UInt64 Id
    //    {
    //        get { return m_id; }
    //    }


    //    /// <summary>
    //    /// Implementing the IEquatable<T> interface
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    /// <returns></returns>
    //    public bool Equals(Obj obj)
    //    {
    //        return (null == obj) 
    //            ? false
    //            : (this.Id == obj.Id
    //                && this.GenerationId == obj.GenerationId
    //                && this.AlignedSize == obj.AlignedSize);
    //    }

    //    /// <summary>
    //    /// Overriding Object.Equals while implementing the IEquatable<T> interface
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    /// <returns></returns>
    //    public override bool Equals(object obj)
    //    {
    //        if (null == obj)
    //            return false;

    //        return (obj is Obj)
    //            ? this.Equals(obj)
    //            : false;
    //    }

    //    /// <summary>
    //    /// Overriding Object.GetHashCode while implementing the IEquatable<T> interface
    //    /// </summary>
    //    /// <returns></returns>
    //    public override int GetHashCode()
    //    {
    //        return this.Id.GetHashCode();
    //    }

    //    /// <summary>
    //    /// Implementing the IComparable<T> interface
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    /// <returns></returns>
    //    public int CompareTo(Obj obj)
    //    {
    //        if (!(this.Id == obj.Id))
    //            return this.Id.CompareTo(obj.Id);

    //        if (!(this.GenerationId == obj.GenerationId))
    //            return this.GenerationId.CompareTo(obj.GenerationId);

    //        return this.AlignedSize.CompareTo(obj.AlignedSize);
    //    }


    //    private UInt64 m_id;
    //}

    //#endregion // Obj

    ///////////////////////////////////////////////////////////////////////////

    /////////////////////////////////
    // ----< class Generation >----//
    /////////////////////////////////

    #region Generation

    public class Generation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="generation"></param>
        public Generation(XElement generation, Snapshot snapshot)
        {
            m_generationId = Convert.ToUInt64(generation.Attribute("g").Value.Substring(2), 16);
            m_heapId = Convert.ToUInt64(generation.Attribute("h").Value.Substring(2), 16);
            m_rangeStart = Convert.ToUInt64(generation.Attribute("rs").Value.Substring(2), 16);
            m_rangeLength = Convert.ToUInt64(generation.Attribute("rl").Value.Substring(2), 16);
            m_rangeLengthReserved = Convert.ToUInt64(generation.Attribute("rlr").Value.Substring(2), 16);

            m_snapshot = snapshot;
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 GenerationId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_generationId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 HeapId
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_heapId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 RangeStart
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_rangeStart;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 RangeLength
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_rangeLength;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt64 RangeLengthReserved
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_rangeLengthReserved;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> Objects
        {
            get 
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in m_snapshot.Objects.AsParallel<ObjectInfo>()
                        where (objInfo.GenerationId == m_generationId
                               && objInfo.HeapId == m_heapId
                               && objInfo.ObjectId >= m_rangeStart
                               && objInfo.ObjectId + objInfo.AlignedSize <= m_rangeStart + m_rangeLength)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> FreeObjects
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        where (0 == objInfo.ClassId)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> PinnedObjects
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        from rootref in m_snapshot.RootReferences.AsParallel<RootReference>()
                        where (objInfo.ObjectId == rootref.ReferencedObjectId
                                  && rootref.RootKind == (UInt64)COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_HANDLE
                                  && rootref.RootFlag == (UInt64)COR_PRF_GC_ROOT_FLAGS.COR_PRF_GC_ROOT_PINNING)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> FReachableObjects
        {
            get
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        from rootref in m_snapshot.RootReferences.AsParallel<RootReference>()
                        where (objInfo.ObjectId == rootref.ReferencedObjectId
                                && rootref.RootKind == (UInt64)COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_FINALIZER)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Fragmentation
        {
            get 
            {
                if (!m_snapshot.IsInitialized)
                    throw new SnapshotNotInitializedException();

                var NumofAllObjsInGen = Objects.Count();

                var NumofFreeObjsInGen = FreeObjects.Count();

                return (0 == NumofAllObjsInGen) 
                            ? 0 
                            : (NumofFreeObjsInGen * 100) / NumofAllObjsInGen;
            }
        }

        private UInt64 m_generationId;
        private UInt64 m_heapId;
        private UInt64 m_rangeStart;
        private UInt64 m_rangeLength;
        private UInt64 m_rangeLengthReserved;
        private Snapshot m_snapshot;
    }

    #endregion // Generation

    ///////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////
    //----< class SnapshotNotInitializedException >----//
    /////////////////////////////////////////////////////

    /// <summary>
    /// This exception is thrown when an attempt is made to query an uninitialized snapshot.
    /// </summary>
    [Serializable()]
    public class SnapshotNotInitializedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the OrangeShellException class.
        /// </summary>
        public SnapshotNotInitializedException()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the SnapshotNotInitializedException class with a 
        /// specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SnapshotNotInitializedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SnapshotNotInitializedException class with a 
        /// specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The inner exception for the new exception</param>
        public SnapshotNotInitializedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    ///////////////////////////////////////////////////////////////////////////

    ///////////////////////////////
    // ----< class Snapshot >----//
    ///////////////////////////////

    #region Snapshot

    public sealed class Snapshot
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="snapshot"></param>
        public Snapshot(XElement snapshot, List<Handle> handleInfos)
        {
            foreach (var gen in snapshot.Elements("gen"))
                m_generations.Add(new Generation(gen, this));

            foreach (var objInfo in snapshot.Elements("o"))
                m_objectInfos.Add(new ObjectInfo(objInfo, this));

            foreach (var va_range in snapshot.Elements("va"))
                m_va_ranges.Add(new VARange(va_range, this));

            foreach (var rootRef in snapshot.Elements("rr"))
                m_rootRefs.Add(new RootReference(rootRef, this));

            foreach (var c in snapshot.Elements("c").Distinct(new ClassComparer()))
                m_classes.Add(new Class(c, this));

            foreach (var module in snapshot.Elements("m"))
                m_modules.Add(new Module(module, this));

            foreach (var functionInfo in snapshot.Elements("j"))
                m_functionInfos.Add(new Function(functionInfo, this));

            if (handleInfos != null)
            {
                m_handleInfos = handleInfos;
            }

            m_bInitialized = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> Objects
        {
            get { return m_objectInfos; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> PinnedObjects
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        from rootref in RootReferences.AsParallel<RootReference>()
                        where (objInfo.ObjectId == rootref.ReferencedObjectId
                                  && rootref.RootKind == (UInt64)COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_HANDLE
                                  && rootref.RootFlag == (UInt64)COR_PRF_GC_ROOT_FLAGS.COR_PRF_GC_ROOT_PINNING)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> FReachableObjects
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        from rootref in RootReferences.AsParallel<RootReference>()
                        where (objInfo.ObjectId == rootref.ReferencedObjectId
                                && rootref.RootKind == (UInt64)COR_PRF_GC_ROOT_KIND.COR_PRF_GC_ROOT_FINALIZER)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ObjectInfo> FreeObjects
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return (from objInfo in Objects.AsParallel<ObjectInfo>()
                        where (0 == objInfo.ClassId)
                        select objInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Generation> Generations
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_generations;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<VARange> VirtualAddressRanges
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_va_ranges;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<RootReference> RootReferences
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_rootRefs;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Class> Classes
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_classes;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Module> Modules
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_modules;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Function> FunctionInfos
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_functionInfos;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Handle> HandleInfos
        {
            get
            {
                if (!IsInitialized)
                    throw new SnapshotNotInitializedException();

                return m_handleInfos;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsInitialized
        {
            get { return m_bInitialized; }
        }


        private List<Generation> m_generations = new List<Generation>();
        private List<ObjectInfo> m_objectInfos = new List<ObjectInfo>();
        private List<VARange> m_va_ranges = new List<VARange>();
        private List<RootReference> m_rootRefs = new List<RootReference>();
        private List<Class> m_classes = new List<Class>();
        private List<Module> m_modules = new List<Module>();
        private List<Function> m_functionInfos = new List<Function>();
        private List<Handle> m_handleInfos = new List<Handle>();
        private bool m_bInitialized = false;
    }

    #endregion // Snapshot

    ///////////////////////////////////////////////////////////////////////////

}