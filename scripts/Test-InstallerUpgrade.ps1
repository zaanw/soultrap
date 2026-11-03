#Requires -Version 7.0
$ErrorActionPreference='Stop'
$target=Join-Path $env:TEMP ('Soultrap-upgrade-'+[guid]::NewGuid().ToString('N'))
$data=Join-Path $env:LOCALAPPDATA 'Soultrap'
if(Test-Path $data){throw 'Expected an isolated Windows runner without existing Soultrap user data.'}
$previous=Join-Path $pwd 'artifacts/previous'
New-Item -ItemType Directory -Path $previous -Force | Out-Null
# Future builds use the durable latest public release. The initial transition uses our validated 0.2.1 CI artifact.
gh release download --repo $env:GITHUB_REPOSITORY --pattern '*Setup-x64.exe' --dir $previous
if($LASTEXITCODE -ne 0){
    gh run download 41661487829 --repo $env:GITHUB_REPOSITORY --name Soultrap-Windows-x64 --dir $previous
    if($LASTEXITCODE -ne 0){throw 'Cannot obtain the baseline installer.'}
}
$old=@(Get-ChildItem $previous -Recurse -Filter '*Setup-x64.exe')
$new=@(Get-ChildItem artifacts/installer -Filter '*Setup-x64.exe')
if($old.Count -ne 1 -or $new.Count -ne 1){throw 'Expected exactly one old and new installer.'}
function Run-Setup($file,$extra=''){
    $process=Start-Process -FilePath $file -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /DIR=`"$target`" $extra" -PassThru
    if(!$process.WaitForExit(120000)){$process.Kill();throw 'Installer timed out.'}
    if($process.ExitCode -ne 0){throw "Installer failed: $($process.ExitCode)"}
}
try{
    Run-Setup $old[0].FullName
    $exe=Join-Path $target 'Soultrap.exe'
    $before=[version](Get-Item $exe).VersionInfo.FileVersion
    New-Item -ItemType Directory -Path (Join-Path $data 'backups') -Force | Out-Null
    $settings=Join-Path $data 'settings.json'
    '{"Schema":1,"Theme":"Retro Green","CheckUpdates":false,"ShowSplashScreen":false}' | Set-Content $settings -Encoding utf8NoBOM
    $backup=Join-Path $data 'backups/keep-marker.bin'
    'retain backup bytes' | Set-Content $backup
    $settingsHash=(Get-FileHash $settings).Hash;$backupHash=(Get-FileHash $backup).Hash
    Run-Setup $new[0].FullName '/SOULTRAPUPDATE=1'
    $after=[version](Get-Item $exe).VersionInfo.FileVersion
    if($after -lt $before){throw 'Installer downgraded the application.'}
    if($after.Major -ne 0 -or $after.Minor -ne 3){throw "Unexpected installed version $after"}
    if((Get-FileHash $settings).Hash -ne $settingsHash -or (Get-FileHash $backup).Hash -ne $backupHash){throw 'Upgrade modified settings or backups.'}
    $app=$null
    for($i=0;$i -lt 150;$i++){
        $app=Get-Process Soultrap -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe -and $_.MainWindowTitle -eq 'SOULTRAP'} | Select-Object -First 1
        if($app){break};Start-Sleep -Milliseconds 100
    }
    if(!$app){throw 'Updated application did not restart to the main window.'}
    $app.CloseMainWindow() | Out-Null
    if(!$app.WaitForExit(10000)){$app.Kill();throw 'Updated application did not close.'}
    $uninstaller=Join-Path $target 'unins000.exe'
    $uninstall=Start-Process $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -PassThru -Wait
    if($uninstall.ExitCode -ne 0){throw 'Uninstall failed.'}
    if((Get-FileHash $settings).Hash -ne $settingsHash -or (Get-FileHash $backup).Hash -ne $backupHash){throw 'Uninstall removed user data.'}
    Write-Host "Upgrade $before -> $after, relaunch, settings/backup preservation and uninstall passed."
}finally{
    Get-Process Soultrap -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq (Join-Path $target 'Soultrap.exe')} | Stop-Process -ErrorAction SilentlyContinue
    Remove-Item $data -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $target -Recurse -Force -ErrorAction SilentlyContinue
}
