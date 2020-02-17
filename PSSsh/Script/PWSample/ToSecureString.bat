@echo off
pushd %~dp0

set scriptfile=".\ToSecreString.ps1"
PowerShell -NoProfile -ExecutionPolicy Unrestricted -Command %scriptfile%
