@echo off
:: Builds SideNotes.exe using the C# compiler that ships with Windows.
:: No SDK, no downloads - just run this.

set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319

%FW%\csc.exe /nologo /target:winexe /out:SideNotes.exe ^
  /r:System.dll /r:System.Core.dll /r:%FW%\System.Xaml.dll ^
  /r:%FW%\WPF\WindowsBase.dll /r:%FW%\WPF\PresentationCore.dll ^
  /r:%FW%\WPF\PresentationFramework.dll ^
  SideNotes.cs

if %errorlevel%==0 (echo Built SideNotes.exe) else (echo BUILD FAILED)
