# ================================================================
# Parameter
# ================================================================

#プロジェクト名
$projectName = "PSSsh"

# 開発者情報
$author = "q"
$companyName = "q"
$copyright = $null
$description = "SSH for PowerShell"

# プロジェクトフォルダー情報
$cmdletDirName = "Cmdlet"
$formatDirName = "Format"
$scriptDirName = "Script"
$helpDirName = "Help"

# 同フォルダーに配置する必要があるファイル
$additionalFiles = @{
}

# ================================================================
# Prepare
# ================================================================

Set-Location -Path (Split-Path $MyInvocation.MyCommand.Path -Parent)
[Environment]::CurrentDirectory = (Get-Location).Path
$dllFilePath = ".\" + $projectName + ".dll"
$dllFullPath = Convert-Path -Path $dllFilePath

function searchUpward([string]$nowDirectory, [string]$searchName){
    $matchName = Get-ChildItem -Path $nowDirectory -Directory | Where { $_.Name -eq $searchName }
    if($matchName -ne $null){
        return $matchName
    }
    $parent = Split-Path $nowDirectory -Parent
    if([string]::IsNullOrEmpty($parent)){
        return $null
    }
    searchUpward $parent $searchName
}

$cmdletPath = searchUpward ((Get-Location).Path) $cmdletDirName
$formatPath = searchUpward ((Get-Location).Path) $formatDirName
$scriptPath = searchUpward ((Get-Location).Path) $scriptDirName
$helpPath = searchUpward ((Get-Location).Path) $helpDirName

# ================================================================
# Main1 - Create PSD1
# ================================================================

$psd1FilePath = ".\" + $projectName + ".psd1"
if(Test-Path -Path $psd1FilePath -PathType Leaf){
    Remove-Item -Path $psd1FilePath
}

# Cmdletを取得
$cmdletToExportList = @()
if($cmdletPath -ne $null){
    $csFiles = Get-ChildItem -Path $cmdletPath -Recurse | Where { $_.Name -like "*.cs" }
    foreach($file in $csFiles){
        foreach($line in (Get-Content -Path $file)){
            if($line -match "^\s*\[Cmdlet\(Verbs"){
                $cmdVerb = $line.Substring(
                    $line.IndexOf(".") + 1, $line.IndexOf(",") - $line.IndexOf(".") -1)
                $cmdNoun = $line.Substring(
                    $line.IndexOf("`"") + 1, $line.LastIndexOf("`"") - $line.IndexOf("`"") - 1)
                $cmdletToExportList += "${cmdVerb}-${cmdNoun}"
            }
        }
    }
}

# Format.ps1xmlを取得
$formatsToProcessList = @()
if($formatPath -ne $null){
    $formatFiles = Get-ChildItem -Path $formatPath -Recurse | Where { $_.Name -like "*.ps1xml" }
    foreach($file in $formatFiles){
        $formatsToProcessList += $file.Name
    }
}

"@{" `
    | Out-File -FilePath $psd1FilePath -Append
"RootModule = `"" + $projectName + ".dll`"" `
    | Out-File -FilePath $psd1FilePath -Append
"ModuleVersion = `"" + ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllFilePath).FileVersion) + "`"" `
    | Out-File -FilePath $psd1FilePath -Append
"GUID = `"" + ([System.Attribute]::GetCustomAttribute([System.Reflection.Assembly]::LoadFile($dllFullPath), [System.Runtime.InteropServices.GuidAttribute])).Value + "`"" `
    | Out-File -FilePath $psd1FilePath -Append
"Author = `"" + $author + "`"" `
    | Out-File -FilePath $psd1FilePath -Append
"CompanyName = `"" + $companyName + "`"" `
    | Out-File -FilePath $psd1FilePath -Append
"Copyright = `"" + $copyright + "`"" `
    | Out-File -FilePath $psd1FilePath -Append
"Description = `"" + $description + "`"" `
    | Out-File -FilePath $psd1FilePath -Append
"CmdletsToExport = @(" + [string]::Join(",`r`n  ", ($cmdletToExportList | ForEach { "`"" + $_ + "`"" }))  + "`r`n)" `
    | Out-File -FilePath $psd1FilePath -Append
"FormatsToProcess = @(" + [string]::Join(",`r`n  ", ($formatsToProcessList | ForEach { "`"" + $_ + "`"" }))  + "`r`n)" `
    | Out-File -FilePath $psd1FilePath -Append
"}" `
    | Out-File -FilePath $psd1FilePath -Append

# ================================================================
# Main2 - Create PSM1
# ================================================================

$psm1FilePath = ".\" + $projectName + ".psm1"
if(Test-Path -Path $psm1FilePath -PathType Leaf){
    Remove-Item -Path $psm1FilePath
}

"" | Out-File -FilePath $psm1FilePath -Append


# ================================================================
# Main3 - Copy Script Directory
# ================================================================

if($scriptPath -ne $null){
    Get-ChildItem -Path $scriptPath | Copy-Item -Destination . -Recurse -Force
}

# ================================================================
# Main4 - Copy Help Directory
# ================================================================

if($helpPath -ne $null){
    Get-ChildItem -Path $helpPath | `
        Where { $_.Name -like "*.dll-Help.xml" } | `
        Copy-Item -Destination . -Recurse -Force
}

# ================================================================
# Main5 - Copy Addtional files
# ================================================================

function copyAdditionalFile([string]$sourceFile, [string]$destinationFile){
    if(Test-Path -Path $sourceFile -PathType Leaf){
        $parent = Split-Path $destinationFile -Parent
        if(!(Test-Path -Path $parent -PathType Container)){
            New-Item -Path $parent -ItemType Directory
        }
        Copy-Item -Path $sourceFile -Destination $destinationFile
    }
}
foreach($key in $additionalFiles.Keys){
    copyAdditionalFile $key $additionalFiles[$key]
}

# ================================================================
# Main6 - Packaging
# ================================================================

$srcDir = Split-Path $MyInvocation.MyCommand.Path -Parent

$outputParent = searchUpward ((Get-Location).Path) "Release"
if($outputParent -eq $null){
    $outputParent = searchUpward ((Get-Location).Path) "Debug"
}
$dstDir = Join-Path (Split-Path $outputParent -Parent) $projectName

$myScriptPath = $MyInvocation.MyCommand.Path
& robocopy "${srcDir}" "${dstDir}" /COPY:DAT /MIR /XJD /XJF /XF "*.log" "*.json" "${$myScriptPath}"






