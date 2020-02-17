@echo off
pushd %~dp0

rem # Ver. 0.01.001

rem # DebugのManifest.exeのタイムスタンプを取得
set execDebug=Manifest\bin\Debug\Manifest.exe
for %%a in (%execDebug%) do set timestampD=%%~ta
echo Debug Manifest.exe : %timestampD%

rem # ReleaseのManifest.exeのタイムスタンプを取得
set execRelease=Manifest\bin\Release\Manifest.exe
for %%a in (%execRelease%) do set timestampR=%%~ta
echo Release Manifest.exe : %timestampR%

rem # Debug/Releaseで新しいほうのexeを実行
if "%timestampR%" GEQ "%timestampD%" (
	%execRelease%
) else (
	%execDebug%
)

pause
