#pragma once
//////////////////////////////////////////////////////////////////////////////
// ProfilerStats.h  - Counters to track the number of profiler callbacks    //
//                    that this profiler receives. Apart from that we also  //
//                    track other events for interest (esp GC events).      //
// Application      - Orange Profiler.                                      //
// Author           - Mithun Shanbhag, mithuns@microsoft.com                //
//////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////
// struct declaration : ProfilerStats //
////////////////////////////////////////

struct ProfilerStats
{
public:
    LONG cInitialize;
    LONG cShutdown;
    LONG cAppDomainCreationFinished;
    LONG cAppDomainCreationStarted;
    LONG cAppDomainShutdownFinished;
    LONG cAppDomainShutdownStarted;
    LONG cAssemblyLoadFinished;
    LONG cAssemblyLoadStarted;
    LONG cAssemblyUnloadFinished;
    LONG cAssemblyUnloadStarted;
    LONG cClassLoadFinished;
    LONG cClassLoadStarted;
    LONG cClassUnloadFinished;
    LONG cClassUnloadStarted;
    LONG cCOMClassicVTableCreated;
    LONG cCOMClassicVTableDestroyed;
    LONG cExceptionCatcherEnter;
    LONG cExceptionCatcherLeave;
    LONG cExceptionCLRCatcherExecute;
    LONG cExceptionCLRCatcherFound;
    LONG cExceptionOSHandlerEnter;
    LONG cExceptionOSHandlerLeave;
    LONG cExceptionSearchCatcherFound;
    LONG cExceptionSearchFunctionEnter;
    LONG cExceptionSearchFunctionLeave;
    LONG cExceptionSearchFilterEnter;
    LONG cExceptionSearchFilterLeave;
    LONG cExceptionThrown;
    LONG cExceptionUnwindFinallyEnter;
    LONG cExceptionUnwindFinallyLeave;
    LONG cExceptionUnwindFunctionEnter;
    LONG cExceptionUnwindFunctionLeave;
    LONG cFunctionUnloadStarted;
    LONG cJITCachedFunctionSearchFinished;
    LONG cJITCachedFunctionSearchStarted;
    LONG cJITCompilationFinished;
    LONG cJITCompilationStarted;
    LONG cJITFunctionPitched;
    LONG cJITInlining;
    LONG cManagedToUnmanagedTransition;
    LONG cModuleAttachedToAssembly;
    LONG cModuleLoadFinished;
    LONG cModuleLoadStarted;
    LONG cModuleUnloadFinished;
    LONG cModuleUnloadStarted;
    LONG cMovedReferences;
    LONG cObjectAllocated;
    LONG cObjectReferences;
    LONG cObjectsAllocatedByClass;
    LONG cThreadCreated;
    LONG cThreadDestroyed;
    LONG cThreadAssignedToOSThread;
    LONG cRemotingClientInvocationStarted;
    LONG cRemotingClientSendingMessage;
    LONG cRemotingClientReceivingReply;
    LONG cRemotingClientInvocationFinished;
    LONG cRemotingServerReceivingMessage;
    LONG cRemotingServerInvocationStarted;
    LONG cRemotingServerInvocationReturned;
    LONG cRemotingServerSendingReply;
    LONG cUnmanagedToManagedTransition;
    LONG cRuntimeSuspendStarted;
    LONG cRuntimeSuspendFinished;
    LONG cRuntimeSuspendAborted;
    LONG cRuntimeResumeStarted;
    LONG cRuntimeResumeFinished;
    LONG cRuntimeThreadSuspended;
    LONG cRuntimeThreadResumed;
    LONG cRootReferences;

    // ICorProfilerCallBack2 Implementation

    LONG cThreadNameChanged;
    LONG cGarbageCollectionStarted;
    LONG cSurvivingReferences;
    LONG cGarbageCollectionFinished;
    LONG cFinalizeableObjectQueued;
    LONG cRootReferences2;
    LONG cHandleCreated;
    LONG cHandleDestroyed;

    // ICorProfilerCallBack3 Implementation
    
    LONG cInitializeForAttach;
    LONG cProfilerAttachComplete;
    LONG cProfilerDetachSucceeded;

    // other GC counters of interest

    LONG cRuntimeSuspensionsForGC;
    LONG cGen0Collections;
    LONG cGen1Collections;
    LONG cGen2Collections;
    LONG cLOHCollections;
    LONG cInducedGCs;
};