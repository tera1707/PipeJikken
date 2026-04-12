@set @dummy=0/*
@echo off
NET FILE 1>NUL 2>NUL
if "%ERRORLEVEL%" neq "0" (
  cscript //nologo //E:JScript "%~f0" %*
  exit /b %ERRORLEVEL%
)

REM 管理者権限で実行したい処理 ここから

cd %~dp0

set SERVICENAME="ServiceJikkenSvcName"
set SERVICEEXENAME=ServiceJikken.exe
set SERVICEDISPNAME=%SERVICENAME%
set BINPATH=%~dp0x64\Debug\%SERVICEEXENAME%
set DISCRIPTION="Service Jikken Svc Description..."

REM サービスを起動する
net start %SERVICENAME%

pause

REM 管理者権限で実行したい処理 ここまで

goto :EOF
*/
var cmd = '"/c ""' + WScript.ScriptFullName + '" ';
for (var i = 0; i < WScript.Arguments.Length; i++) cmd += '"' + WScript.Arguments(i) + '" ';
(new ActiveXObject('Shell.Application')).ShellExecute('cmd.exe', cmd + ' "', '', 'runas', 1);
