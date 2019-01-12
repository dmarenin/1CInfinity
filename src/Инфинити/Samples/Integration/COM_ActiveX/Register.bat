 if /i --%processor_architecture% == --AMD64 GOTO AMD64
 if /i --%PROCESSOR_ARCHITEW6432% == --AMD64 GOTO AMD64
 if /i --%processor_architecture% == --x86   GOTO x86
 GOTO ERR

 :AMD64

    rem x86
    "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" "%~dp0Cx.Integration.AgatInfinityConnectorInterfaces.dll" /codebase /tlb
    "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" "%~dp0Cx.Client.ThirdPartyIntegration.dll" /codebase /tlb

    rem x64
    "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" "%~dp0Cx.Integration.AgatInfinityConnectorInterfaces.dll" /codebase /tlb
    "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" "%~dp0Cx.Client.ThirdPartyIntegration.dll" /codebase /tlb

 GOTO EXEC

 :x86
    rem x86
    "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" "%~dp0Cx.Integration.AgatInfinityConnectorInterfaces.dll" /codebase /tlb
    "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" "%~dp0Cx.Client.ThirdPartyIntegration.dll" /codebase /tlb
 GOTO EXEC

 :EXEC
 GOTO END

 :ERR
 @echo Unsupported architecture!

 :END 
