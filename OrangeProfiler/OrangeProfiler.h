#pragma once
//////////////////////////////////////////////////////////////////////////
// Application      - Orange Profiler.                                  //
// Author           - Mithun Shanbhag                                   //
//////////////////////////////////////////////////////////////////////////

/*
 * Design Notes
 * ============
 *
 *  @TODO.
 * 
 * 
 * 
 */


/*
 * Version History
 * ===============
 *  Version 0.90 - Early Prototype.
 * 
 * Planned Bug-fixes
 * =================
 * 
 *  - Malformed XML: After parsing the field signature, if the field is of ELEMENT_TYPE_CLASS, we 
 *    publish its class name. However the name can contain '<' and '>' characters which lead to 
 *    malformed XML. E.g. : '<CrtImplementationDetails>.ModuleUninitializer'. The fix involves 
 *    replacing those characters with "&lt;" and "&gt;" respectively.
 * 
 *  - Files not in use. Some .h, .cpp files are not in use and can be safely removed.
 *    - ProfilerOptions.h
 *    - ProfilerHelper.cpp
 *
 *  - Macros: Can be optimized!
 *
 *  - Tracking "free" objects: The method OrangeProfiler::TrackFreeObjects has a small bug. We never 
 *    take into consideration the free objects that lie between start of generation boundaries and 
 *    first object range in that generation. 
 *
 * Planned Design Changes
 * ======================
 *
 *  - Acknowledgement mechanism: We have a mechanism wherein the profiler notifies any listening client 
 *    of special events like successful profiler initialization, profiler detach, GC triggering etc. Frankly 
 *    the current implementation is a bit of a mess and needs to be redesigned.
 *
 *  - Lifetime management: Need to carefully manage the lifetimes of certain entities. Currently we rely 
 *    on this ordering -  
 * 	  lifetime of process > lifetime of loaded runtime(s) > lifetime of profiler(s) > profiling-sessions.
 *
 *  - Appdomain info: Currently we are unable to dump out the "AppDomainID" to "AppDomain Name" mapping. The
 *    root cause is that ICorProfilerInfo::GetAppdomainInfo() is a GC_TRIGGER method that cannot be called
 *    from within the ICorProfilerCallback::GarbageCollectionFinished callback. Calling GetAppdomainInfo
 *    from RuntimeResumeStarted is probably an alternative. This needs to be investigated further.
 * 
 *  - W3wp app-pool recycling: The profiler does not get any shutdown notifications when the app-pool recyles. 
 *    As a result, the opening <trace> tag is never closed with a matching </trace> tag. This leaves the trace 
 *    file in a malformed state.
 * 
 *  - OrangeProfiler::CommonInit: This method does a ton of initialization work. This can easily be 
 *    split up into smaller methods that are more focused on a single activities. 
 *
 *  - Well-formed XML: Often times the type/field/module names will contain characters like '<' and '>' 
 *    indicating generic type args. Since we are writing out the trace file in XML format, we have to replace 
 *    those character with "&lt;" and "&gt;" respectively. Currently the entire code is littered with patches 
 *    to the type/field/module names. We could definitely do with a common/helper routine that does this.

 *  - Duplicating work to retrieve object info: For each objectID, it seems that we call ObjectInfo::Init()
 *    on it twice. The first time in the DumpObjects() method and the next time in the TrackFreeObjects()
 *    method. We can probably consolidate this activity in a single place.
 * 
 *  - Payloads : We have created some payload structures (ObjectPayload, ClassPayload) to
 *    help ferry some complex data from various profiler callbacks to the GarbageCollectionFinished
 *    callbacks. This whole 'payload' thing can probably be done away with with some design changes. 
 *
 *  - Manipulation of global state from within a profiler instance: We are tweaking g_DiagnosticInfoLevel when
 *    the profiler initializes. This is bad. We must future proof our design to be InProc-SxS resistant.
 * 
 *  - ProfilerStats: Although we have included the profilerstats.h header, we aren't using it. We probably 
 *    could put it to some use. Think about this.
 *
 *  - Policy decision to stop (or continue or assert) on Errors: Currently on errors, we log the error to the 
 *    debug stream and bail out gracefully. We need a mechanism to immediately stop on errors (say process
 *    termination). 
 * 
 * Planned Features
 * ================
 *
 *  - Enable call-stack tracking for GCHandles and object allocation (using Shadow stacks via ELT).  
 *
 *  - Currently the orange profiler works only on CLR-v4 processes (we cancel activation on non V4 runtimes). 
 *    It is a future goal to make the profiler run on CLR-v2 as well in InProc-SxS situations.
 * 
 */


#include "ProfilerHelper.h"
#include "ProfilerStats.h"
#include "Synchronize.h"
#include "EntityInfo.h"
#include "SigFormat.h" 


// Requests from UI/Client

#define NAMED_EVENT_REQ_FORCEGC         		L"Global\\OP_NAMED_EVENT_REQ_FORCEGC"
#define NAMED_EVENT_REQ_PROFILERDETACH  		L"Global\\OP_NAMED_EVENT_REQ_PROFILERDETACH"
#define NAMED_TIMER_REQ_FORCEGC         		L"Global\\OP_NAMED_TIMER_REQ_FORCEGC"
#define NAMED_TIMER_REQ_PROFILERDETACH  		L"Global\\OP_NAMED_TIMER_REQ_PROFILERDETACH"


enum WaitReqEventHandles
{ 
    FORCEGC_REQ_EVENT_HANDLE  = 0, 
    DETACH_REQ_EVENT_HANDLE   = 1,
    FORCEGC_REQ_TIMER_HANDLE  = 2, 
    DETACH_REQ_TIMER_HANDLE   = 3, 
    SENTINEL_HANDLE       	  = 4 // always keep the sentinel as the last value
};


// Acknowledgements by profiler

#define NAMED_EVENT_ACK_PROFILER_INITIALIZED  	L"Global\\OP_NAMED_EVENT_ACK_PROFILER_INITIALIZED"
#define NAMED_EVENT_ACK_FORCEGC         		L"Global\\OP_NAMED_EVENT_ACK_FORCEGC"
#define NAMED_EVENT_ACK_PROFILER_DETACHED  		L"Global\\OP_NAMED_EVENT_ACK_PROFILER_DETACHED"


// Global thread Routines

unsigned __stdcall _ListenerThreadStub(
    void *p
    );



////////////////////////////////////////
// class declaration : OrangeProfiler //
////////////////////////////////////////

class OrangeProfiler : public ICorProfilerCallback3
{
  protected:

	static OrangeProfiler * _This;

	//
    // Process and system specific members-variables 
	//

	ProcessHelper ProcessHelper;

	//
    // Runtime specific members-variables 
	//

	// The Info pointer (valid as long as the runtime is loaded).
	ICorProfilerInfo3 * m_pInfo;  
    
	//
    // Profiler specific members-variables 
	//




	// reference count 
	long m_cRef; 

	CRITICAL_SECTION m_cs;     

	// TLS index
	DWORD m_dwTLSIndex; 

	// Listener thread id
	DWORD m_dwListenerThreadId; 

	// Listener thread handle
	HANDLE m_hndListenerThread; 

	// should listener thread exit?
	BOOL m_bListenerThreadExit; 

	HANDLE m_hndWaitReqHandles[(DWORD) SENTINEL_HANDLE]; 

	// handle to profiler-init acknowledgement event
	HANDLE m_hndAckProfilerInitialized; 

	// handle to profiler-detach acknowledgement event
	HANDLE m_hndAckProfilerDetached; 


	//
    // Profiling session specific members-variables 
	//

	ProfilerHelper ProfilerHelper;

	// tracking condemned generations (and gc-nesting levels)
	stack<ULONG> m_condemnedGeneration; 
 
	vector<ObjectPayload> m_objectPayloads; 

	vector<ClassPayload> m_classPayloads; 

	vector<ObjectRange> m_vecObjectRanges;

	vector<COR_PRF_GC_GENERATION_RANGE> m_vecGCGenerationRanges;

	set<FunctionID> m_functions;

    HRESULT DumpGCGenerationRanges(
		vector<COR_PRF_GC_GENERATION_RANGE> &
		);
  
    HRESULT TrackFreeObjects(
		vector<ObjectRange> &,
		vector<COR_PRF_GC_GENERATION_RANGE> &,
		vector<ObjectPayload> &
        );

	HRESULT DumpObjects(
		vector<ObjectPayload> &,
		set<ClassID> &
        );

	HRESULT DumpClasses(
		set<ClassID> &,
		vector<ClassPayload> &,
		set<ModuleID> &
        );

    HRESULT DumpModules(
		set<ModuleID> &, 
		set<AssemblyID> &
        );

    HRESULT DumpAssemblies(
		set<AssemblyID> &,
		set<AppDomainID> &
		);

    HRESULT DumpAppdomains(
		set<AppDomainID> &
		);

	HRESULT DumpFields(
		vector<ClassPayload> &,
		set<AppDomainID> &
		);

	HRESULT DumpJittedFunctions(
		);

	HRESULT DumpEncounteredFunctions(
		);

    HRESULT CustomSnapshotAction(
        );

    HRESULT RetrieveSystemAndProcessInfo(
        );

    HRESULT DumpVirtualAddressSpace(
        );

    HRESULT CommonInit(
        IUnknown * pUnk, 
		BOOL IsAttachingProfiler,
		void * pvClientData, 
		UINT cbClientData
        );

   
  public:

    OrangeProfiler();
  
    virtual ~ OrangeProfiler(); 

    void ListenerThreadStubWrapper(
        );

    static OrangeProfiler * Instance(
		) { 
			return _This; 
		}

	//
    // ICorProfilerCallback implementation
	//

    virtual HRESULT __stdcall Initialize(
        IUnknown * pICorProfilerInfoUnk
        );

    virtual HRESULT __stdcall Shutdown(
        );

    virtual HRESULT __stdcall AppDomainCreationFinished(
        AppDomainID appDomainId,
        HRESULT hrStatus
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall AppDomainCreationStarted(
        AppDomainID appDomainId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall AppDomainShutdownFinished(
        AppDomainID appDomainId,
        HRESULT hrStatus
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall AppDomainShutdownStarted(
        AppDomainID appDomainId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall AssemblyLoadFinished(
        AssemblyID assemblyId,
        HRESULT hrStatus
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall AssemblyLoadStarted(
        AssemblyID assemblyId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall AssemblyUnloadFinished(
        AssemblyID assemblyId,
        HRESULT hrStatus
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall AssemblyUnloadStarted(
        AssemblyID assemblyId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ClassLoadFinished(
        ClassID classId,
        HRESULT hrStatus
		)	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ClassLoadStarted(
        ClassID classId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ClassUnloadFinished(
        ClassID classId,
        HRESULT hrStatus
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ClassUnloadStarted(
        ClassID classId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall COMClassicVTableCreated(
        ClassID wrappedClassId,
        REFGUID implementedIID,
        void * pVTable,
        ULONG cSlots
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall COMClassicVTableDestroyed(
        ClassID wrappedClassId,
        REFGUID implementedIID,
        void * pVTable
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionCatcherEnter(
        FunctionID functionId,
        ObjectID objectId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionCatcherLeave(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionCLRCatcherExecute(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionCLRCatcherFound(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionOSHandlerEnter(
        UINT_PTR __unused
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionOSHandlerLeave(
        UINT_PTR __unused
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionSearchCatcherFound(
        FunctionID functionId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionSearchFunctionEnter(
        FunctionID functionId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionSearchFunctionLeave(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionSearchFilterEnter(
        FunctionID functionId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionSearchFilterLeave(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionThrown(
        ObjectID thrownObjectId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionUnwindFinallyEnter(
        FunctionID functionId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionUnwindFinallyLeave(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ExceptionUnwindFunctionEnter(
        FunctionID functionId
        );

    virtual HRESULT __stdcall ExceptionUnwindFunctionLeave(
        );

    virtual HRESULT __stdcall FunctionUnloadStarted(
        FunctionID functionId
		)	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall JITCachedFunctionSearchFinished(
        FunctionID functionId,
        COR_PRF_JIT_CACHE result
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall JITCachedFunctionSearchStarted(
        FunctionID functionId,
        BOOL * pbUseCachedFunction
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall JITCompilationFinished(
        FunctionID functionId,
        HRESULT hrStatus,
        BOOL fIsSafeToBlock
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall JITCompilationStarted(
        FunctionID functionId,
        BOOL fIsSafeToBlock
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall JITFunctionPitched(
        FunctionID functionId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall JITInlining(
        FunctionID callerId,
        FunctionID calleeId,
        BOOL * pfShouldInline
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ManagedToUnmanagedTransition(
        FunctionID functionId,
        COR_PRF_TRANSITION_REASON reason
        );

    virtual HRESULT __stdcall ModuleAttachedToAssembly(
        ModuleID moduleId,
        AssemblyID AssemblyId
        )	{ return E_NOTIMPL; }
        
    virtual HRESULT __stdcall ModuleLoadFinished(
        ModuleID moduleId,
        HRESULT hrStatus
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ModuleLoadStarted(
        ModuleID moduleId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ModuleUnloadFinished(
        ModuleID moduleId,
        HRESULT hrStatus
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ModuleUnloadStarted(
        ModuleID moduleId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall MovedReferences(
        ULONG cMovedObjectIDRanges,
        ObjectID oldObjectIDRangeStart[] ,
        ObjectID newObjectIDRangeStart[] ,
        ULONG cObjectIDRangeLength[]
        );

    virtual HRESULT __stdcall ObjectAllocated(
        ObjectID objectId,
        ClassID classId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ObjectReferences(
        ObjectID objectId,
        ClassID classId,
        ULONG cObjectRefs,
        ObjectID objectRefIds[]
        );

    virtual HRESULT __stdcall ObjectsAllocatedByClass(
        ULONG cClassCount,
        ClassID classIds[],
        ULONG cObjects[]
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall ThreadCreated(
        ThreadID threadId
        );

    virtual HRESULT __stdcall ThreadDestroyed(
        ThreadID threadId
        );

    virtual HRESULT __stdcall ThreadAssignedToOSThread(
        ThreadID managedThreadId,
        DWORD osThreadId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingClientInvocationStarted(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingClientSendingMessage(
        GUID * pCookie,
        BOOL fIsAsync
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingClientReceivingReply(
        GUID * pCookie,
        BOOL fIsAsync
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingClientInvocationFinished(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingServerReceivingMessage(
        GUID * pCookie,
        BOOL fIsAsync
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingServerInvocationStarted(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingServerInvocationReturned(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RemotingServerSendingReply(
        GUID * pCookie,
        BOOL fIsAsync
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall UnmanagedToManagedTransition(
        FunctionID functionId,
        COR_PRF_TRANSITION_REASON reason
        );

    virtual HRESULT __stdcall RuntimeSuspendStarted(
        COR_PRF_SUSPEND_REASON suspendReason
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RuntimeSuspendFinished(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RuntimeSuspendAborted(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RuntimeResumeStarted(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RuntimeResumeFinished(
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RuntimeThreadSuspended(
        ThreadID threadId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RuntimeThreadResumed(
        ThreadID threadId
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RootReferences(
        ULONG    cRootRefs,
        ObjectID rootRefIds[]
        )	{ return E_NOTIMPL; }

	//
    // ICorProfilerCallBack2 Implementation
	//

    virtual HRESULT __stdcall ThreadNameChanged(
        ThreadID threadId,
        ULONG cchName,
        WCHAR name[]
        )	{ return E_NOTIMPL; }   

    virtual HRESULT __stdcall GarbageCollectionStarted(
        int cGenerations,
        BOOL generationCollected[],
        COR_PRF_GC_REASON reason
        );

    virtual HRESULT __stdcall SurvivingReferences(
        ULONG cSurvivingObjectIDRanges,
        ObjectID objectIDRangeStart[] ,
        ULONG cObjectIDRangeLength[]
        );

    virtual HRESULT __stdcall GarbageCollectionFinished(
        ); 

    virtual HRESULT __stdcall FinalizeableObjectQueued(
        DWORD finalizerFlags,
        ObjectID objectID
        )	{ return E_NOTIMPL; }

    virtual HRESULT __stdcall RootReferences2(
        ULONG cRootRefs,
        ObjectID rootRefIds[],
        COR_PRF_GC_ROOT_KIND rootKinds[],
        COR_PRF_GC_ROOT_FLAGS rootFlags[],
        UINT_PTR rootIds[]
        );

    virtual HRESULT __stdcall HandleCreated(
        GCHandleID handleId,
        ObjectID initialObjectId
        );

    virtual HRESULT __stdcall HandleDestroyed(
        GCHandleID handleId
        );
        
	//
    // ICorProfilerCallBack3 Implementation
    //

    virtual HRESULT __stdcall InitializeForAttach(
        IUnknown * pICorProfilerInfoUnk,
        void * pvClientData,
        UINT cbClientData
        );
    
    virtual HRESULT __stdcall ProfilerAttachComplete(
        );

    virtual HRESULT __stdcall ProfilerDetachSucceeded(
        );
    
	//        
    // This is ultimately derived from IUnknown. So....
	//

    virtual HRESULT __stdcall QueryInterface(
        const IID & iid, 
        void** ppv
        );

    virtual ULONG __stdcall AddRef(
        );

    virtual ULONG __stdcall Release(
        );

	//
	// Enter, Leave, Tailcall hooks
	//
	
	void FunctionEnter3CallBack(
		FunctionIDOrClientID functionIDOrClientID
		);

	void FunctionLeave3CallBack(
		FunctionIDOrClientID functionIDOrClientID
		);

	void FunctionTailCall3CallBack(
		FunctionIDOrClientID functionIDOrClientID
		);

};
