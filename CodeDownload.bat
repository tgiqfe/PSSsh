@echo off
pushd %~dp0

rem # プロジェクト名
set ProjectName=PSSsh

rem # Code for Manifest
echo Manifest Code Update

powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/Manifest/master/Manifest/Program.cs\" -OutFile \".\Manifest\Program.cs\""
powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/Manifest/master/Manifest/PSD1.cs\" -OutFile \".\Manifest\PSD1.cs\""
powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/Manifest/master/Manifest/PSM1.cs\" -OutFile \".\Manifest\PSM1.cs\""

powershell -Command "(Get-Content \".\Manifest\Program.cs\") -replace \"`n\",\"`r`n\" | Out-File \".\Manifest\Program.cs\" -Encoding UTF8"
powershell -Command "(Get-Content \".\Manifest\PSD1.cs\") -replace \"`n\",\"`r`n\" | Out-File \".\Manifest\PSD1.cs\" -Encoding UTF8"
powershell -Command "(Get-Content \".\Manifest\PSM1.cs\") -replace \"`n\",\"`r`n\" | Out-File \".\Manifest\PSM1.cs\" -Encoding UTF8"

