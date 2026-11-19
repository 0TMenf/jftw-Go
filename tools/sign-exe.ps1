# sign-exe.ps1 — 用本机自签名证书给 exe 签名
# 用法: powershell -ExecutionPolicy Bypass -File tools/sign-exe.ps1 <exe路径>
# 首次运行会自动创建证书并加入本机"受信任根/受信任发布者"。
param([Parameter(Mandatory=$true)][string]$ExePath)

$ErrorActionPreference = "Stop"
$subject = "CN=jftw Go Developer"
$thumb = "189A3DF31AF1428F6E01E76E9D054C8801F2548F"

$cert = Get-ChildItem "Cert:\CurrentUser\My\$thumb" -ErrorAction SilentlyContinue
if (-not $cert) {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject `
        -KeyUsage DigitalSignature -FriendlyName "jftw Go Code Signing" `
        -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(5)

    $root = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
    $root.Open("ReadWrite"); $root.Add($cert); $root.Close()
    $pub = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPublisher", "CurrentUser")
    $pub.Open("ReadWrite"); $pub.Add($cert); $pub.Close()
    Write-Host "created cert: $($cert.Thumbprint)"
}

$r = Set-AuthenticodeSignature -FilePath $ExePath -Certificate $cert -HashAlgorithm SHA256
Write-Host "$ExePath : $($r.Status) - $($r.StatusMessage)"
if ($r.Status -ne "Valid") { exit 1 }
