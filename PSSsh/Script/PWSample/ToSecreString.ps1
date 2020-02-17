$credDir = ".\Credential"
Push-Location (Get-Item $Myinvocation.MyCommand.Path).DirectoryName
if(!(Test-Path $credDir -PathType Container)){
    try{
        New-Item $credDir -ItemType Directory -ErrorAction Stop > $null
    }catch{}
}
$cred = Get-Credential 
$cred.UserName | Set-Content "${credDir}\UserName.crd"
$cred.Password | ConvertFrom-SecureString | Set-Content "${credDir}\Password.crd"
