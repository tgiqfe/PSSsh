$credDir = ".\Credential"

$userName = Get-Content "${credDir}\UserName.crd"
$passWord = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR( `
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR( `
    (Get-Content "${credDir}\Password.crd" | ConvertTo-SecureString)))

