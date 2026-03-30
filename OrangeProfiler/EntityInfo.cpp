////////////////////////////////////////////////////////////////////////////////////////
// EntityInfo.cpp - Gets detailed info on entities like Objects, classes, fields etc. //
// Application 	  - Orange Profiler.                                                  //
// Author      	  - Mithun Shanbhag                                                   //
////////////////////////////////////////////////////////////////////////////////////////

#include "EntityInfo.h"

/////////////////////////////////////////////////////////////////////////////////////////

//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

ClassInfo::ClassInfo(ClassID classId, ICorProfilerInfo3 * const pInfo)
:   m_ClassId       (classId),
    m_TypeDefToken  (0),
    m_ModuleId      (0),
    m_ParentClassId (0),
    m_tkExtends     (0),
    m_cTypeArgs     (0),
    m_rTypeArgs     (NULL),
    m_ulClassSize   (0),
    m_cFields       (0),
    m_cchTypeName   (0),
    m_wszTypeName   (NULL),
    m_dwTypeDefFlags(0),
	m_bIsFinalizable(false),
    m_bIsString     (false),
    m_bIsGeneric    (false),
    m_bInit         (false),
    m_pInfo         (pInfo)
{
	m_FieldToks.clear();
}


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

ClassInfo::~ClassInfo()
{
    if (NULL != m_rTypeArgs) {
        delete[] m_rTypeArgs;
        m_rTypeArgs = NULL;
    }
    
    if (NULL != m_wszTypeName) {
        delete[] m_wszTypeName;
        m_wszTypeName = NULL;
    }    

	m_FieldToks.clear();
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ClassInfo::Init()
{
    HRESULT hr = S_OK;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> m_pInfo");
        goto ErrReturn;
    }

    // first and foremost we have to check if the classID refers to an array
    hr = m_pInfo->IsArrayClass(m_ClassId, NULL, NULL, NULL);
    if (S_OK == hr) 
    {
		hr = CORPROF_E_CLASSID_IS_ARRAY;
		goto ErrReturn; 
    }

    else if (S_FALSE != hr)
    {
        TRACEERROR(hr, L"ICorProfilerInfo::IsArrayClass");
        goto ErrReturn;
    }
    
    hr = InitHelper(m_ClassId);
    HR_CHECK(hr, S_OK, L"ClassInfo::InitHelper");
      
    /////////////////////////////////////////////////
ErrReturn:
     
	m_bInit = (S_OK == hr) ? true : false;
    
    return hr;
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ClassInfo::InitHelper(ClassID classId)
{
    HRESULT hr = S_OK;
    HCORENUM hEnum = NULL;
    IMetaDataImport2 * pMDImport = NULL;
    mdFieldDef fieldTok;
    COR_FIELD_OFFSET * rFieldOffsets = NULL;
    DWORD dwLayoutFields = 0;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> m_pInfo");
        goto ErrReturn;
    }

    // We retrive some class info here 
    // - module ID 
    // - typedef token
    // - Parent's class ID
    // However we need to determine if the class has any generic type args. Hence we need to do two passes. In the first
    // pass, we'll pass in a zero length buffer to get the required size of buffer. After we allocate memory for the 
    // buffer, we'll actually retrieve the generic type-args via the buffer.
    hr = m_pInfo->GetClassIDInfo2(classId, & m_ModuleId, & m_TypeDefToken, & m_ParentClassId, NULL, & m_cTypeArgs, NULL);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetClassIDInfo2");

    if (0 != m_cTypeArgs) 
    {
        m_rTypeArgs = new ClassID[m_cTypeArgs];
        if (NULL == m_rTypeArgs) 
		{
            hr = E_OUTOFMEMORY;
            TRACEERROR(hr, L"Failed to allocate memory");
            goto ErrReturn;
        }
        
        hr = m_pInfo->GetClassIDInfo2(classId, & m_ModuleId, & m_TypeDefToken, & m_ParentClassId, m_cTypeArgs, & m_cTypeArgs, m_rTypeArgs);
        HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetClassIDInfo2");
        
        m_bIsGeneric = true;
    }

    // retrieve the IMetadataImport interface
    hr = m_pInfo->GetModuleMetaData(m_ModuleId, ofRead, IID_IMetaDataImport2, (IUnknown**) & pMDImport);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetModuleMetaData"); 

    // use the typedef token to get more properties. Again we use two passes here - First pass to get the 
    // required size of buffer & next pass to get the name of the class.
    hr = pMDImport->GetTypeDefProps(m_TypeDefToken, NULL, NULL, & m_cchTypeName, NULL, NULL);
    HR_CHECK(hr, S_OK, L"IMetadataImport::GetTypeDefProps");

    m_wszTypeName = (true == m_bIsGeneric /*|| true == m_bIsArrayClass*/)
                    ? new WCHAR[m_cchTypeName + MAX_PATH] // we'll probably need to append "[]" or "<type-params>" at end of the type name
                    : new WCHAR[m_cchTypeName];
    if (NULL == m_wszTypeName) 
	{
        hr = E_OUTOFMEMORY;
        TRACEERROR(hr, L"Failed to allocate memory");
        goto ErrReturn;
    }
        
    hr = pMDImport->GetTypeDefProps(m_TypeDefToken, m_wszTypeName, m_cchTypeName, & m_cchTypeName, & m_dwTypeDefFlags, & m_tkExtends);
    HR_CHECK(hr, S_OK, L"IMetadataImport::GetTypeDefProps");

    // let us now enumerate all the fields belonging to this type. Unfortunately the metadata API does not return the 
    // required buffer size (using the zero-buffer technique). Hence we have to iterate over the whole enumeration and 
    // temporarily we shall cache them in a vector. Later we shall use this vector to initialize the FieldInfo * member.
    while (S_FALSE != (hr = pMDImport->EnumFields(& hEnum, m_TypeDefToken, & fieldTok, 1, NULL))) 
    {
        HR_CHECK(hr, S_OK, L"IMetaDataImport::EnumFields");

		m_FieldToks.push_back(fieldTok);
        ++ m_cFields;
    }
    hr = S_OK;


	// TEMP: check if the class is finalizable
	DWORD dwAttr = 0;
	hr = _ClassHasFinalizeMethod(pMDImport, m_TypeDefToken, dwAttr, m_bIsFinalizable);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::_ClassHasFinalizeMethod");



    // Now we'll get the classlayout. We don't need two passes for this. We already have
    // m_cFields (count of static and instance fields). It is bound to be greater than (or equal to) 
	// the count of instance fields returned by GetClassLayout.
    rFieldOffsets = new COR_FIELD_OFFSET[m_cFields];
    if (NULL == rFieldOffsets) {
        hr = E_OUTOFMEMORY;
        TRACEERROR(hr, L"Failed to allocate memory");
        goto ErrReturn;
    }
    
    hr = m_pInfo->GetClassLayout(classId, rFieldOffsets, m_cFields, & dwLayoutFields, & m_ulClassSize);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetClassLayout");

  //  if (0 != dwLayoutFields)
  //  {
  //      m_pFields = new FieldInfo[m_cFields];
  //      if (NULL == m_pFields) {
  //          hr = E_OUTOFMEMORY;
  //          TRACEERROR(hr, L"Failed to allocate memory");
  //          goto ErrReturn;
  //      }
  //      
  //      for (int i = 0; i < m_cFields; i++)
  //      {
		//	//hr = m_pFields[i].Init(rFieldOffsets[i].ridOfField, rFieldOffsets[i].ulOffset, m_ModuleId, m_ClassId, pMDImport, m_pInfo); 
  //          //HR_CHECK(hr, S_OK, L"FieldInfo::Init");
		//}
  //  }

    // Anything special about these types?
 //   if (true == m_bIsArrayClass) 
	//{
 //       wcscat_s(m_wszTypeName, m_cchTypeName + MAX_PATH, L"[]"); // append "[]" if array class // @TODO
 //   }

	if (0 == wcsicmp(L"System.String", m_wszTypeName)) 
	{
        m_bIsString = true;     // Is this a string?
    }


    /////////////////////////////////////////////////
ErrReturn:

    if (NULL != rFieldOffsets) 
	{
        delete[] rFieldOffsets;
        rFieldOffsets = NULL;
    }

    if (NULL != pMDImport) 
	{
        if (NULL !=  hEnum) 
		{
            pMDImport->CloseEnum(hEnum);
            hEnum = NULL;
        }
        pMDImport->Release();
        pMDImport = NULL;
    }

    return hr;
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ClassInfo::_ClassHasFinalizeMethod(IMetaDataImport2 * pMDImport, mdToken classToken, DWORD & dwAttr, bool & bResult)
{
	HRESULT hr = S_OK;
	HCORENUM hEnum = NULL;
	mdMethodDef methodDefToken[10];
	DWORD cMethodDefToken = 10;

	// some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> m_pInfo");
        goto ErrReturn;
    }
    if (NULL == pMDImport) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pMDImport");
        goto ErrReturn;
    }
    if (IsNilToken(classToken)) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> classToken");
        goto ErrReturn;
    }

	hr = pMDImport->EnumMethodsWithName(& hEnum, classToken, L"Finalize", methodDefToken, cMethodDefToken, & cMethodDefToken); 
	if (hr == S_FALSE) // no tokens to enumerate. Class does not implement finalizer.
	{
		hr = S_OK;
		goto ErrReturn;
	}
	HR_CHECK(hr, S_OK, L"IMetaDataImport2::EnumMethodsWithName");

	if (cMethodDefToken > 1)
	{
		hr = E_FAIL;
		TRACEERROR(hr, L"More than one finalizer method detected!");
		goto ErrReturn;
	}

	hr = pMDImport->GetMethodProps(methodDefToken[0], NULL, NULL, NULL, NULL, & dwAttr, NULL, NULL, NULL, NULL);
	HR_CHECK(hr, S_OK, L"IMetaDataImport2::GetMethodProps");

	bResult = !IsMdStatic(dwAttr) && IsMdVirtual(dwAttr);

    /////////////////////////////////////////////////
ErrReturn:

    if (NULL != pMDImport) 
	{
        if (NULL !=  hEnum) 
		{
            pMDImport->CloseEnum(hEnum);
            hEnum = NULL;
        }

		// we shouldn't release the IMetaDataImport2 interface.
		// Instead it should be handed back to the caller who passed it in.
    }

	return hr;
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ClassInfo::_ClassOverridesFinalize(IMetaDataImport2 * pMDImport, mdToken classToken, bool & result)
{
	HRESULT hr = S_OK;
    DWORD dwAttr = 0;
	bool bResult = false;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> m_pInfo");
        goto ErrReturn;
    }
    if (NULL == pMDImport) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pMDImport");
        goto ErrReturn;
    }
    if (IsNilToken(classToken)) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> classToken");
        goto ErrReturn;
    }

	// check if class implements a finalizer method
    hr = _ClassHasFinalizeMethod(pMDImport, classToken, dwAttr, bResult);
	HR_CHECK(hr, S_OK, L"ClassInfo::_ClassHasFinalizeMethod");

	// 
	if (bResult) 
	{
		bResult = IsMdReuseSlot(dwAttr);
	}

    /////////////////////////////////////////////////
ErrReturn:
	return hr;
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ClassInfo::_ClassReintroducesFinalize(IMetaDataImport2 *pMDImport, mdToken classToken,  bool & result)
{
	HRESULT hr = S_OK;
    DWORD dwAttr = 0;
	bool bResult = false;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> m_pInfo");
        goto ErrReturn;
    }
    if (NULL == pMDImport) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pMDImport");
        goto ErrReturn;
    }
    if (IsNilToken(classToken)) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> classToken");
        goto ErrReturn;
    }

	// check if class implements a finalizer method
    hr = _ClassHasFinalizeMethod(pMDImport, classToken, dwAttr, bResult);
	HR_CHECK(hr, S_OK, L"ClassInfo::_ClassHasFinalizeMethod");

	//
	if (bResult) 
	{
		bResult = IsMdNewSlot(dwAttr);
	}

    /////////////////////////////////////////////////
ErrReturn:
	return hr;
}



//HRESULT ClassInfo::GetClassName(
//								   IMetaDataImport2 * pMDImport, 
//								   mdTypeDef tkType, 
//								   wchar_t* szClass, 
//								   wchar_t* szParentClass, 
//								   wchar_t* szNamespace
//								   )
//{
//	// sanity checks
//	// --> there is not need to check the wchar_t* pointers neither
//	//     against null nor for buffer size because this is an internal
//	//     method only called with right sized buffers
//	//
//	HRESULT hr = S_OK;
//
//	wchar_t szFullClassName[1024 + 1];
//	szFullClassName[1024] = L'\0';
//
//	ULONG Length = 0;
//	CorTypeAttr tdFlags = tdClass;   // 0 value
//	mdToken tkParent = 0;
//	hr = 
//		pIMetaDataImport->GetTypeDefProps(
//		tkType,
//		szFullClassName, 
//		_MAX_CLASS_NAME,
//		&Length,
//		(DWORD*)&tdFlags, 
//		&tkParent
//		);
//
//	// Note: this could be S_FALSE in some cases :^(
//	ASSERT(SUCCEEDED(hr));
//	if (FAILED(hr))
//		return(hr);
//
//	// Note: the full name is retrieved 
//	//       if nested type, szFullClassName is the class only 
//	//          --> need to do the same for its parent
//	//       else, 
//	//          --> contain the namespaces and the class name at the end
//	//
//	if (!IsTdNested(tdFlags))
//	{
//		// not a nested type 
//		// --> szFullClassName = namespace.namespace...namespace.ClassName
//		wchar_t* pszNamespace = szFullClassName;
//		wchar_t* pszDot = wcsrchr(szFullClassName, L'.');
//		if (pszDot == NULL)
//		{
//			// no namespace
//			szNamespace[0] = L'\0';
//
//			// only the class name
//			wcsncpy(szClass, szFullClassName, _MAX_CLASS_NAME);
//		}
//		else
//		{
//			// the '.' is just between the namespaces and the class name
//			*pszDot = L'\0';
//
//			// the namespace itself starts after the '.'
//			wchar_t* pszName = pszDot + 1;
//
//			// extract class and namespace         
//			wcsncpy(szClass, pszName, _MAX_CLASS_NAME);
//			wcsncpy(szNamespace, pszNamespace, _MAX_CLASS_NAME);
//		}
//
//		// and no parent class
//		szParentClass[0] = L'\0';
//	}
//	else
//	{
//		// get the token for its parent
//		hr = pIMetaDataImport->GetNestedClassProps(tkType, &tkParent);
//		ASSERT(SUCCEEDED(hr));
//
//		// szFullClassName is the class name and we have to search recursively starting from its parent
//		wchar_t szTmpClass[_MAX_CLASS_NAME+1];
//		szTmpClass[_MAX_CLASS_NAME] = L'\0';
//
//		wchar_t szTmpParentClass[_MAX_CLASS_NAME+1];
//		szTmpParentClass[_MAX_CLASS_NAME] = L'\0';
//
//		hr = 
//			GetClassName(
//			pMDImport, 
//			tkParent, 
//			szTmpClass, 
//			szTmpParentClass, 
//			szNamespace
//			);
//		ASSERT(SUCCEEDED(hr));
//
//		// build the class name from its parent name
//		if (szTmpParentClass[0] != L'\0')
//		{
//			size_t length = 0;
//
//			wcsncpy(szParentClass, szTmpParentClass, _MAX_CLASS_NAME);
//			length = wcslen(szTmpParentClass); // count the string already copied
//
//			wcsncat(szParentClass, L".", _MAX_CLASS_NAME - length);
//			length++;   // count the '.'
//
//			wcsncat(szParentClass, szTmpClass, _MAX_CLASS_NAME - length);
//		}
//		else
//			wcsncpy(szParentClass, szTmpClass, _MAX_CLASS_NAME);
//
//		// the single class name is returned by GetTypeDefProps()
//		wcsncpy(szClass, szFullClassName, _MAX_CLASS_NAME);
//
//		// nothing special to do for namespace
//		// --> it has been computed during the parent lookup
//	}
//
//	return(hr);
//}
//

/////////////////////////////////////////////////////////////////////////////////////////


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

ArrayInfo::ArrayInfo(ClassID classId, ICorProfilerInfo3 * const pInfo)
:	m_ClassId 					(classId),
    m_cRank 					(0),
	m_baseElemClassId			(NULL),
	m_ulJaggedArrayDepth 		(0),
	m_InnerMostBaseElemClassId 	(NULL),
	m_bInit         			(false),
    m_pInfo         			(pInfo)
{
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ArrayInfo::Init()
{
    HRESULT hr = S_OK;

    hr = m_pInfo->IsArrayClass(m_ClassId, & m_baseElemType, & m_baseElemClassId, & m_cRank);
	HR_CHECK(hr, S_OK, L"ICorProfilerInfo::IsArrayClass");

	// detect whether this is a jagged array.
	ClassID tempId = m_baseElemClassId;
	CorElementType tempElemType = m_baseElemType;

	while (S_OK == hr)
	{
		++ m_ulJaggedArrayDepth;
		m_InnerMostBaseElemClassId = tempId;
		m_InnerMostBaseElemType = tempElemType;

		hr = m_pInfo->IsArrayClass(m_InnerMostBaseElemClassId, & m_InnerMostBaseElemType, & tempId, NULL);
		if (S_OK != hr && S_FALSE != hr)
		{
			TRACEERROR(hr, L"ICorProfilerInfo::IsArrayClass");
			goto ErrReturn;
		}
	}

	hr = S_OK;
	
    /////////////////////////////////////////////////
ErrReturn:

	m_bInit = (S_OK == hr) ? true : false;
    
    return hr;
}

/////////////////////////////////////////////////////////////////////////////////////////


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

ModuleInfo::ModuleInfo(ModuleID moduleId, ICorProfilerInfo3 * const pInfo)
: m_ModuleId          (moduleId),
  m_cchModuleName     (0),
  m_wszModuleName     (NULL),
  m_ParentAssemblyId  (0),
  m_bInit             (false),
  m_pInfo             (pInfo)
{
}


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

ModuleInfo::~ModuleInfo()
{
    if (NULL != m_wszModuleName) 
	{
        delete[] m_wszModuleName;
        m_wszModuleName = NULL;
    }    
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ModuleInfo::Init()
{
    HRESULT hr = S_OK;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pInfo");
        goto ErrReturn;
    }

    // We need to retrieve the module name via a buffer. For this we need to do two passes. In the first
    // pass, we'll pass in a zero length buffer to get the required size of buffer. After we allocate memory for the 
    // buffer, we'll actually use it to retrieve the module name.
    hr = m_pInfo->GetModuleInfo(m_ModuleId, NULL, NULL, & m_cchModuleName, NULL, NULL);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetModuleInfo");

    m_wszModuleName = new WCHAR[m_cchModuleName];
    if (NULL == m_wszModuleName) 
	{
        hr = E_OUTOFMEMORY;
        TRACEERROR(hr, L"Failed to allocate memory");
        goto ErrReturn;
    }
    
    hr = m_pInfo->GetModuleInfo(m_ModuleId, NULL, m_cchModuleName, & m_cchModuleName, m_wszModuleName, & m_ParentAssemblyId);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetModuleInfo");

    /////////////////////////////////////////////////
ErrReturn:

	m_bInit = (S_OK == hr) ? true : false;
    
    return hr;
}

/////////////////////////////////////////////////////////////////////////////////////////


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

AssemblyInfo::AssemblyInfo(AssemblyID assemblyId, ICorProfilerInfo3 * const pInfo)
: m_AssemblyId         (assemblyId),
  m_cchAssemblyName    (0),
  m_wszAssemblyName    (NULL),
  m_ParentAppdomainId  (0),
  m_bInit              (false),
  m_pInfo              (pInfo)
{
}


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

AssemblyInfo::~AssemblyInfo()
{
    if (NULL != m_wszAssemblyName) 
	{
        delete[] m_wszAssemblyName;
        m_wszAssemblyName = NULL;
    }    
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT AssemblyInfo::Init()
{
    HRESULT hr = S_OK;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pInfo");
        goto ErrReturn;
    }

    // We need to retrieve the Assembly name via a buffer. For this we need to do two passes. In the first
    // pass, we'll pass in a zero length buffer to get the required size of buffer. After we allocate memory for the 
    // buffer, we'll actually use it to retrieve the Assembly name.
    hr = m_pInfo->GetAssemblyInfo(m_AssemblyId, NULL, & m_cchAssemblyName, NULL, NULL, NULL);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetAssemblyInfo");

    m_wszAssemblyName = new WCHAR[m_cchAssemblyName];
    if (NULL == m_wszAssemblyName) 
	{
        hr = E_OUTOFMEMORY;
        TRACEERROR(hr, L"Failed to allocate memory");
        goto ErrReturn;
    }
    
    hr = m_pInfo->GetAssemblyInfo(m_AssemblyId, m_cchAssemblyName, & m_cchAssemblyName, m_wszAssemblyName, & m_ParentAppdomainId, NULL);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetAssemblyInfo");

    /////////////////////////////////////////////////
ErrReturn:

	m_bInit = (S_OK == hr) ? true : false;
    
    return hr;
}

/////////////////////////////////////////////////////////////////////////////////////////


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

AppDomainInfo::AppDomainInfo(AppDomainID appdomainId, ICorProfilerInfo3 * const pInfo)
: m_AppDomainId        (appdomainId),
  m_cchAppDomainName   (0),
  m_wszAppDomainName   (NULL),
  m_bInit              (false),
  m_pInfo              (pInfo)
{
}


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

AppDomainInfo::~AppDomainInfo()
{
    if (NULL != m_wszAppDomainName) 
	{
        delete[] m_wszAppDomainName;
        m_wszAppDomainName = NULL;
    }    
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT AppDomainInfo::Init()
{
    HRESULT hr = S_OK;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pInfo");
        goto ErrReturn;
    }

    // We need to retrieve the AppDomain name via a buffer. For this we need to do two passes. In the first
    // pass, we'll pass in a zero length buffer to get the required size of buffer. After we allocate memory for the 
    // buffer, we'll actually use it to retrieve the AppDomain name.
    hr = m_pInfo->GetAppDomainInfo(m_AppDomainId, NULL, & m_cchAppDomainName, NULL, NULL);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetAppDomainInfo");

    m_wszAppDomainName = new WCHAR[m_cchAppDomainName];
    if (NULL == m_wszAppDomainName) 
	{
        hr = E_OUTOFMEMORY;
        TRACEERROR(hr, L"Failed to allocate memory");
        goto ErrReturn;
    }
    
    hr = m_pInfo->GetAppDomainInfo(m_AppDomainId, m_cchAppDomainName, & m_cchAppDomainName, m_wszAppDomainName, NULL);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetAppDomainInfo");

    /////////////////////////////////////////////////
ErrReturn:

	m_bInit = (S_OK == hr) ? true : false;
    
    return hr;
}

/////////////////////////////////////////////////////////////////////////////////////////

//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

FunctionInfo::FunctionInfo(FunctionID functionId, ICorProfilerInfo3 * const pInfo)
: m_FunctionId     	(functionId),
  m_cchFunctionName (0),
  m_wszFunctionName (NULL),
  m_classId			(0),
  m_moduleId		(0),
  m_bInit           (false),
  m_pInfo           (pInfo)
{
}


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

FunctionInfo::~FunctionInfo()
{
    if (NULL != m_wszFunctionName) 
	{
        delete[] m_wszFunctionName;
        m_wszFunctionName = NULL;
    }    
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT FunctionInfo::Init()
{
    HRESULT hr = S_OK;
	IMetaDataImport2 * pMDImport = NULL;
	mdToken token;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pInfo");
        goto ErrReturn;
    }

	// Get the function info ..
	hr = m_pInfo->GetFunctionInfo(m_FunctionId, & m_classId, & m_moduleId, & token);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetFunctionInfo");

    // retrieve the IMetadataImport interface
    hr = m_pInfo->GetModuleMetaData(m_moduleId, ofRead, IID_IMetaDataImport2, (IUnknown**) & pMDImport);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetModuleMetaData"); 

    // We need to retrieve the function name via a buffer. For this we need to do two passes. In the first
    // pass, we'll pass in a zero length buffer to get the required size of buffer. After we allocate memory for the 
    // buffer, we'll actually use it to retrieve the function name.
	hr = pMDImport->GetMethodProps(token, NULL, NULL, NULL, & m_cchFunctionName, NULL, NULL, NULL, NULL, NULL);
    HR_CHECK(hr, S_OK, L"IMetaDataImport2::GetMethodProps");

    m_wszFunctionName = new WCHAR[m_cchFunctionName];
    if (NULL == m_wszFunctionName) 
	{
        hr = E_OUTOFMEMORY;
        TRACEERROR(hr, L"Failed to allocate memory");
        goto ErrReturn;
    }
    
	hr = pMDImport->GetMethodProps(token, NULL, m_wszFunctionName, m_cchFunctionName, & m_cchFunctionName, NULL, NULL, NULL, NULL, NULL);
    HR_CHECK(hr, S_OK, L"IMetaDataImport2::GetMethodProps");

    /////////////////////////////////////////////////
ErrReturn:

	if (NULL != pMDImport)
	{
		pMDImport->Release();
		pMDImport = NULL;
	}

	m_bInit = (S_OK == hr) ? true : false;
    
    return hr;
}


//*********************************************************************
//  Descripton: Ctor for the ObjectInfo class.
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

ObjectInfo::ObjectInfo(ObjectID objectId, ICorProfilerInfo3 * const pInfo)
: m_ObjectId        (objectId),   
  m_dwGeneration	(0),
  m_cbObjRawSize    (0),
  m_cbObjAlignedSize(0),
  m_bInit           (false),
  m_pInfo           (pInfo)
{
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ObjectInfo::Init()
{
    HRESULT hr = S_OK;
    COR_PRF_GC_GENERATION_RANGE genRange;

    // some pre-condition checks
    if (NULL == m_pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pInfo");
        goto ErrReturn;
    }

    hr = m_pInfo->GetObjectGeneration(m_ObjectId, & genRange);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetObjectGeneration");

    hr = m_pInfo->GetObjectSize(m_ObjectId, & m_cbObjRawSize);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetObjectSize");

    m_cbObjAlignedSize = GetAlignedSizeInternal(genRange.generation, m_cbObjRawSize);
	m_dwGeneration = genRange.generation;

    /////////////////////////////////////////////////
ErrReturn:

	m_bInit = (S_OK == hr) ? true : false;
    
    return hr;
}

/////////////////////////////////////////////////////////////////////////////////////////


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT ObjectInfo::GetAlignedSize(ICorProfilerInfo3 * pInfo, ObjectID objectId, ULONG * pulAlignedObjSize)
{
    HRESULT hr = S_OK;
    COR_PRF_GC_GENERATION_RANGE genRange;
    ULONG cbObjRawSize; 

    // some pre-condition checks
    if (NULL == pInfo) 
	{
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"pre-condition check failed -> pInfo");
        goto ErrReturn;
    }

    hr = pInfo->GetObjectGeneration(objectId, & genRange);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetObjectGeneration");
    
    hr = pInfo->GetObjectSize(objectId, & cbObjRawSize);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetObjectSize");

	* pulAlignedObjSize = GetAlignedSizeInternal(genRange.generation, cbObjRawSize);

    /////////////////////////////////////////////////
ErrReturn:    
    return hr;
}


//*********************************************************************
//  Descripton: If object is on LOH, we have to align up the object size. The rules are simple - 
//  			- if we are on X86 and object is in LOH, then we align size to 8 byte boundary
//  			- if we are on X86 and object in NOT in LOH, we simply align it to 4 byte boundary
//  			- if we are on AMD64/IA64, we align the size to 8 byte boundary irrespective of object location.
//  
//  Params:     COR_PRF_GC_GENERATION gen - generation in which the object lies.
//				ULONG cbObjRawSize - raw/unaligned size of the object.
//
//  Return:     The aligned size of the object.
//  
//  Notes:      n/a
//*********************************************************************

ULONG ObjectInfo::GetAlignedSizeInternal(COR_PRF_GC_GENERATION gen, ULONG cbObjRawSize)
{
#ifdef _X86_
    return (COR_PRF_GC_LARGE_OBJECT_HEAP == gen)
           		? (cbObjRawSize + (2 * sizeof(ULONG_PTR)) - 1) & ~((2 * sizeof(ULONG_PTR)) - 1)
                : (cbObjRawSize + sizeof(ULONG_PTR) - 1) & ~(sizeof(ULONG_PTR) - 1);
#else
    return (cbObjRawSize + sizeof(ULONG_PTR) - 1) & ~(sizeof(ULONG_PTR) - 1);
#endif
}

/////////////////////////////////////////////////////////////////////////////////////////


//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

FieldInfo::FieldInfo(mdFieldDef fieldDefTok, ClassID parentClassId, ModuleID moduleId, set<AppDomainID>& appdomainIds, ICorProfilerInfo3 * const pInfo)
: m_fieldDefTok     (fieldDefTok),
  m_TypeDefTok      (0),
  m_ulFieldOffset   (0),
  m_ParentClassId   (parentClassId),
  m_ModuleId		(moduleId),
  m_staticType		(COR_PRF_FIELD_NOT_A_STATIC),
  m_dwFieldAttr     (0),
  m_pwszFieldName   (NULL),
  m_cchFieldName    (0),
  m_pvSigBlob       (0),
  m_cbSigBlob       (0),
  m_dwCPlusTypeFlag (0),
  m_pValue          (NULL),
  m_cchValue        (0),
  m_appdomainIds	(appdomainIds),
  m_bInit           (false),
  m_pInfo           (pInfo)
{
}

//*********************************************************************
//  Descripton: 
//  
//  Notes:      n/a
//*********************************************************************

FieldInfo::~FieldInfo()
{
    if (NULL != m_pwszFieldName) {
        delete[] m_pwszFieldName;
        m_pwszFieldName = NULL;
    }

	m_AppDomainStaticAddresses.clear();
	m_RVAStaticAddresses.clear();
}


//*********************************************************************
//  Descripton: 
//  
//  Params:     
//
//  Return:     S_OK if method successful, else appropriate error code.
//  
//  Notes:      n/a
//*********************************************************************

HRESULT FieldInfo::Init()
{
    HRESULT hr = S_OK;
    IMetaDataImport2 * pMDImport = NULL;    
    LPVOID pAddressOfStatic = (LPVOID) 0;
	ULONG32 cAppDomainIDs = 0;
	AppDomainID * pAppDomainIDs = NULL;

    // pre-condition check
    if (NULL == m_pInfo) {
        hr = E_INVALIDARG;
        TRACEERROR(hr, L"Invalid arg passed in -> pInfo");
        goto ErrReturn;
    }
        
    // retrieve the IMetadataImport interface
    hr = m_pInfo->GetModuleMetaData(m_ModuleId, ofRead, IID_IMetaDataImport2, (IUnknown**) & pMDImport);
    HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetModuleMetaData"); 

    // need to get required buffer size .........
    hr = pMDImport->GetFieldProps(m_fieldDefTok, NULL, NULL, NULL, & m_cchFieldName, NULL, NULL, NULL, NULL, NULL, NULL);
    HR_CHECK(hr, S_OK, L"IMetaDataImport::GetFieldProps");
            
    m_pwszFieldName = new WCHAR[m_cchFieldName];
    if (NULL == m_pwszFieldName) {
        hr = E_OUTOFMEMORY;
        TRACEERROR(hr, L"Failed to allocate memory");
        goto ErrReturn;
    }
              
    // retrieving the field props ......
    hr = pMDImport->GetFieldProps(
                                m_fieldDefTok, 
                                & m_TypeDefTok, 
                                m_pwszFieldName, 
                                m_cchFieldName, 
                                & m_cchFieldName, 
                                & m_dwFieldAttr, 
                                & m_pvSigBlob, 
                                & m_cbSigBlob, 
                                & m_dwCPlusTypeFlag, 
                                & m_pValue, 
                                & m_cchValue);
    HR_CHECK(hr, S_OK, L"IMetaDataImport::GetFieldProps");

    // get all info on static fields
    if (IsFdStatic(m_dwFieldAttr) && !IsFdLiteral(m_dwFieldAttr))
    {
        hr = m_pInfo->GetStaticFieldInfo(m_ParentClassId, m_fieldDefTok, & m_staticType);
        HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetStaticFieldInfo");


		switch(m_staticType)
        {
            case COR_PRF_FIELD_APP_DOMAIN_STATIC:
				{
					//hr = m_pInfo->GetAppDomainsContainingModule(m_ModuleId, 0, & cAppDomainIDs, NULL);
     //   			HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetAppDomainsContainingModule");
					//				
				 //   if (0 != cAppDomainIDs) 
				 //   {
				 //       pAppDomainIDs = new AppDomainID[cAppDomainIDs];
				 //       if (NULL == pAppDomainIDs) 
					//	{
				 //           hr = E_OUTOFMEMORY;
				 //           TRACEERROR(hr, L"Failed to allocate memory");
				 //           goto ErrReturn;
				 //       }

					//	hr = m_pInfo->GetAppDomainsContainingModule(m_ModuleId, cAppDomainIDs, & cAppDomainIDs, pAppDomainIDs);
	    //    			HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetAppDomainsContainingModule");

					//	for (int i = 0; i < cAppDomainIDs; i++)
					//	{
			  //              hr = m_pInfo->GetAppDomainStaticAddress(m_ParentClassId, m_fieldDefTok, pAppDomainIDs[i], & pAddressOfStatic);
					//		if (S_OK == hr)
					//		{
					//			m_AppDomainStaticAddresses[pAppDomainIDs[i]] = * ((UINT_PTR *) (pAddressOfStatic));
					//		}
					//		else if (E_INVALIDARG == hr || CORPROF_E_DATAINCOMPLETE == hr)
					//		{ 
			  //                  pAddressOfStatic = 0;
			  //                  hr = S_OK;
					//		}
					//		else
					//		{
			  //              	TRACEERROR(hr, L"ICorProfilerInfo2::GetAppDomainStaticAddress");
					//			goto ErrReturn;
					//		}
					//	}
					//}


					//set<AppDomainID>::iterator apdItr;
					//for (apdItr = m_appdomainIds.begin(); apdItr != m_appdomainIds.end(); apdItr++)
					//{
		   //             hr = m_pInfo->GetAppDomainStaticAddress(m_ParentClassId, m_fieldDefTok, *apdItr, & pAddressOfStatic);
					//	if (S_OK == hr)
					//	{
					//		m_AppDomainStaticAddresses[*apdItr] = * ((UINT_PTR *) (pAddressOfStatic));
					//	}
					//	else if (E_INVALIDARG == hr || CORPROF_E_DATAINCOMPLETE == hr)
					//	{ 
		   //                 pAddressOfStatic = 0;
		   //                 hr = S_OK;
					//	}
					//	else
					//	{
		   //             	TRACEERROR(hr, L"ICorProfilerInfo2::GetAppDomainStaticAddress");
					//		goto ErrReturn;
					//	}
					//}
				}
                break;

			case COR_PRF_FIELD_THREAD_STATIC:

                break;
            case COR_PRF_FIELD_CONTEXT_STATIC:

                break;            
            case COR_PRF_FIELD_RVA_STATIC:
				{
	                hr = m_pInfo->GetRVAStaticAddress(m_ParentClassId, m_fieldDefTok, & pAddressOfStatic);
					if (S_OK == hr)
					{
						m_RVAStaticAddresses.push_back(* ((UINT_PTR *) pAddressOfStatic));
					}
					else if (E_INVALIDARG == hr || CORPROF_E_DATAINCOMPLETE == hr)
					{ 
	                    pAddressOfStatic = 0;
	                    hr = S_OK;
					}
					else
					{
	                	TRACEERROR(hr, L"ICorProfilerInfo2::GetAppDomainStaticAddress");
						goto ErrReturn;
					}

				}
                break;
                
            case COR_PRF_FIELD_NOT_A_STATIC:    
            default:
				{
	                hr = E_FAIL;
	                TRACEERROR(hr, L"Received an invalid COR_PRF_STATIC_TYPE enum");
	                goto ErrReturn;
				}
                break;
        }


	}


        //// HACK begin
        //hr = m_pInfo->GetCurrentThreadID(& threadId);        
        //HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetCurrentThreadID");
        //
        //hr = m_pInfo->GetThreadContext(threadId, & contextId);
        //HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetThreadContext");
        //
        //hr = m_pInfo->GetThreadAppDomain(threadId, & appDomainId);
        //HR_CHECK(hr, S_OK, L"ICorProfilerInfo2::GetThreadAppDomain");
        //// END HACK
    

    // @TODO - At this point i do not know if the field is a generic type. I'm currently assuming that it is not.
    // Maybe I can lookup m_pvSigBlob and decide ?????????
    // @TODO - WE see a contract violation assert because of this call. GC-NOtriggers
    //hr = m_pInfo->GetClassFromTokenAndTypeArgs(moduleId, m_TypeDefTok, 0, 0, & m_ClassId);
    //HR_CHECK(hr, S_OK, L"ICorProfilerInfo::GetClassFromToken");

    /////////////////////////////////////////////////
ErrReturn:

    if (S_OK == hr) {
        m_bInit = true;
    }
    
    return hr;    
}

/////////////////////////////////////////////////////////////////////////////////////////


