///////////////////////////////////////////////////////////////////////////////////////////
// CorProfWrapper.cs: Managed wrappers for structures, enums in CorProf.IDL file.        //
// Application      : CLR V4 DST Test Infrastructure                                     //
// Author           : Mithun Shanbhag                                                    //
///////////////////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


namespace CorProfWrapper
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum COR_PRF_GC_ROOT_KIND
    {
        COR_PRF_GC_ROOT_STACK       = 1,
        COR_PRF_GC_ROOT_FINALIZER   = 2,
        COR_PRF_GC_ROOT_HANDLE      = 3,
        COR_PRF_GC_ROOT_OTHER       = 0
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum COR_PRF_GC_ROOT_FLAGS
    {
        COR_PRF_GC_ROOT_PINNING     = 0x1,
        COR_PRF_GC_ROOT_WEAKREF     = 0x2,
        COR_PRF_GC_ROOT_INTERIOR    = 0x4,
        COR_PRF_GC_ROOT_REFCOUNTED  = 0x8
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum COR_PRF_STATIC_TYPE
    {
        COR_PRF_FIELD_NOT_A_STATIC      = 0x0,
        COR_PRF_FIELD_APP_DOMAIN_STATIC = 0x1,
        COR_PRF_FIELD_THREAD_STATIC     = 0x2,
        COR_PRF_FIELD_CONTEXT_STATIC    = 0x4,
        COR_PRF_FIELD_RVA_STATIC        = 0x8
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags]
    public enum CorElementType 
    {
        ELEMENT_TYPE_END            = 0x0,
        ELEMENT_TYPE_VOID           = 0x1,
        ELEMENT_TYPE_BOOLEAN        = 0x2,
        ELEMENT_TYPE_CHAR           = 0x3,
        ELEMENT_TYPE_I1             = 0x4,
        ELEMENT_TYPE_U1             = 0x5,
        ELEMENT_TYPE_I2             = 0x6,
        ELEMENT_TYPE_U2             = 0x7,
        ELEMENT_TYPE_I4             = 0x8,
        ELEMENT_TYPE_U4             = 0x9,
        ELEMENT_TYPE_I8             = 0xa,
        ELEMENT_TYPE_U8             = 0xb,
        ELEMENT_TYPE_R4             = 0xc,
        ELEMENT_TYPE_R8             = 0xd,
        ELEMENT_TYPE_STRING         = 0xe,
            
        ELEMENT_TYPE_PTR            = 0xf,
        ELEMENT_TYPE_BYREF          = 0x10,
            
        ELEMENT_TYPE_VALUETYPE      = 0x11,
        ELEMENT_TYPE_CLASS          = 0x12,
        ELEMENT_TYPE_VAR            = 0x13,
        ELEMENT_TYPE_ARRAY          = 0x14,
        ELEMENT_TYPE_GENERICINST    = 0x15,
        ELEMENT_TYPE_TYPEDBYREF     = 0x16,

        ELEMENT_TYPE_I              = 0x18,
        ELEMENT_TYPE_U              = 0x19,
        ELEMENT_TYPE_FNPTR          = 0x1B,
        ELEMENT_TYPE_OBJECT         = 0x1C,
        ELEMENT_TYPE_SZARRAY        = 0x1D,
        ELEMENT_TYPE_MVAR           = 0x1e,

        ELEMENT_TYPE_CMOD_REQD      = 0x1F,
        ELEMENT_TYPE_CMOD_OPT       = 0x20,

        ELEMENT_TYPE_INTERNAL       = 0x21,
        ELEMENT_TYPE_MAX            = 0x22,

        ELEMENT_TYPE_MODIFIER       = 0x40,
        ELEMENT_TYPE_SENTINEL       = 0x01 | ELEMENT_TYPE_MODIFIER,
        ELEMENT_TYPE_PINNED         = 0x05 | ELEMENT_TYPE_MODIFIER,
        ELEMENT_TYPE_R4_HFA         = 0x06 | ELEMENT_TYPE_MODIFIER,
        ELEMENT_TYPE_R8_HFA         = 0x07 | ELEMENT_TYPE_MODIFIER
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

}