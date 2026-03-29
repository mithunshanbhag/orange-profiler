#pragma once
////////////////////////////////////////////////////////////////////////////////////////
// Common.hxx  - Includes necessary headers and defines global configuration options. //
// Application - Orange Profiler.                                                     //
// Author      - Mithun Shanbhag, mithuns@microsoft.com                               //
////////////////////////////////////////////////////////////////////////////////////////

#include <windows.h>
#include <cor.h>
#include <corprof.h>
#include <process.h>
#include <Psapi.h>
#include <iostream>
#include <iomanip>
#include <objbase.h>
#include <map>
#include <vector>
#include <set>
#include <stack>
#include <string>

using namespace std;

///////////////////////////////////////////////////////////////////////////////

// These flags can be "OR"ed together. Higher-order flags will over-ride 
// lower-order flags if ORed together.   

// Diagnostic info levels -                                
enum DiagnosticInfoLevel {
    TRACELEVEL_NONE        = 0x00000000,
    TRACELEVEL_ERROR       = 0x00000001,
    TRACELEVEL_DIAGNOSTIC  = 0x00000002,  
};  


///////////////////////////////////////////////////////////////////////////////

//////////////////////////////////
// Global configuration options //
//////////////////////////////////

extern DWORD g_DiagnosticInfoLevel; // Global verbosity level 
extern FILE * g_pLog;

/////////////////////
// Utility Macros. //
/////////////////////


#define HR_CHECK(_hr_, _exp_, _func_)   if (_exp_ != _hr_) {            \
                                            TRACEERROR(_hr_, _func_)    \
                                            if (S_OK == hr) {           \
                                                hr = E_FAIL;            \
                                            }                           \
                                            goto ErrReturn;             \
                                        }                               

#define TRACE(...)                      if (NULL != g_pLog) {                                                                 \
                                              fwprintf_s(g_pLog, __VA_ARGS__); fwprintf_s(g_pLog, L"\n");                     \
                                        }                                                                                     


#define DIAGNOSTIC(_msg_, ...)     		if (g_DiagnosticInfoLevel & TRACELEVEL_DIAGNOSTIC) {                                  \
                                            wchar_t buffer[2 * MAX_PATH] = {0};                                               \
                                            wchar_t buffer2[2 * MAX_PATH] = {0};                                              \
                                            swprintf(buffer, L"(OP) DIAGNOSTIC: %s\n", _msg_);                                \
                                            swprintf(buffer2, buffer, __VA_ARGS__);                                           \
                                            OutputDebugString(buffer2);                                                       \
                                        }                                                                                     
                                        

#define TRACEERROREX(_msg_, ...)        if (g_DiagnosticInfoLevel & TRACELEVEL_ERROR) {                                       \
                                            wchar_t buffer[2 * MAX_PATH] = {0};                                               \
                                            wchar_t buffer2[2 * MAX_PATH] = {0};                                              \
                                            swprintf(buffer, L"(OP) ERROR: %s\n", _msg_);                                     \
                                            swprintf(buffer2, buffer, __VA_ARGS__);                                           \
                                            OutputDebugString(buffer2);                                                       \
											if (IsDebuggerPresent()) {														  \
												DebugBreak();																  \
											}																				  \
                                        }                                                                                     

#define TRACEERROR(_hr_, _msg_)         if (g_DiagnosticInfoLevel & TRACELEVEL_ERROR) {                                       \
                                            wchar_t buffer[2 * MAX_PATH] = {0};                                               \
                                            swprintf(buffer, L"(OP) ERROR: %s. Error Code 0x%X\n", _msg_, _hr_);              \
                                            OutputDebugString(buffer);                                                        \
											if (IsDebuggerPresent()) {														  \
												DebugBreak();																  \
											}																				  \
										}                                                                                     \
                                        
                                        
