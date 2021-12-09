# TimerCrash

Program Output:
```
Timer Evaluation in C#
 OS: Linux 5.4.0-91-generic #102-Ubuntu SMP Fri Nov 5 16:31:28 UTC 2021
 Framework: .NET 6.0.0-rtm.21522.10
Timer starts with period (ms): 1000
Press ESC-key to exit
Unhandled exception. System.DllNotFoundException: Unable to load shared library 'kernel32.dll' or one of its dependencies. In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable: libkernel32.dll: cannot open shared object file: No such file or directory
   at Win32ThreadPoolTimerApiSet.ThreadPoolTimer.CreateThreadpoolTimer(TimerCallback pfnti, IntPtr pCBContext, IntPtr pcbe)
   at Win32ThreadPoolTimerApiSet.ThreadPoolTimer.StartRelativeTimer(Action`1 callback, Object cbUserdata, UInt32 initialwaitTimeMs, UInt32 msperiod) in C:\ffl\src\playground\cs\MyTimer\Win32ThreadPoolApiSet.cs:line 117
   at MyTimer.Program.Main(String[] args) in C:\ffl\src\playground\cs\MyTimer\Program.cs:line 42
Abgebrochen (Speicherabzug geschrieben)
```

dotnet crashed after exception occurence 

Output of apport (ubuntu crash reporter) excerpt:
```
Executable Path: /usr/share/dotnet/dotnet
Package: dotnet-host 6.0.0.-1 [origin microsoft-ubuntu-focal-prod focal
Title: dotnet crashed with SIGABRT
Architecture: amd64
Xubuntu 20.04 LTS "Focal Fossa" - Release amd64 (20200423)
Intel i5-4200M
Stacktrace Top ??() from /usr/share/dotnet/shared/Microsoft.NETCore.App/6.0.0/libcoreclr.so
```