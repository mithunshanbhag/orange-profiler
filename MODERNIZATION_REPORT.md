# Orange Profiler modernization report

## Executive summary

This repository is not a simple "retarget the project files" upgrade.

It is a mixed managed/native profiler stack built around:

- legacy Visual Studio solution/project formats (`Orange.sln`, `*.csproj`, `OrangeProfiler.vcxproj`)
- .NET Framework 4.0 managed code (`Orange/Orange.csproj`, `OrangeCoreExtension/OrangeCoreExtension.csproj`, `NativeWrappers/NativeWrappers.csproj`, `OrangeUtil/OrangeUtil.csproj`)
- a native Windows COM profiler DLL implementing `ICorProfilerCallback3` / `ICorProfilerInfo3` (`OrangeProfiler/OrangeProfiler.h`, `OrangeProfiler/OrangeProfiler.cpp`)
- Windows-only activation and control paths based on COM registration, Win32 handles/events, and `ICLRMetaHost` / `ICLRProfiling` (`OrangeProfiler/COMGoo.cpp`, `OrangeCoreExtension/OrangeCoreExtension.cs`, `OrangeCoreExtension/ProcessHelper.cs`)
- x86 assumptions, including x86-only enter/leave hook assembly stubs (`Orange/Orange.csproj`, `OrangeCoreExtension/OrangeCoreExtension.csproj`, `OrangeProfiler/OrangeProfiler.cpp`)

### Bottom line

If the goal is to make this work on **current .NET on Windows**, the smallest realistic modernization is:

1. keep the profiler **Windows-only**
2. port the managed tools to **SDK-style `net8.0-windows`**
3. port the native profiler to a **modern Visual Studio C++ toolchain**
4. add **64-bit support**
5. replace the **.NET Framework-only attach/activation path** with the modern CoreCLR startup and attach model
6. audit the profiler callbacks/APIs against current .NET runtime behavior
7. add a basic automated validation harness

If the goal is to make it **cross-platform**, this becomes a significantly larger rewrite because several core assumptions in the current code are Windows-specific.

---

## What the project is today

### Solution layout

The repository contains a Visual Studio 2013-era solution with these major parts:

- `Orange/`: console shell/launcher
- `OrangeCoreExtension/`: managed extension layer with process launch/attach logic
- `NativeWrappers/`: COM and Win32 interop definitions
- `OrangeProfiler/`: native profiler DLL
- `OrangeUtil/`: helper library
- `ShapePainter/`: WinForms sample app
- `AuxiliaryPanel/`: WinForms helper UI

### Current technical baseline

- Managed projects target **.NET Framework 4.0** and mostly force **x86**
- The native profiler project uses **VC++ v120** / Visual Studio 2013 settings
- The profiler is exposed as a **COM DLL** with `DllGetClassObject`, `DllRegisterServer`, and `DllUnregisterServer`
- Startup profiling uses `COR_ENABLE_PROFILING`, `COR_PROFILER`, and `COR_PROFILER_PATH`
- Attach uses `ICLRMetaHost` + `ICLRProfiling.AttachProfiler`
- The code uses **Win32 events, timers, process handles, registry activation, and Windows Forms**

Examples:

- .NET Framework 4.0 + x86: `Orange/Orange.csproj`, `NativeWrappers/NativeWrappers.csproj`, `OrangeCoreExtension/OrangeCoreExtension.csproj`, `OrangeUtil/OrangeUtil.csproj`
- WinForms dependencies: `OrangeCoreExtension/OrangeCoreExtension.csproj`, `AuxiliaryPanel/AuxiliaryPanel.csproj`, `ShapePainter/ShapePainter.csproj`
- COM registration exports: `OrangeProfiler/COMGoo.cpp`, `OrangeProfiler/OrangeProfiler.def`
- Startup env-vars: `OrangeCoreExtension/OrangeCoreExtension.cs`
- Attach via Metahost COM APIs: `OrangeCoreExtension/ProcessHelper.cs`
- Profiler implementation: `OrangeProfiler/OrangeProfiler.h`, `OrangeProfiler/OrangeProfiler.cpp`

---

## What I verified in this environment

I attempted to build the existing solution with:

```bash
dotnet build Orange.sln
```

That failed immediately for expected legacy reasons:

- `.NETFramework,Version=v4.0` reference assemblies are missing
- the native profiler project imports `Microsoft.Cpp.*` targets that are only available in a Windows Visual Studio C++ toolchain

This confirms the repo does **not** build in a modern default .NET SDK environment as-is, and it also confirms that any modernization plan must address both the managed and native toolchains.

---

## What must change to modernize it

## 1. Decide the target: modern Windows-only vs true cross-platform

This is the first and most important product decision.

### Option A: modernize for current .NET on Windows

This is the recommended first step.

Why:

- the profiler is already deeply Windows-specific
- WinForms and COM assumptions can be preserved where useful
- the .NET profiler API is still available on Windows
- effort stays bounded

### Option B: modernize for cross-platform .NET

This is possible in principle for the profiler concept, but not for this implementation without major redesign.

Why it is much harder:

- COM registration is Windows-only
- `ICLRMetaHost` / `ICLRProfiling` hosting and attach path is Windows/.NET Framework-centric
- Win32 named events and process control are Windows-only
- the binary packaging model would need to handle `.dll`/`.so`/`.dylib`
- the profiler control UI would need a new transport/control mechanism

Recommendation: **do Option A first**. Only pursue Option B if cross-platform support is a real requirement.

---

## 2. Upgrade the build system and project formats

### Managed projects

The managed projects should be converted from legacy `.csproj` format to SDK-style projects.

Recommended targets:

- `Orange`, `OrangeCoreExtension`, `AuxiliaryPanel`: `net8.0-windows`
- `ShapePainter`: `net8.0-windows`
- `OrangeUtil`: `net8.0` or `net8.0-windows` depending on whether you keep WinForms references out of it
- `NativeWrappers`: likely `net8.0-windows`

Key work:

- convert project files to SDK-style format
- replace legacy assembly references with package/framework references where needed
- update solution/build docs
- normalize output paths (several projects currently write to ad hoc locations like external `orange\` folders or `c:\orange`)

### Native profiler

The native project needs to move to a current MSVC toolset and 64-bit-capable build configuration.

Key work:

- upgrade `OrangeProfiler/OrangeProfiler.vcxproj` to a current Visual Studio toolset
- add x64 builds
- remove assumptions that only Win32/x86 exists
- review warnings/errors under a modern compiler

---

## 3. Remove or isolate x86-only assumptions

This is one of the biggest blockers.

The current code is not just "prefer x86"; parts of it are explicitly **implemented for x86**:

- managed projects set `PlatformTarget` to `x86`
- native project is configured for `Win32`
- `OrangeProfiler/OrangeProfiler.cpp` contains x86-specific naked assembly enter/leave/tailcall stubs guarded by `_X86_`

### What this means

A modern profiler must at least handle **x64**, because most modern .NET workloads run 64-bit.

### Required work

- redesign or replace the x86-only ELT hook implementation
- verify whether the existing call-stack tracking strategy still makes sense on current runtimes
- add x64-safe hook paths, or drop the ELT-dependent features in the first modernization milestone

Practical recommendation:

- first ship a modernized build **without** the most architecture-sensitive ELT features if that is what it takes to get a stable baseline
- then restore advanced call-stack tracking incrementally

---

## 4. Replace the legacy profiler activation model

The code currently supports two activation/control styles:

1. startup profiling through classic .NET Framework environment variables
2. runtime attach through `ICLRMetaHost` / `ICLRProfiling.AttachProfiler`

Files:

- startup env-vars: `OrangeCoreExtension/OrangeCoreExtension.cs`
- attach path: `OrangeCoreExtension/ProcessHelper.cs`
- COM registration exports: `OrangeProfiler/COMGoo.cpp`

### What needs to change

#### Startup activation

For modern .NET / CoreCLR startup profiling, the environment variable model changes to the CoreCLR form, typically using:

- `CORECLR_ENABLE_PROFILING`
- `CORECLR_PROFILER`
- `CORECLR_PROFILER_PATH` (or architecture-specific variants)

The launcher should be able to set the appropriate variables for:

- modern .NET / CoreCLR
- optionally legacy .NET Framework, if dual support is desired

#### Attach

The current attach code is built around the Windows Metahost COM APIs and explicitly filters for `v4.*` runtimes in `OrangeCoreExtension/ProcessHelper.cs`.

For modern .NET, attach should be redesigned around the **current diagnostics/profiler attach mechanism** used by CoreCLR rather than the .NET Framework `ICLRMetaHost` path.

That likely means:

- a new attach controller layer
- runtime detection logic that distinguishes .NET Framework from .NET (Core/5+/6+/8+)
- potentially separate implementations for legacy CLR and modern CoreCLR

#### COM registration

The current `regsvr32`-based registration path should not be the primary activation model for a modern profiler.

Recommendation:

- prefer explicit profiler path activation over COM registration where possible
- keep COM registration only if you intentionally support older .NET Framework scenarios

---

## 5. Audit the profiler API usage against modern .NET behavior

The native profiler is centered on `ICorProfilerCallback3` / `ICorProfilerInfo3`.

That does **not automatically mean** it will work correctly on current .NET runtimes.

The code needs a targeted audit of:

- callback contracts
- event masks
- heap/object inspection APIs
- metadata queries
- attach/detach behavior
- enter/leave hook behavior

Areas that deserve special attention:

- `GarbageCollectionFinished` processing in `OrangeProfiler/OrangeProfiler.cpp`
- metadata/object/class/module traversal in `OrangeProfiler/EntityInfo.cpp`
- detach logic in `OrangeProfiler/OrangeProfiler.cpp`
- runtime version assumptions in `OrangeProfiler/OrangeProfiler.cpp` and `OrangeCoreExtension/ProcessHelper.cs`

### Why this matters

The project was built around .NET 4-era assumptions such as:

- CLR v4 being the only target of interest
- AppDomain-oriented thinking
- .NET Framework-specific attach behavior
- older GC/profiler interaction expectations

Modern .NET has different runtime behavior, different hosting/diagnostics infrastructure, and fewer reasons to preserve older compatibility code.

---

## 6. Rework Windows-only control and transport assumptions

Even if the first milestone remains Windows-only, some implementation choices should still be cleaned up.

Current control paths include:

- global named Win32 events (`Global\\...`)
- process-wide environment variables
- direct `CreateProcess` / `OpenProcess` / `WaitForSingleObject`
- optional WinForms auxiliary panels

Files:

- `OrangeProfiler/OrangeProfiler.h`
- `OrangeCoreExtension/OrangeCoreExtension.cs`
- `OrangeCoreExtension/AuxiliaryPanel.cs`

### Recommended modernization

- separate profiler engine concerns from UI concerns
- keep the profiler control protocol small and explicit
- prefer a documented control channel over ad hoc named-event coordination
- treat the UI as optional tooling, not part of the profiler core

The easiest shape is:

- native profiler library
- modern CLI controller
- optional Windows desktop UI on top

---

## 7. Modernize the managed codebase

The managed code does not need a full rewrite, but it does need cleanup during porting.

Main tasks:

- move to modern SDK-style projects
- enable nullable reference types if desired
- replace legacy exception/argument patterns gradually
- isolate Windows-specific interop behind explicit boundaries
- cleanly separate launcher logic, attach logic, and output/reporting logic

This is also a good point to decide whether the `Orange` shell is still valuable as-is or whether a simpler command-line interface would serve the project better today.

---

## 8. Add automated validation

There is no real automated test infrastructure in the repository today.

That is a major risk for a profiler, because profiler regressions are usually integration failures rather than unit-test failures.

### Minimum validation required

1. build the managed projects in CI
2. build the native profiler in CI on Windows
3. add smoke tests that:
   - launch a small sample app under profiling
   - verify the profiler loads
   - verify a trace/output file is produced
   - verify attach/detach works if that scenario is kept
4. run those smoke tests on x64

### Good sample targets

- a small `net8.0` console app
- optionally a GC-heavy workload
- optionally the existing `ShapePainter` sample if it is retained

---

## 9. Expect some feature triage

Trying to preserve every feature in the first pass is likely to slow the project down badly.

A safer plan is:

### Keep for milestone 1

- startup profiling
- basic tracing
- GC/object inspection that still maps cleanly to modern APIs
- x64 support
- stable Windows build

### Defer for milestone 2

- complex attach flows
- auxiliary UI panel
- advanced ELT/call-stack capture
- legacy COM registration workflows
- legacy .NET Framework compatibility, unless it is still required

---

## Suggested phased plan

## Phase 1: establish a supported target

- choose **Windows-only modern .NET** as the first goal
- choose supported runtimes (for example: .NET 8 and .NET Framework 4.8, or only .NET 8)
- decide whether legacy .NET Framework compatibility is still required

## Phase 2: make it build again

- convert managed projects to SDK-style
- upgrade the native VC++ project
- produce repeatable Windows builds for managed + native components

## Phase 3: make startup profiling work on modern .NET

- implement CoreCLR startup activation
- load the native profiler successfully in a simple sample app
- verify output generation

## Phase 4: restore advanced profiler behaviors

- re-enable or redesign ELT/call-stack features
- audit heap/object inspection
- verify detach behavior

## Phase 5: harden and document

- add Windows CI
- add smoke/integration tests
- write setup and usage documentation

---

## Effort/risk assessment

### Small effort?

No.

### Medium effort?

Only if the scope is narrowed to:

- Windows-only
- startup profiling first
- x64 support
- no promise that every legacy feature survives unchanged

### Large effort?

Yes, if you want any of the following:

- cross-platform support
- full attach parity on modern .NET
- preservation of every legacy feature
- continued support for both old .NET Framework and current .NET from the same codebase

---

## Recommended modernization target

If I were modernizing this project pragmatically, I would target:

### First deliverable

- **Windows-only**
- **native profiler still in C++**
- **managed controller in `net8.0-windows`**
- **x64 first**
- **startup profiling first**
- **attach support only after startup profiling is stable**

### Optional second deliverable

- dual support for:
  - legacy .NET Framework 4.8 on Windows
  - modern .NET 8+ on Windows

### Explicit non-goal for the first pass

- cross-platform profiler support

---

## In one sentence

To modernize this code, you need to treat it as a **mixed C++ profiler + managed launcher migration project**, not as a routine `.NET Framework -> .NET` retargeting exercise.
