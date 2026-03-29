#pragma once


#include "common.hxx"

//////////////////////////////////
// class declaration : CSHolder //
//////////////////////////////////

class Synchronize
{
public:

    Synchronize(CRITICAL_SECTION * pcs)
    {
        m_pcs = pcs;
        EnterCriticalSection(m_pcs);
    }
    
	~ Synchronize()
    {
        LeaveCriticalSection(m_pcs);
    }

protected:
    
	CRITICAL_SECTION * m_pcs;
};
