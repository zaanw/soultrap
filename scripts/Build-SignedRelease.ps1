#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CertificateThumbprint,
    [string]$PublishDirectory = (Join-Path $PSScriptRoot '../artifacts/publish'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/signed'),
    [string]$TimestampUrl = 'https://timestamp.digicert.com'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (!$IsWindows) { throw 'Authenticode release signing requires Windows.' }
$thumbprint = $CertificateThumbprint.Replace(' ', '').ToUpperInvariant()
if ($thumbprint -notmatch '^[A-F0-9]{40}$') { throw 'Supply the public SHA-1 certificate thumbprint (not a password or private key).' }
$timestamp = [Uri]$TimestampUrl
if (!$timestamp.IsAbsoluteUri -or $timestamp.Scheme -ne 'https') { throw 'An HTTPS RFC 3161 timestamp service is required.' }
$certificate = Get-Item "Cert:\CurrentUser\My\$thumbprint" -ErrorAction Stop
if (!$certificate.HasPrivateKey) { throw 'Certificate has no accessible private key. Configure its Windows key provider or hardware token.' }
if ($certificate.NotAfter -le (Get-Date) -or $certificate.NotBefore -gt (Get-Date)) { throw 'Certificate is not currently valid.' }
if (!($certificate.EnhancedKeyUsageList | Where-Object { $_.ObjectId.Value -eq '1.3.6.1.5.5.7.3.3' })) {
    throw 'Certificate must explicitly permit Code Signing.'
}
$chain = [System.Security.Cryptography.X509Certificates.X509Chain]::new()
try {
    $chain.ChainPolicy.RevocationMode = [System.Security.Cryptography.X509Certificates.X509RevocationMode]::Online
    if (!$chain.Build($certificate)) { throw 'Windows could not validate the publisher certificate chain and revocation status.' }
} finally { $chain.Dispose() }
$sdkBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
$signtool = Get-ChildItem "$sdkBin/*/x64/signtool.exe" | Sort-Object { [version]$_.Directory.Parent.Name } -Descending | Select-Object -First 1
if (!$signtool) { throw 'Install Windows SDK Signing Tools (signtool.exe).' }
$compiler = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'
if (!(Test-Path $compiler)) { throw 'Install Inno Setup 6.' }
$publish = (Resolve-Path $PublishDirectory).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path $output) { throw 'Output directory already exists. Choose a new directory to avoid mixing signed and unsigned artifacts.' }
$ownFiles = @('Soultrap.exe', 'Soultrap.dll', 'Soultrap.Core.dll', 'Soultrap.Infrastructure.dll')
foreach ($name in $ownFiles) {
    if (!(Test-Path (Join-Path $publish $name))) { throw "Publish output is missing $name." }
}
New-Item -ItemType Directory -Path $output | Out-Null
$staging = Join-Path $output 'publish'
Copy-Item -LiteralPath $publish -Destination $staging -Recurse
function Confirm-PublisherSignature([string]$FilePath) {
    $signature = Get-AuthenticodeSignature -LiteralPath $FilePath
    if ($signature.Status -ne 'Valid' -or !$signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $thumbprint -or !$signature.TimeStamperCertificate) {
        throw "Publisher signature or trusted timestamp validation failed: $FilePath"
    }
    & $signtool.FullName verify /pa /all $FilePath
    if ($LASTEXITCODE -ne 0) { throw "SignTool verification failed: $FilePath" }
}
try {
    foreach ($name in $ownFiles) {
        $path = Join-Path $staging $name
        & $signtool.FullName sign /sha1 $thumbprint /fd SHA256 /tr $timestamp.AbsoluteUri /td SHA256 $path
        if ($LASTEXITCODE -ne 0) { throw "Signing failed: $name" }
        Confirm-PublisherSignature $path
    }
    # Inno Setup signs both the installer and its embedded uninstaller using this command.
    # $f is Inno Setup's quoted filename placeholder; do not expand it in PowerShell.
    $signCommand = '"' + $signtool.FullName + '" sign /sha1 ' + $thumbprint + ' /fd SHA256 /tr "' + $timestamp.AbsoluteUri + '" /td SHA256 $f'
    $installerDir = Join-Path $output 'installer'
    $iss = Join-Path $PSScriptRoot '../Soultrap.Installer/Soultrap.iss'
    & $compiler /DSignedBuild "/DPublishDir=$staging" "/O$installerDir" "/Ssoultrap=$signCommand" $iss
    if ($LASTEXITCODE -ne 0) { throw 'Signed installer compilation failed.' }
    $installers = @(Get-ChildItem $installerDir -Filter '*.exe')
    if ($installers.Count -ne 1) { throw 'Expected exactly one signed installer.' }
    Confirm-PublisherSignature $installers[0].FullName
    $archive = Join-Path $output 'Soultrap-0.1.0-win-x64.zip'
    Compress-Archive -Path "$staging/*" -DestinationPath $archive
    $files = @((Get-Item $archive)) + $installers
    $lines = foreach ($file in $files) { '{0}  {1}' -f (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $file.Name }
    $lines | Set-Content (Join-Path $output 'SHA256SUMS.txt') -Encoding utf8NoBOM
    Write-Host "Signed and verified artifacts: $output"
    Write-Host 'A valid signature identifies the publisher; SmartScreen reputation is decided by Microsoft.'
} catch {
    # Incomplete staging may be useful for diagnosis. No completion manifest is retained on failure.
    Remove-Item (Join-Path $output 'SHA256SUMS.txt') -ErrorAction SilentlyContinue
    throw
}
