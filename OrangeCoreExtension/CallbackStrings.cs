using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrangeClient
{
    public sealed class CallbackStrings
    {
        public static readonly string CALLBACK_Initialize                          ="i";
        public static readonly string CALLBACK_Shutdown                            ="s";
        public static readonly string CALLBACK_AppDomainCreationFinished           ="adcf";
        public static readonly string CALLBACK_AppDomainCreationStarted            ="ADCS";
        public static readonly string CALLBACK_AppDomainShutdownFinished           ="ADSF";              
        public static readonly string CALLBACK_AppDomainShutdownStarted            ="ADSS";
        public static readonly string CALLBACK_AssemblyLoadFinished                ="ALF";
        public static readonly string CALLBACK_AssemblyLoadStarted                 ="ALS";
        public static readonly string CALLBACK_AssemblyUnloadFinished              ="AUF";
        public static readonly string CALLBACK_AssemblyUnloadStarted               ="AUS";
        public static readonly string CALLBACK_ClassLoadFinished                   ="CLF";
        public static readonly string CALLBACK_ClassLoadStarted                    ="CLS";
        public static readonly string CALLBACK_ClassUnloadFinished                 ="CUF";
        public static readonly string CALLBACK_ClassUnloadStarted                  ="CUS";
        public static readonly string CALLBACK_COMClassicVTableCreated             ="CCVC";
        public static readonly string CALLBACK_COMClassicVTableDestroyed           ="CCVD";
        public static readonly string CALLBACK_ExceptionCatcherEnter               ="ECE";
        public static readonly string CALLBACK_ExceptionCatcherLeave               ="ECL";
        public static readonly string CALLBACK_ExceptionCLRCatcherExecute          ="ECCE";
        public static readonly string CALLBACK_ExceptionCLRCatcherFound            ="ECCF";
        public static readonly string CALLBACK_ExceptionOSHandlerEnter             ="EOHE";
        public static readonly string CALLBACK_ExceptionOSHandlerLeave             ="EOHL";
        public static readonly string CALLBACK_ExceptionSearchCatcherFound         ="ESCF";
        public static readonly string CALLBACK_ExceptionSearchFunctionEnter        ="ESFUE";
        public static readonly string CALLBACK_ExceptionSearchFunctionLeave        ="ESFUL";
        public static readonly string CALLBACK_ExceptionSearchFilterEnter          ="ESFIE";
        public static readonly string CALLBACK_ExceptionSearchFilterLeave          ="ESFIL"; 
        public static readonly string CALLBACK_ExceptionThrown                     ="ET";
        public static readonly string CALLBACK_ExceptionUnwindFinallyEnter         ="EUFIE";
        public static readonly string CALLBACK_ExceptionUnwindFinallyLeave         ="EUFIL";        
        public static readonly string CALLBACK_ExceptionUnwindFunctionEnter        ="EUFUE";
        public static readonly string CALLBACK_ExceptionUnwindFunctionLeave        ="EUFUL";
        public static readonly string CALLBACK_FunctionUnloadStarted               ="FUS";
        public static readonly string CALLBACK_JITCachedFunctionSearchFinished     ="JCFSF";
        public static readonly string CALLBACK_JITCachedFunctionSearchStarted      ="JCFSS";
        public static readonly string CALLBACK_JITCompilationFinished              ="JCF";
        public static readonly string CALLBACK_JITCompilationStarted               ="JCS";
        public static readonly string CALLBACK_JITFunctionPitched                  ="JFP";
        public static readonly string CALLBACK_JITInlining                         ="JI";
        public static readonly string CALLBACK_ManagedToUnmanagedTransition        ="MTUT";
        public static readonly string CALLBACK_ModuleAttachedToAssembly            ="MATA";
        public static readonly string CALLBACK_ModuleLoadFinished                  ="MLF";
        public static readonly string CALLBACK_ModuleLoadStarted                   ="MLS";
        public static readonly string CALLBACK_ModuleUnloadFinished                ="MUF";
        public static readonly string CALLBACK_ModuleUnloadStarted                 ="MUS";
        public static readonly string CALLBACK_MovedReferences                     ="mr";
        public static readonly string CALLBACK_ObjectAllocated                     ="OA";
        public static readonly string CALLBACK_ObjectReferences                    ="or";
        public static readonly string CALLBACK_ObjectsAllocatedByClass             ="OABC";
        public static readonly string CALLBACK_ThreadCreated                       ="TC";
        public static readonly string CALLBACK_ThreadDestroyed                     ="TD";
        public static readonly string CALLBACK_ThreadAssignedToOSThread            ="TATOT";
        public static readonly string CALLBACK_RemotingClientInvocationStarted     ="RCIS";
        public static readonly string CALLBACK_RemotingClientSendingMessage        ="RCSM";
        public static readonly string CALLBACK_RemotingClientReceivingReply        ="RCRR";
        public static readonly string CALLBACK_RemotingClientInvocationFinished    ="RCIF";
        public static readonly string CALLBACK_RemotingServerReceivingMessage      ="RSRM";
        public static readonly string CALLBACK_RemotingServerInvocationStarted     ="RSIS";
        public static readonly string CALLBACK_RemotingServerInvocationReturned    ="RSIR";
        public static readonly string CALLBACK_RemotingServerSendingReply          ="RSSR";
        public static readonly string CALLBACK_UnmanagedToManagedTransition        ="UTMT";
        public static readonly string CALLBACK_RuntimeSuspendStarted               ="RSS";
        public static readonly string CALLBACK_RuntimeSuspendFinished              ="RSF";     
        public static readonly string CALLBACK_RuntimeSuspendAborted               ="RSA";
        public static readonly string CALLBACK_RuntimeResumeStarted                ="RRS";
        public static readonly string CALLBACK_RuntimeResumeFinished               ="RRF";
        public static readonly string CALLBACK_RuntimeThreadSuspended              ="RTS";
        public static readonly string CALLBACK_RuntimeThreadResumed                ="RTR";
        public static readonly string CALLBACK_RootReferences                      ="RR";

        // ICorProfilerCallBack2 Implementation

        public static readonly string CALLBACK_ThreadNameChanged                   ="tnc";
        public static readonly string CALLBACK_GarbageCollectionStarted            ="gcs";
        public static readonly string CALLBACK_SurvivingReferences                 ="sr";
        public static readonly string CALLBACK_GarbageCollectionFinished           ="gcf";
        public static readonly string CALLBACK_FinalizeableObjectQueued            ="foq";
        public static readonly string CALLBACK_RootReferences2                     ="rr2";
        public static readonly string CALLBACK_HandleCreated                       ="hc";
        public static readonly string CALLBACK_HandleDestroyed                     ="hd";

        // ICorProfilerCallBack3 Implementation

        public static readonly string CALLBACK_InitializeForAttach                 ="ifa";
        public static readonly string CALLBACK_ProfilerAttachComplete              ="pac";
        public static readonly string CALLBACK_ProfilerDetachSucceeded             ="pds";




    }
}
