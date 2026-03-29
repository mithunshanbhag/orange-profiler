#pragma once
////////////////////////////////////////////////////////////////////////////////////////
// EntityInfo.h	- Gets detailed info on entities like Objects, classes, fields etc.   //
// Application 	- Orange Profiler.                                                    //
// Author      	- Mithun Shanbhag, mithuns@microsoft.com                              //
////////////////////////////////////////////////////////////////////////////////////////

#include "common.hxx"


/////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////
//----< class declaration : FieldInfo >----//
/////////////////////////////////////////////

class FieldInfo 
{
 public:

    FieldInfo(mdFieldDef, ClassID, ModuleID, set<AppDomainID> &, ICorProfilerInfo3 * const);

	virtual ~ FieldInfo();

    virtual HRESULT Init();  
 
 private:
 
	FieldInfo();

    FieldInfo(const FieldInfo &);

 public:

	// fielddef token
    mdFieldDef m_fieldDefTok;

    mdTypeDef  m_TypeDefTok;

    // ID of the class that contains this field
    ClassID m_ParentClassId;

    // ID of the module that contains the class (which contains this field).
	ModuleID m_ModuleId;

 	// field offset   
    ULONG m_ulFieldOffset;
 
	// field attributes
    DWORD m_dwFieldAttr;

	// name of the field
    WCHAR * m_pwszFieldName;

	// length in chars of the field name
    ULONG m_cchFieldName;

	// field sig
    PCCOR_SIGNATURE m_pvSigBlob;

	// length in bytes of the field sig
    ULONG m_cbSigBlob;
    
    DWORD m_dwCPlusTypeFlag;
    
    UVCP_CONSTANT m_pValue;
    
    ULONG m_cchValue;

	// additional info for static fields

	COR_PRF_STATIC_TYPE m_staticType;

	map<AppDomainID, UINT_PTR> m_AppDomainStaticAddresses;

	vector<UINT_PTR> m_RVAStaticAddresses;


 private:
    
	set<AppDomainID>& m_appdomainIds;

    bool m_bInit;
        
    ICorProfilerInfo3 * m_pInfo;
};




/////////////////////////////////////////////
//----< class declaration : ClassInfo >----//
/////////////////////////////////////////////

class ClassInfo
{
 public:

    ClassInfo(ClassID, ICorProfilerInfo3 * const);

	virtual ~ ClassInfo();

    virtual HRESULT Init();  

 private:
 
    ClassInfo();

	ClassInfo(const ClassInfo &);

    HRESULT InitHelper(ClassID classId);

	HRESULT GetClassName();
    
	HRESULT ClassIsFinalizable(ModuleID, mdToken, bool &);

	HRESULT _ClassHasFinalizeMethod(IMetaDataImport2 *, mdToken, DWORD &, bool &);

	HRESULT _ClassOverridesFinalize(IMetaDataImport2 *, mdToken, bool &);

	HRESULT _ClassReintroducesFinalize(IMetaDataImport2 *, mdToken, bool &);


 public:

	// class id
    ClassID m_ClassId;

	// its typedef token
    mdTypeDef m_TypeDefToken;

	// id of its module
    ModuleID m_ModuleId;

	// id of its parent
    ClassID m_ParentClassId;

    mdToken m_tkExtends;
    

    // generics stuff

    ULONG32 m_cTypeArgs;

    ClassID * m_rTypeArgs;


    // class layout stuff

    ULONG m_ulClassSize;

    ULONG m_cFields;

    std::vector <mdFieldDef> m_FieldToks;

//    FieldInfo * m_pFields;
    
	// length in chars of the type name
    ULONG m_cchTypeName;

	// the name of this type
    WCHAR * m_wszTypeName;

	// the namespace which contains this type
	WCHAR * m_wszNamespace;
    
    DWORD m_dwTypeDefFlags;  

    // can this type be finalized?
    bool m_bIsFinalizable;

    bool m_bIsString;    

    bool m_bIsGeneric;
    
    
 private:

    bool m_bInit;
 
    ICorProfilerInfo3 * const m_pInfo;
};

/////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////
//----< class declaration : ArrayInfo >----//
/////////////////////////////////////////////

class ArrayInfo
{
 public:

    ArrayInfo(ClassID, ICorProfilerInfo3 * const);

	virtual ~ ArrayInfo() {}

    virtual HRESULT Init();  

 private:
 
    ArrayInfo();

	ArrayInfo(const ArrayInfo &);

 public:

	ClassID m_ClassId;

	ClassID m_baseElemClassId;

	CorElementType m_baseElemType;

	ULONG m_cRank;

	// special fields for jagged arrays.

	ULONG m_ulJaggedArrayDepth;

	ClassID m_InnerMostBaseElemClassId;

	CorElementType m_InnerMostBaseElemType;

 private:

    bool m_bInit;
 
    ICorProfilerInfo3 * const m_pInfo;
};

/////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////
//----< class declaration : ModuleInfo >----//
//////////////////////////////////////////////

class ModuleInfo
{
 public:
 
    ModuleInfo(ModuleID, ICorProfilerInfo3 * pInfo);

	virtual ~ ModuleInfo();

    virtual HRESULT Init();  

 private:
 
    ModuleInfo();

	ModuleInfo(const ModuleInfo &);

 public:

	// The module's id
    ModuleID m_ModuleId;

	// size in chars of the module's name
    ULONG m_cchModuleName;

	// module's name
    WCHAR * m_wszModuleName;
    
	// Id of the assembly containing the module
    AssemblyID m_ParentAssemblyId;

private:

    bool m_bInit;
 
    ICorProfilerInfo3 * const m_pInfo;
};

/////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////
//----< class declaration : AssemblyInfo >----//
////////////////////////////////////////////////

class AssemblyInfo
{
 public:
 
    AssemblyInfo(AssemblyID, ICorProfilerInfo3 * pInfo);

	virtual ~ AssemblyInfo();

    virtual HRESULT Init();  

 private:
 
    AssemblyInfo();

	AssemblyInfo(const AssemblyInfo &);

 public:

	// The assembly's id
    AssemblyID m_AssemblyId;

	// Length in chars of the assembly's name
    ULONG m_cchAssemblyName;

	// The assmebly's name
    WCHAR * m_wszAssemblyName;

	// Id of the appdomins in which the assembly is loaded
    AppDomainID m_ParentAppdomainId;

private:

    bool m_bInit;
 
    ICorProfilerInfo3 * const m_pInfo;
};

/////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////
//----< class declaration : AppDomainInfo >----//
/////////////////////////////////////////////////

class AppDomainInfo
{
 public:
 
    AppDomainInfo(AppDomainID, ICorProfilerInfo3 * pInfo);

	virtual ~ AppDomainInfo();

    virtual HRESULT Init();  

 private:
 
    AppDomainInfo();

	AppDomainInfo(const AppDomainInfo &);

 public:

	// The appdomain's ID
	AppDomainID m_AppDomainId;

	// Appdomain's name
    WCHAR * m_wszAppDomainName;

	// length in chars of the Appdomain's name
	ULONG m_cchAppDomainName;

private:

    bool m_bInit;
 
    ICorProfilerInfo3 * const m_pInfo;
};


/////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////
//----< class declaration : FunctionInfo >----//
////////////////////////////////////////////////

class FunctionInfo
{
 public:
 
    FunctionInfo(FunctionID, ICorProfilerInfo3 * pInfo);

	virtual ~ FunctionInfo();

    virtual HRESULT Init();  

 private:
 
    FunctionInfo();

	FunctionInfo(const FunctionInfo &);

 public:

	// The appdomain's ID
	FunctionID m_FunctionId;

	// Appdomain's name
    WCHAR * m_wszFunctionName;

	// length in chars of the Appdomain's name
	ULONG m_cchFunctionName;

	// ID of containing class
	ClassID m_classId;

	// ID of containing module
	ModuleID m_moduleId;

private:

    bool m_bInit;
 
    ICorProfilerInfo3 * const m_pInfo;
};

/////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////
//----< class declaration : ObjectInfo >----//
//////////////////////////////////////////////

class ObjectInfo 
{
 public:
 
    ObjectInfo(ObjectID, ICorProfilerInfo3 * const);

	virtual ~ ObjectInfo() {}

    virtual HRESULT Init();  

    static HRESULT GetAlignedSize(ICorProfilerInfo3 * const pInfo, ObjectID objectId, ULONG * pulAlignedObjSize);

 private:
 
    ObjectInfo();

	ObjectInfo(const ObjectInfo &);

	static ULONG GetAlignedSizeInternal(COR_PRF_GC_GENERATION gen, ULONG cbObjRawSize);

 public:

	// object's ID.
    ObjectID m_ObjectId;
    
	// Raw (unaligned) size of the object on the heap.
    ULONG m_cbObjRawSize; 

	// Aligned size of the object on the heap.
    ULONG m_cbObjAlignedSize;

	// Generation in which the object lies.
	ULONG m_dwGeneration;   
    
 protected:
 
    bool m_bInit;    
    
    ICorProfilerInfo3 * m_pInfo;
};

/////////////////////////////////////////////////////////////////////////////////////////



