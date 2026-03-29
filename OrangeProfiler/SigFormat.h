#pragma once
//////////////////////////////////////////////////////////////////////////////////////
// SigFormat.h - This file demonstrates how to use the general-purpose signature 	//
//				 parser (SigParser) by deriving a new class from it and overriding 	//
//				 the virtuals.                  									//
// Application - Orange Profiler.                                                   //
// Author      - (Probably) David Broman, DavBr@microsoft.com						//
// 			   - Heavily modified by Mithun Shanbhag, mithuns@microsoft.com			//
//////////////////////////////////////////////////////////////////////////////////////


#include "common.hxx"
#include "SigParse.h"


#define dimensionof(a) 		(sizeof(a)/sizeof(*(a)))
#define MAKE_CASE(__elt) case __elt: return #__elt;
#define MAKE_CASE_OR(__elt) case __elt: return #__elt "|";

class SigFormat : public SigParser
{
private:
	IMetaDataImport2  * m_pMDImport;
	ICorProfilerInfo3 * m_pInfo;
	ModuleID m_moduleId;

public:

	SigFormat(IMetaDataImport2 * pMDImport, ICorProfilerInfo3 * pInfo, ModuleID moduleId)
		: m_pMDImport 	(pMDImport),	
		 m_pInfo		(pInfo),
		 m_moduleId	(moduleId)
	{
	}

   
protected:
   LPCSTR SigIndexTypeToString(sig_index_type sit)
    {
        switch(sit)
        {
            default:
                return "unknown index type";
            MAKE_CASE(SIG_INDEX_TYPE_TYPEDEF)
            MAKE_CASE(SIG_INDEX_TYPE_TYPEREF)
            MAKE_CASE(SIG_INDEX_TYPE_TYPESPEC)
        }
    }
                
    LPCSTR SigMemberTypeOptionToString(sig_elem_type set)
    {
        switch(set & 0xf0)
        {
            default:
                return "unknown element type";
            case 0:
                return "";
                
            MAKE_CASE_OR(SIG_GENERIC)
            MAKE_CASE_OR(SIG_HASTHIS)
            MAKE_CASE_OR(SIG_EXPLICITTHIS)
        }
    }

    LPCSTR SigMemberTypeToString(sig_elem_type set)
    {
        switch(set & 0xf)
        {
            default:
                return "unknown element type";
            MAKE_CASE(SIG_METHOD_DEFAULT)
            MAKE_CASE(SIG_METHOD_C)
            MAKE_CASE(SIG_METHOD_STDCALL)
            MAKE_CASE(SIG_METHOD_THISCALL)
            MAKE_CASE(SIG_METHOD_FASTCALL)
            MAKE_CASE(SIG_METHOD_VARARG)
            MAKE_CASE(SIG_FIELD)
            MAKE_CASE(SIG_LOCAL_SIG)
            MAKE_CASE(SIG_PROPERTY)
        }
    }

    LPCSTR SigElementTypeToString(sig_elem_type set)
    {
        switch(set)
        {
            default:
                return "unknown element type";
            MAKE_CASE(ELEMENT_TYPE_END)
            MAKE_CASE(ELEMENT_TYPE_VOID)
            MAKE_CASE(ELEMENT_TYPE_BOOLEAN)
            MAKE_CASE(ELEMENT_TYPE_CHAR)
            MAKE_CASE(ELEMENT_TYPE_I1)
            MAKE_CASE(ELEMENT_TYPE_U1)
            MAKE_CASE(ELEMENT_TYPE_I2)
            MAKE_CASE(ELEMENT_TYPE_U2)
            MAKE_CASE(ELEMENT_TYPE_I4)
            MAKE_CASE(ELEMENT_TYPE_U4)
            MAKE_CASE(ELEMENT_TYPE_I8)
            MAKE_CASE(ELEMENT_TYPE_U8)
            MAKE_CASE(ELEMENT_TYPE_R4)
            MAKE_CASE(ELEMENT_TYPE_R8)
            MAKE_CASE(ELEMENT_TYPE_STRING)
            MAKE_CASE(ELEMENT_TYPE_PTR)
            MAKE_CASE(ELEMENT_TYPE_BYREF)
            MAKE_CASE(ELEMENT_TYPE_VALUETYPE)
            MAKE_CASE(ELEMENT_TYPE_CLASS)
            MAKE_CASE(ELEMENT_TYPE_VAR)
            MAKE_CASE(ELEMENT_TYPE_ARRAY)
            MAKE_CASE(ELEMENT_TYPE_GENERICINST)
            MAKE_CASE(ELEMENT_TYPE_TYPEDBYREF)
            MAKE_CASE(ELEMENT_TYPE_I)
            MAKE_CASE(ELEMENT_TYPE_U)
            MAKE_CASE(ELEMENT_TYPE_FNPTR)
            MAKE_CASE(ELEMENT_TYPE_OBJECT)
            MAKE_CASE(ELEMENT_TYPE_SZARRAY)
            MAKE_CASE(ELEMENT_TYPE_MVAR)
            MAKE_CASE(ELEMENT_TYPE_CMOD_REQD)
            MAKE_CASE(ELEMENT_TYPE_CMOD_OPT)
            MAKE_CASE(ELEMENT_TYPE_INTERNAL)
            MAKE_CASE(ELEMENT_TYPE_MODIFIER)
            MAKE_CASE(ELEMENT_TYPE_SENTINEL)
            MAKE_CASE(ELEMENT_TYPE_PINNED)
        }
    }

    // Simple wrapper around printf that prints the indenting spaces for you
    void Print(const char* format, ...)
    {
        va_list argList;
        va_start(argList, format);
        vprintf(format, argList);
    }
    
    // a method with given elem_type
    virtual void NotifyBeginMethod(sig_elem_type elem_type)
    {
        Print("BEGIN METHOD\n");
    }
    
    virtual void NotifyEndMethod()
    {
        Print("END METHOD\n");
    }

    // total parameters for the method
    virtual void NotifyParamCount(sig_count count)
    {
        Print("Param count = '%d'\n", count);
    }

    // starting a return type
    virtual void NotifyBeginRetType()
    {
        Print("BEGIN RET TYPE\n");
    }
    virtual void NotifyEndRetType()
    {
        Print("END RET TYPE\n");
    }

    // starting a parameter
    virtual void NotifyBeginParam()
    {
        Print("BEGIN PARAM\n");
    }
    
    virtual void NotifyEndParam()
    {
        Print("END PARAM\n");
    }

    // sentinel indication the location of the "..." in the method signature
    virtual void NotifySentinal()
    {
        Print("...\n");
    }

    // number of generic parameters in this method signature (if any)
    virtual void NotifyGenericParamCount(sig_count count)
    {
        Print("Generic param count = '%d'\n", count);
    }

    //----------------------------------------------------

    // a field with given elem_type
    virtual void NotifyBeginField(sig_elem_type elem_type)
    {
    }
    
    virtual void NotifyEndField()
    {
    }

    //----------------------------------------------------

    // a block of locals with given elem_type (always just LOCAL_SIG for now)
    virtual void NotifyBeginLocals(sig_elem_type elem_type)
    {
        Print("BEGIN LOCALS: '%s%s'\n", SigMemberTypeOptionToString(elem_type), SigMemberTypeToString(elem_type));
        
    }
    
    virtual void NotifyEndLocals()
    {
        
        Print("END LOCALS\n");
    }
        

    // count of locals with a block
    virtual void NotifyLocalsCount(sig_count count)
    {
        Print("Locals count: '%d'\n", count);
    }

    // starting a new local within a local block
    virtual void NotifyBeginLocal()
    {
        Print("BEGIN LOCAL\n");
        
    }
    
    virtual void NotifyEndLocal()
    {
        
        Print("END LOCAL\n");
    }
        

    // the only constraint available to locals at the moment is ELEMENT_TYPE_PINNED
    virtual void NotifyConstraint(sig_elem_type elem_type)
    {
        Print("Constraint: '%s%s'\n", SigMemberTypeOptionToString(elem_type), SigMemberTypeToString(elem_type));
    }


    //----------------------------------------------------

    // a property with given element type
    virtual void NotifyBeginProperty(sig_elem_type elem_type)
    {
        Print("BEGIN PROPERTY: '%s%s'\n", SigMemberTypeOptionToString(elem_type), SigMemberTypeToString(elem_type));
        
    }
    
    virtual void NotifyEndProperty()
    {
        
        Print("END PROPERTY\n");
    }
        

    //----------------------------------------------------

    // starting array shape information for array types
    virtual void NotifyBeginArrayShape()
    {
        TRACE(L"<ar>");        
    }
    
    virtual void NotifyEndArrayShape()
    {
        TRACE(L"</ar>");        
    }
        

    // array rank (total number of dimensions)
    virtual void NotifyRank(sig_count count)
    {
        TRACE(L"<r>%x</r>", count);        
    }

    // number of dimensions with specified sizes followed by the size of each
    virtual void NotifyNumSizes(sig_count count)
    {
        TRACE(L"<d>%x</d>", count);            
	}
    
    virtual void NotifySize(sig_count count)
    {
        TRACE(L"<s>%x</s>", count);            
    }

    // BUG BUG lower bounds can be negative, how can this be encoded?
    // number of dimensions with specified lower bounds followed by lower bound of each 
    virtual void NotifyNumLoBounds(sig_count count)
    {
        TRACE(L"<lb>%x</lb>", count);            
    }
    
    virtual void NotifyLoBound(sig_count count)
    {
        TRACE(L"<l>%x</l>", count);            
    }

    //----------------------------------------------------


    // starting a normal type (occurs in many contexts such as param, field, local, etc)
    virtual void NotifyBeginType()
    {
        TRACE(L"<t>");
    }
    
    virtual void NotifyEndType()
    {
        TRACE(L"</t>");
    }

	// the type is "VOID" (this has limited uses, function returns and void pointer)
    virtual void NotifyType(sig_elem_type  elem_type)
    {
       	TRACE(L"<e>%x</e>", elem_type);
    }


    virtual void NotifyTypedByref()
    {
        Print("Typed byref\n");
    }

    // the type has the 'byref' modifier on it -- this normally proceeds the type definition in the context
    // the type is used, so for instance a parameter might have the byref modifier on it
    // so this happens before the BeginType in that context
    virtual void NotifyByref()
    {
        Print("Byref\n");
    }

    // the type is "VOID" (this has limited uses, function returns and void pointer)
    virtual void NotifyVoid(sig_elem_type  elem_type)
    {
       TRACE(L"<e>%x</e>", elem_type);
    }

    // the type has the indicated custom modifiers (which can be optional or required)
    virtual void NotifyCustomMod(sig_elem_type cmod, sig_index_type indexType, sig_index index)
    {
        Print(
            "Custom modifers: '%s', index type: '%s', index: '0x%x'\n",
            SigElementTypeToString(cmod),
            SigIndexTypeToString(indexType),
            index);
    }

    // the type is specified by the given index of the given index type (normally a type index in the type metadata)
    // this callback is normally qualified by other ones such as NotifyTypeClass or NotifyTypeValueType
    virtual void NotifyTypeDefOrRef(sig_index_type  indexType, int index, mdToken mdTok)
    {
		HRESULT hr = S_OK; 
		WCHAR wszTypeName[1024] = {0};
		ULONG cchTypeName = 1024;
		mdToken tkScope;
 
		if (!IsNilToken(mdTok))
		{
			if (mdtTypeDef == TypeFromToken(mdTok) || mdtTypeSpec == TypeFromToken(mdTok))
			{
				hr = m_pMDImport->GetTypeDefProps(mdTok, wszTypeName, cchTypeName, & cchTypeName, NULL, NULL);
				HR_CHECK(hr, S_OK, L"IMetaDataImport2::GetTypeDefProps");
	       	
				TRACE(L"<n>%s</n>", wszTypeName);
			}
			else if (mdtTypeRef == TypeFromToken(mdTok))
			{
				hr = m_pMDImport->GetTypeRefProps(mdTok, & tkScope, wszTypeName, cchTypeName, & cchTypeName);
				HR_CHECK(hr, S_OK, L"IMetaDataImport2::GetTypeRefProps");

				TRACE(L"<n>%s</n>", wszTypeName);
			}
		}

ErrReturn:
		return;
    }

    // the type is an instance of a generic
    // elem_type indicates value_type or class
    // indexType and index indicate the metadata for the type in question
    // number indicates the number of type specifications for the generic types that will follow
    virtual void NotifyTypeGenericInst(sig_elem_type elem_type, sig_index_type indexType, sig_index index, sig_mem_number number, mdToken mdTok)
    {
		HRESULT hr = S_OK; 
		WCHAR wszTypeName[1024] = {0};
		ULONG cchTypeName = 1024;
		mdToken tkScope;

		TRACE(L"<a>%x</a>", number);

		if (!IsNilToken(mdTok))
		{
			if (mdtTypeDef == TypeFromToken(mdTok) || mdtTypeSpec == TypeFromToken(mdTok))
			{
				hr = m_pMDImport->GetTypeDefProps(mdTok, wszTypeName, cchTypeName, & cchTypeName, NULL, NULL);
				HR_CHECK(hr, S_OK, L"IMetaDataImport2::GetTypeDefProps");
	       	
				TRACE(L"<n>%s</n>", wszTypeName);
			}
			else if (mdtTypeRef == TypeFromToken(mdTok))
			{
				hr = m_pMDImport->GetTypeRefProps(mdTok, & tkScope, wszTypeName, cchTypeName, & cchTypeName);
				HR_CHECK(hr, S_OK, L"IMetaDataImport2::GetTypeRefProps");

				TRACE(L"<n>%s</n>", wszTypeName);
			}
		}

ErrReturn:
		return;
    }

    // the type is the type of the nth generic type parameter for the class
    virtual void NotifyTypeGenericTypeVariable(sig_mem_number number)
    {
        Print("Type generic type variable: number: '%d'\n", number);
    }

    // the type is the type of the nth generic type parameter for the member
    virtual void NotifyTypeGenericMemberVariable(sig_mem_number number)
    {
        Print("Type generic member variable: number: '%d'\n", number);
    }

};
