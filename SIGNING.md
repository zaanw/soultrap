# Windows publisher signing and SmartScreen

The warning **“Windows protected your PC / Unknown publisher”** is Microsoft Defender SmartScreen's application-reputation warning. It is different from Defender detecting a named threat. Current public CI artifacts are **unsigned**. A source-code change or successful virus scan does not establish a trusted publisher or guarantee removal of SmartScreen warnings.

## Required publisher identity

Obtain an Authenticode code-signing certificate from a publicly trusted CA, or configure a managed signing service such as Microsoft Artifact Signing / an eligible open-source signing provider. Follow the provider's identity and key-protection requirements; certificates may require a hardware token or managed key. A self-signed certificate does not establish public trust. Do not share certificate passwords, private keys or tokens in chat or commit them to Git.

No usable publisher certificate or signing-service configuration was supplied for Soultrap. The build cannot become a signed release until this external prerequisite is configured. A proxy CA, TLS certificate or generic Azure environment variable is not a publisher code-signing certificate.

## Sign on the publisher's Windows machine

Install .NET SDK 8.0.427, Windows SDK Signing Tools and Inno Setup 6. Configure the certificate and private-key provider under `Cert:\CurrentUser\My`. Its public certificate thumbprint is not secret. Then:

```powershell
dotnet publish Soultrap.App -c Release -r win-x64 --self-contained true -o artifacts/publish
pwsh -File scripts/Build-SignedRelease.ps1 -CertificateThumbprint YOUR_PUBLIC_CERTIFICATE_THUMBPRINT
```

The script checks certificate validity, code-signing EKU, private-key availability, Windows trust chain and online revocation. It signs copies of the four Soultrap binaries with SHA-256 and an HTTPS RFC 3161 timestamp. It leaves Microsoft's bundled runtime binaries untouched. Inno Setup signs the installer **and embedded uninstaller**. Signature/timestamp validation must pass before the script emits a ZIP and final checksum manifest. Outputs go to a fresh `artifacts/signed` directory; existing output is not overwritten.

A hardware token may request its PIN through the Windows/provider interface. Never put PINs or key passwords in command arguments. The ordinary GitHub-hosted build has no publisher key and deliberately continues to produce unsigned artifacts. The script has syntax validation in Windows CI; a real signed end-to-end build remains untested without a certificate.

## Distribution and reputation

Use a consistent verified publisher identity and distribute the signed artifacts from official GitHub Releases. Keep the executable metadata truthful. Do not modify the files after signing. Signing proves publisher identity and integrity; Microsoft still evaluates reputation and may warn for new signed files, including EV-signed applications.

If Defender reports an actual named threat, record the detection name and submit the exact build to Microsoft's official analysis portal: https://www.microsoft.com/en-us/wdsi/filesubmission . Request review of a suspected false positive. A SmartScreen unknown-publisher warning alone is not evidence of malware or an antivirus false positive.

This project does not change Defender settings, add antivirus exclusions, install a private root CA, or remove download security markings. Those actions would not make the publisher publicly trusted.
