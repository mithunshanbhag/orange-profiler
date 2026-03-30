#pragma once
//////////////////////////////////////////////////////////////////////
// ProfilerHelper.h	- Includes some useful helper routines. 		//
// Application 		- Orange Profiler.                              //
// Author      		- Mithun Shanbhag                                  //
//////////////////////////////////////////////////////////////////////

#include "common.hxx"


///////////////////////////////////
// enumeration : ProfilerOptions //
///////////////////////////////////

/* The Orange profiler's activities are controlled by these profiler options.  
   NOTE: These flags can be "OR"ed together. 
*/

enum SnapshotOptions 
{
    NONE 								= 0x00000000,

    // Retrieves the virtual-address-space info at each snapshot point.
    ENABLE_VADUMP     					= 0x00000001, 

	// Record call-stacks for handle allocations.
	// Note: Not available for attaching profilers.
	ENABLE_HANDLE_ALLOCATION_CALLSTACKS	= 0x00000002,

	// Record call-stacks for object allocations.
	// Note: Not available for attaching profilers.
	ENABLE_OBJECT_ALLOCATION_CALLSTACKS	= 0x00000004,

	// Tracks object allocations, relocations & collections across 
	// snapshot points (warning: will incur performance penalty).
    ENABLE_OBJECT_LIFETIME_TRACKING  	= 0x00000008, 

    // Performs user specified action at each snapshot point.
    CUSTOM_SNAPSHOT_ACTION 				= 0x00000010 
};


////////////////////////////////////////
// struct : AttachingProfilerOptions //
///////////////////////////////////////

/* 	Traditionally the trigger process passes in the setting by first setting environment
	variables and then by launching the profilee. This howerver does not work for profiler
	attach (since you cannt change the environment variables after the process has been
	launched). Hence we use the "clientData" field of the AttachProfiler API to marshal
	the profiler settings across from trigger process to the profilee.
*/

struct AttachingProfilerOptions
{
	wchar_t wszDiagnosticInfo[10];	
	wchar_t wszEnableVadump[10];
	wchar_t wszEnableObjectLifetimeTracking[10];
	wchar_t wszTraceFileName[256];
	wchar_t wszCustomSnapshotAction[1024];
	wchar_t wszSnapshotInterval[256];
	wchar_t wszProfilerDetachInterval[256];
	wchar_t wszProfilerInitAckEvent[256];
	wchar_t wszProfilerDetachAckEvent[256];
};

/////////////////////////////////////
// struct declaration : ThreadData //
/////////////////////////////////////

struct ThreadData
{
	DWORD managedThreadId;
	DWORD win32ThreadId;
	std::vector<FunctionID> callStack; // std::stack do not support iterators. hence using std::vector instead.
	DWORD unwindingFunctionId;
};

////////////////////////////////////////
// struct declaration : ObjectPayload //
////////////////////////////////////////

struct ObjectPayload
{
	ObjectID objectId;
	ClassID classId;
	vector<ObjectID> objRefs; 
};

//////////////////////////////////////
// struct declaration : ObjectRange //
//////////////////////////////////////

struct ObjectRange
{
	ObjectID oldObjectRangeStart; 
	ObjectID newObjectRangeStart; 
	ULONG lenObjectRange; 
};

///////////////////////////////////////
// struct declaration : ClassPayload //
///////////////////////////////////////

struct ClassPayload
{
	ClassID classId;
	ModuleID moduleId;
	vector<mdFieldDef> fieldDefs; 
};


///////////////////////////////////////
// class declaration : ProcessHelper //
///////////////////////////////////////

class ProfilerHelper
{
 public:

	ProfilerHelper() { Clear(); }

	~ProfilerHelper() { Clear(); }

	HRESULT Init(BOOL bAttachingProfiler, PVOID pvData)
	{
		HRESULT hr = S_OK;

		IsAttachingProfiler = bAttachingProfiler;

		if (IsAttachingProfiler)
		{
			if (NULL == pvData)
			{
		        hr = E_INVALIDARG;
		        TRACEERROR(hr, L"Invalid arg passed into ProfilerHelper::Init -> pvData");
		        goto ErrReturn;
			}

			AttachingProfilerOptions * opts = (AttachingProfilerOptions*) pvData;

			if (NULL != opts->wszCustomSnapshotAction) 
			{
				wcscpy(wszCustomSnapshotAction, opts->wszCustomSnapshotAction);
				dwSnapshotOptions |= CUSTOM_SNAPSHOT_ACTION;
			}

			if (NULL != opts->wszTraceFileName)
				wcscpy(wszTraceFileName, opts->wszTraceFileName);

			if (NULL != opts->wszProfilerInitAckEvent)
				wcscpy(wszProfilerInitAckEvent, opts->wszProfilerInitAckEvent);

			if (NULL != opts->wszProfilerDetachAckEvent)
				wcscpy(wszProfilerDetachAckEvent, opts->wszProfilerDetachAckEvent);

			if (NULL != opts->wszSnapshotInterval)
				dwSnapshotInterval = _wtoi((const wchar_t *) opts->wszSnapshotInterval);

			if (NULL != opts->wszProfilerDetachInterval)
				dwProfilerDetachInterval = _wtoi((const wchar_t *) opts->wszProfilerDetachInterval);

			if (NULL != opts->wszDiagnosticInfo)
		        if (0 == wcscmp(opts->wszDiagnosticInfo, L"1"))
		            g_DiagnosticInfoLevel |= TRACELEVEL_DIAGNOSTIC; // It is unfortunate that we are changing global state.

			if (NULL != opts->wszEnableVadump)
        		if (0 == wcscmp(opts->wszEnableVadump, L"1"))
            		dwSnapshotOptions |= ENABLE_VADUMP;

			if (NULL != opts->wszEnableObjectLifetimeTracking)
        		if (0 == wcscmp(opts->wszEnableObjectLifetimeTracking, L"1"))
            		dwSnapshotOptions |= ENABLE_OBJECT_LIFETIME_TRACKING;

			// NOTE: We shall not enable the following options for attaching profilers
			// since they require profiler functionality unavailable on profiler-attach.
			// - ENABLE_HANDLE_ALLOCATION_CALLSTACKS
			// - ENABLE_OBJECT_ALLOCATION_CALLSTACKS
		}

		else
		{
			if (NULL != _wgetenv(L"OP_CUSTOM_SNAPSHOT_ACTION")) 
			{
				wcscpy(wszCustomSnapshotAction, _wgetenv(L"OP_CUSTOM_SNAPSHOT_ACTION"));
				dwSnapshotOptions |= CUSTOM_SNAPSHOT_ACTION;
			}

	    	if (NULL != _wgetenv(L"OP_TRACE_FILE_NAME"))
				wcscpy(wszTraceFileName, _wgetenv(L"OP_TRACE_FILE_NAME"));

	    	if (NULL != _wgetenv(L"OP_INITIALIZATION_ACK_EVENT"))
				wcscpy(wszProfilerInitAckEvent, _wgetenv(L"OP_INITIALIZATION_ACK_EVENT"));

			if (NULL != _wgetenv(L"OP_DETACH_ACK_EVENT"))
				wcscpy(wszProfilerDetachAckEvent, _wgetenv(L"OP_DETACH_ACK_EVENT"));

			if (NULL != _wgetenv(L"OP_SNAPSHOT_INTERVAL"))
				dwSnapshotInterval = _wtoi((const wchar_t *) _wgetenv(L"OP_SNAPSHOT_INTERVAL"));

			if (NULL != _wgetenv(L"OP_DETACH_INTERVAL"))
				dwProfilerDetachInterval = _wtoi((const wchar_t *) _wgetenv(L"OP_DETACH_INTERVAL"));

			if (NULL != _wgetenv(L"OP_ENABLE_DIAGNOSTIC_INFO"))
		        if (0 == wcscmp(_wgetenv(L"OP_ENABLE_DIAGNOSTIC_INFO"), L"1"))
		            g_DiagnosticInfoLevel |= TRACELEVEL_DIAGNOSTIC; // It is unfortunate that we are changing global state.

			if (NULL != _wgetenv(L"OP_ENABLE_VADUMP"))
        		if (0 == wcscmp(_wgetenv(L"OP_ENABLE_VADUMP"), L"1"))
            		dwSnapshotOptions |= ENABLE_VADUMP;

			if (NULL != _wgetenv(L"OP_ENABLE_OBJECT_LIFETIME_TRACKING"))
        		if (0 == wcscmp(_wgetenv(L"OP_ENABLE_OBJECT_LIFETIME_TRACKING"), L"1"))
            		dwSnapshotOptions |= ENABLE_OBJECT_LIFETIME_TRACKING;

			if (NULL != _wgetenv(L"OP_ENABLE_HANDLE_ALLOCATION_CALLSTACKS"))
        		if (0 == wcscmp(_wgetenv(L"OP_ENABLE_HANDLE_ALLOCATION_CALLSTACKS"), L"1"))
            		dwSnapshotOptions |= ENABLE_HANDLE_ALLOCATION_CALLSTACKS;

			if (NULL != _wgetenv(L"OP_ENABLE_OBJECT_ALLOCATION_CALLSTACKS"))
        		if (0 == wcscmp(_wgetenv(L"OP_ENABLE_OBJECT_ALLOCATION_CALLSTACKS"), L"1"))
            		dwSnapshotOptions |= ENABLE_OBJECT_ALLOCATION_CALLSTACKS;
		}

	/////////////////////////////////////////////////////
	ErrReturn:

	    return hr;
	}



	void Clear()
	{
		IsAttachingProfiler = FALSE;

		ZeroMemory(wszCustomSnapshotAction, 1024 * sizeof(WCHAR));

		ZeroMemory(wszTraceFileName, 1024 * sizeof(WCHAR));

		ZeroMemory(wszProfilerInitAckEvent, 256 * sizeof(WCHAR));

		ZeroMemory(wszProfilerDetachAckEvent, 256 * sizeof(WCHAR));

		dwSnapshotInterval = 0;

		dwProfilerDetachInterval = 0;

		dwSnapshotOptions = NONE; // change this later. 
	}

	// @TODO - push this somewhere else
	LPCWSTR ProfilerHelper::GetRuntimeTypeString(COR_PRF_RUNTIME_TYPE runtimetype)
	{
	    switch(runtimetype)
	    {
	        case COR_PRF_DESKTOP_CLR:   return (LPCWSTR) L"desktop";
	        case COR_PRF_CORE_CLR:      return (LPCWSTR) L"core";
	        default:                    return (LPCWSTR) L"unknown";
	    }
	}


	// custom action to take at each snapshot point.
    WCHAR wszCustomSnapshotAction[1024];

	// name of the trace file.
	WCHAR wszTraceFileName[1024];

	// name of the profiler initialization acknowledgement event.
	WCHAR wszProfilerInitAckEvent[256];

	// name of the profiler detach acknowledgement event.
	WCHAR wszProfilerDetachAckEvent[256];

	// time interval (in seconds) between snapshot requests (ForceGC).
	DWORD dwSnapshotInterval;

	// time interval (in seconds) between profiler-detach requests. 
	DWORD dwProfilerDetachInterval;

	// snapshot options. 
    DWORD dwSnapshotOptions;

	// Is this an startup-load or an attaching profiler?
	BOOL IsAttachingProfiler;

};

///////////////////////////////////////
// class declaration : ProcessHelper //
///////////////////////////////////////

//*********************************************************************
// Descripton: Retrieves the process's handle, ID, name and full-path.
// Also captures system specific information (in a SYSTEM_INFO 
// structure).
//  
// Notes: This helper class uses the TRACE, TRACEERROR macros to 
// log any errors. Please ensure that this method is called only
// after the underlying log-stream has been initialized.
//*********************************************************************

class ProcessHelper
{
public:

	ProcessHelper()
		: hProcess (NULL) { Clear(); }

	~ProcessHelper() { Clear(); }

	HRESULT Init()
	{
	    HRESULT hr = S_OK;
	    
	    if (NULL == (hProcess = GetCurrentProcess())) 
		{
	        hr = HRESULT_FROM_WIN32(GetLastError());
	        TRACEERROR(hr, L"Failed to get currrent process's handle.");
	        goto ErrReturn;
	    }

	    if (0 == (dwPid = GetCurrentProcessId())) 
		{
	        hr = HRESULT_FROM_WIN32(GetLastError());
	        TRACEERROR(HRESULT_FROM_WIN32(GetLastError()), L"Failed to get currrent process's ID.");
	        goto ErrReturn;
	    }

	    if (0 == GetModuleFileName(NULL, wszFilePath, 2 * MAX_PATH)) 
		{
	        hr = HRESULT_FROM_WIN32(GetLastError());
	        TRACEERROR(hr, L"Call to Kernel32!GetModuleFileName failed.");
	        goto ErrReturn;
	    }

	    if (0 == GetModuleBaseName(hProcess, NULL, wszFileName, 2 * MAX_PATH)) 
		{
	        hr = HRESULT_FROM_WIN32(GetLastError());
	        TRACEERROR(hr, L"Call to Psapi!GetModuleBaseName failed.");
	        goto ErrReturn;
	    }

		if (NULL != hProcess) 
	        IsWow64Process(hProcess, & IsWOW64Process); 

	    // The MSDN documentation says that if we are running in WOW64, then we must
	    // call GetNativeSystemInfo() instead of GetSystemInfo() to retrieve the 
	    // highest address accessible to the app. 
	    // However I've observed this - 
	    // If I call GetNativeSystemInfo() on WOW64, the sysInfo.lpMaximumApplicationAddress is 0xFFFEFFFF (which is bogus).
	    // While if I call GetSystemInfo() on WOW64, the sysInfo.lpMaximumApplicationAddress is 0x7FFEFFFF (which looks correct).
	    // Temporarily commenting out the code below until I can figure this one out - 
	    /*  
	    if (TRUE == bIsWOW64Process) 
	        GetNativeSystemInfo(& sysInfo);
	    else */ 
	        GetSystemInfo(& sysInfo);

	    if (NULL == sysInfo.lpMaximumApplicationAddress) 
		{
	        hr = E_FAIL;
	        TRACEERROR(hr, L"sysInfo.lpMaximumApplicationAddress is NULL");
	        goto ErrReturn;
	    }

	    /////////////////////////////////////////////////////
	ErrReturn:

	    return hr;
	}


 private:

	void Clear()
	{
	    if (NULL != hProcess) 
		{
	        CloseHandle(hProcess);
	        hProcess = NULL;
	    }

		dwPid = 0;

		ZeroMemory(wszFilePath, 2 * MAX_PATH * sizeof(WCHAR));

		ZeroMemory(wszFileName, 2 * MAX_PATH * sizeof(WCHAR));

	    ZeroMemory(& sysInfo, sizeof(sysInfo));

		IsWOW64Process = FALSE;
	}


 public:

	// pseudo process handle 
    HANDLE hProcess;  

	// process-Id of the profilee
	DWORD dwPid;  
    
	// filepath of the profilee process 
	WCHAR wszFilePath[2 * MAX_PATH]; 
    
	// name of the profilee process
	WCHAR wszFileName[2 * MAX_PATH]; 

	// some basic system info
    SYSTEM_INFO sysInfo;

    BOOL IsWOW64Process;
};