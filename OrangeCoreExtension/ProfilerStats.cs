///////////////////////////////////////////////////////////////////////////////
// ProfilerStats.cs : @todo.                                                 //
// Application      : CLR V4 Profiler Test Infrastructure                    //
// Author           : Mithun Shanbhag, mithuns@microsoft.com                 //
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrangeClient
{
    ////////////////////////////////////
    // ----< class ProfilerStats >----//
    ////////////////////////////////////

    class ProfilerStats
    {
        public uint cInitialize = 0;
        public uint cShutdown = 0;
        public uint cAppDomainCreationFinished = 0;
        public uint cAppDomainCreationStarted = 0;
        public uint cAppDomainShutdownFinished = 0;
        public uint cAppDomainShutdownStarted = 0;
        public uint cAssemblyLoadFinished = 0;
        public uint cAssemblyLoadStarted = 0;
        public uint cAssemblyUnloadFinished = 0;
        public uint cAssemblyUnloadStarted = 0;
        public uint cClassLoadFinished = 0;
        public uint cClassLoadStarted = 0;
        public uint cClassUnloadFinished = 0;
        public uint cClassUnloadStarted = 0;
        public uint cCOMClassicVTableCreated = 0;
        public uint cCOMClassicVTableDestroyed = 0;
        public uint cExceptionCatcherEnter = 0;
        public uint cExceptionCatcherLeave = 0;
        public uint cExceptionCLRCatcherExecute = 0;
        public uint cExceptionCLRCatcherFound = 0;
        public uint cExceptionOSHandlerEnter = 0;
        public uint cExceptionOSHandlerLeave = 0;
        public uint cExceptionSearchCatcherFound = 0;
        public uint cExceptionSearchFunctionEnter = 0;
        public uint cExceptionSearchFunctionLeave = 0;
        public uint cExceptionSearchFilterEnter = 0;
        public uint cExceptionSearchFilterLeave = 0;
        public uint cExceptionThrown = 0;
        public uint cExceptionUnwindFinallyEnter = 0;
        public uint cExceptionUnwindFinallyLeave = 0;
        public uint cExceptionUnwindFunctionEnter = 0;
        public uint cExceptionUnwindFunctionLeave = 0;
        public uint cFunctionUnloadStarted = 0;
        public uint cJITCachedFunctionSearchFinished = 0;
        public uint cJITCachedFunctionSearchStarted = 0;
        public uint cJITCompilationFinished = 0;
        public uint cJITCompilationStarted = 0;
        public uint cJITFunctionPitched = 0;
        public uint cJITInlining = 0;
        public uint cManagedToUnmanagedTransition = 0;
        public uint cModuleAttachedToAssembly = 0;
        public uint cModuleLoadFinished = 0;
        public uint cModuleLoadStarted = 0;
        public uint cModuleUnloadFinished = 0;
        public uint cModuleUnloadStarted = 0;
        public uint cMovedReferences = 0;
        public uint cObjectAllocated = 0;
        public uint cObjectReferences = 0;
        public uint cObjectsAllocatedByClass = 0;
        public uint cThreadCreated = 0;
        public uint cThreadDestroyed = 0;
        public uint cThreadAssignedToOSThread = 0;
        public uint cRemotingClientInvocationStarted = 0;
        public uint cRemotingClientSendingMessage = 0;
        public uint cRemotingClientReceivingReply = 0;
        public uint cRemotingClientInvocationFinished = 0;
        public uint cRemotingServerReceivingMessage = 0;
        public uint cRemotingServerInvocationStarted = 0;
        public uint cRemotingServerInvocationReturned = 0;
        public uint cRemotingServerSendingReply = 0;
        public uint cUnmanagedToManagedTransition = 0;
        public uint cRuntimeSuspendStarted = 0;
        public uint cRuntimeSuspendFinished = 0;
        public uint cRuntimeSuspendAborted = 0;
        public uint cRuntimeResumeStarted = 0;
        public uint cRuntimeResumeFinished = 0;
        public uint cRuntimeThreadSuspended = 0;
        public uint cRuntimeThreadResumed = 0;
        public uint cRootReferences = 0;

        // ICorProfilerCallBack2 Implementation

        public uint cThreadNameChanged = 0;
        public uint cGarbageCollectionStarted = 0;
        public uint cSurvivingReferences = 0;
        public uint cGarbageCollectionFinished = 0;
        public uint cFinalizeableObjectQueued = 0;
        public uint cRootReferences2 = 0;
        public uint cHandleCreated = 0;
        public uint cHandleDestroyed = 0;

        // ICorProfilerCallBack3 Implementation

        public uint cInitializeForAttach = 0;
        public uint cProfilerAttachComplete = 0;
        public uint cProfilerDetachSucceeded = 0;

        // other GC counters of interest

        public uint cRuntimeSuspensionsForGC = 0;
        public uint cGen0Collections = 0;
        public uint cGen1Collections = 0;
        public uint cGen2Collections = 0;
        public uint cLOHCollections = 0;
        public uint cInducedGCs = 0;
    }
}
