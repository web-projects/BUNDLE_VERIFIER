#------------------------------------------------------------------------------
# This script creates a consolidated bundle. It should be executed after 
# EMVConfiguration repo has been pulleed and the required files from 
# Verifone_Repo have been downloaded to $downloadDir.
#
# To run locally, uncomment the "Local overrides" block and change the values
# of those variables accordingly.
#------------------------------------------------------------------------------

# Predevined and pipeline variables
$localRepoDir  = "${env:BUILD_REPOSITORY_LOCALPATH}"
$stagingDir    = "${env:BUILD_ARTIFACTSTAGINGDIRECTORY}"
$configuration = "${env:CONFIGURATION}"
$device        = "${env:DEVICE}"
$processor     = "${env:PROCESSOR}"
$vipaVersion   = "${env:VIPAVERSION}"

$baseBundle = "VIPA_$($vipaVersion)_$($device)_release_install_sphere.tgz"
$sevenZip = "$PSScriptRoot\7z.exe"

$downloadDir = "${env:AGENT_BUILDDIRECTORY}\_src"
$bundleDir = "$downloadDir\bundle"
$tempDir = "$downloadDir\temp"
$manifest

function Create-Directory
{
  param([string] $dir, [bool] $deleteIfExists)
  
  if ($deleteIfExists -and (Test-Path -Path $dir -PathType Container))
  {
    Remove-Item -Path $dir -Force -Recurse
  }
  
  if (-not (Test-Path -Path $dir -PathType Container))
  {
    New-Item -ItemType Directory -Path $dir -Force
  }
}

function Initialize-Variables
{
  # Local overrides
  $script:localRepoDir = "C:\git\EMVConfiguration"
  $script:configuration = "attendednopin"
  $script:device = "P200"
  $script:processor = "FDRC"
  $script:vipaVersion = "6.8.2.32"
  $script:stagingDir = "C:\tmp\_src\temp"
  $script:baseBundle = "VIPA_$($vipaVersion)_$($device)_release_install_sphere.tgz"
  $script:downloadDir = "C:\tmp\_src"
  $script:bundleDir = "$downloadDir\bundle"
  $script:tempDir = "$downloadDir\temp"
  
  $script:manifest = Get-Content -Path "$PSScriptRoot\manifest.json" | Out-String | ConvertFrom-Json
  
  Create-Directory $bundleDir $True
  Create-Directory $tempDir $True
}

function Extract-VipaSource
{
  Write-Host "Extracting files to $bundleDir..."
  & $sevenZip x "$downloadDir\$baseBundle" -ttar -o"$bundleDir"
}

function Remove-Files
{
  foreach ($item in $manifest.remove)
  {
    $path = Resolve-Path "$bundleDir\$($item.path)"
    $path = "$path\$($item.name)"
    
    if (Test-Path -Path $path -PathType Container)
    {
      Write-Host "Removing $path..."
      Remove-Item -Path $path -Force -Recurse
    }
  }
}

function Copy-File
{
  param([string] $sourceDir, [string] $file, [string] $targetDir)
  
  if (Test-Path -Path "$targetDir\$file" -PathType Leaf)
  {
    Write-Host "Removing $targetDir\$file..."
    Remove-Item -Path "$targetDir\$file" -Force
  }
  
  if (Test-Path -Path "$sourceDir\$file" -PathType Leaf)
  {
    Write-Host "Copying $file to $targetDir..."
    Copy-Item -Path "$sourceDir\$file" -Destination "$targetDir"
  }
  else
  {
    Write-Host "Cannot copy file: $sourceDir\$file does not exist." -ForegroundColor Red
  }
}

function Copy-SphereFiles
{
  Copy-File $downloadDir "config_hr_mm_ss_reboot-01.00.00.tgz" $bundleDir
  Copy-File $downloadDir "config_hr_mm_ss_reboot-01.00.00.tgz.p7s" $bundleDir

  $deviceLowercase = $device.ToLower()

  Copy-File $downloadDir "idleScreen_$($deviceLowercase).tgz" $bundleDir
  Copy-File $downloadDir "idleScreen_$($deviceLowercase).tgz.p7s" $bundleDir
}

function Update-VersionPackage
{
  # vipa_ver.txt/.p7s are in usr1.VIPA_bin.tgz/usr1.VIPA_bin.tar/pkg.bincfg.tgz/pkg.bincfg.tar
  # Extract nested archives
  $level1File = "$bundleDir\usr1.VIPA_bin.tgz"
  $level1Dir = "$($level1File).dir"
  & $sevenZip x $level1File -o"$level1Dir"
  
  $level2File = "$level1Dir\usr1.VIPA_bin.tar"
  $level2Dir = "$($level2File).dir"
  & $sevenZip x $level2File -o"$level2Dir"

  $level3File = "$level2Dir\pkg.bincfg.tgz"
  $level3Dir = "$($level3File).dir"
  & $sevenZip x $level3File -o"$level3Dir"

  $level4File = "$level3Dir\pkg.bincfg.tar"
  $level4Dir = "$($level4File).dir"
  & $sevenZip x $level4File -o"$level4Dir"
  
  # 2. Copy version files
  Copy-File $downloadDir "vipa_ver.txt" $level4Dir
  Copy-File $downloadDir "vipa_ver.txt.p7s" $level4Dir
  
  # 3. Going backward
  Write-Host "Creating pkg.bincfg.tar..."
  & $sevenZip a -ttar "$tempDir\pkg.bincfg.tar" "$level4Dir\*"
  
  Write-Host "Creating pkg.bincfg.tgz..."
  & $sevenZip a -tgzip "$tempDir\pkg.bincfg.tgz" "$tempDir\pkg.bincfg.tar"
  
  Write-Host "Removing $level3Dir..."
  Remove-Item -Path "$level3Dir" -Force -Recurse
  
  # Replace pkg.bincfg.tgz
  Copy-File "$tempDir" "pkg.bincfg.tgz" "$level2Dir"
  
  Write-Host "Creating usr1.VIPA_bin.tar..."
  & $sevenZip a -ttar "$tempDir\usr1.VIPA_bin.tar" "$level2Dir\*"

  Write-Host "Creating usr1.VIPA_bin.tgz..."
  & $sevenZip a -tgzip "$tempDir\usr1.VIPA_bin.tgz" "$tempDir\usr1.VIPA_bin.tar"

  Write-Host "Removing $level1Dir..."
  Remove-Item -Path "$level1Dir" -Force -Recurse

  # Replace pkg.bincfg.tgz
  Copy-File "$tempDir" "usr1.VIPA_bin.tgz" "$bundleDir"
}

function Update-WwwPackage
{
  # Relevant files are in usr1.VIPA_www.tgz/usr1.VIPA_www.tar/pkg.www.tgz/pkg.www.tar
  
  # Extract nested archives
  $level1File = "$bundleDir\usr1.VIPA_www.tgz"
  $level1Dir = "$($level1File).dir"
  & $sevenZip x $level1File -o"$level1Dir"
  
  $level2File = "$level1Dir\usr1.VIPA_www.tar"
  $level2Dir = "$($level2File).dir"
  & $sevenZip x $level2File -o"$level2Dir"

  $level3File = "$level2Dir\pkg.www.tgz"
  $level3Dir = "$($level3File).dir"
  & $sevenZip x $level3File -o"$level3Dir"

  $level4File = "$level3Dir\pkg.www.tar"
  $level4Dir = "$($level4File).dir"
  & $sevenZip x $level4File -o"$level4Dir"

  # Copy/Replace files
  Copy-File "$downloadDir" "eng" "$level4Dir\www\mapp\langs"
  Copy-File "$downloadDir" "display_message.html" "$level4Dir\www\mapp"
  Copy-File "$downloadDir" "idle.html" "$level4Dir\www\mapp"
  Copy-File "$downloadDir" "signature.html" "$level4Dir\www\mapp"
  Copy-File "$downloadDir" "verify_amount.html" "$level4Dir\www\mapp"

  Write-Host "Creating pkg.www.tar..."
  & $sevenZip a -ttar "$tempDir\pkg.www.tar" "$level4Dir\*"
  
  Write-Host "Creating pkg.www.tgz..."
  & $sevenZip a -tgzip "$tempDir\pkg.www.tgz" "$tempDir\pkg.www.tar"
  
  Write-Host "Removing $level3Dir..."
  Remove-Item -Path "$level3Dir" -Force -Recurse
  
  # Replace pkg.bincfg.tgz
  Copy-File "$tempDir" "pkg.www.tgz" "$level2Dir"
  
  Write-Host "Creating usr1.VIPA_www.tar..."
  & $sevenZip a -ttar "$tempDir\usr1.VIPA_www.tar" "$level2Dir\*"

  Write-Host "Creating usr1.VIPA_www.tgz..."
  & $sevenZip a -tgzip "$tempDir\usr1.VIPA_www.tgz" "$tempDir\usr1.VIPA_www.tar"

  Write-Host "Removing $level1Dir..."
  Remove-Item -Path "$level1Dir" -Force -Recurse

  # Replace usr1.VIPA_www.tgz
  Copy-File "$tempDir" "usr1.VIPA_www.tgz" "$bundleDir"
}

function Update-ConfigPackage
{
  # Relevant files are in usr1.VIPA_cfg.tgz/usr1.VIPA_cfg.tar/pkg.cfg.tgz/pkg.cfg.tar
  
  # Extract nested archives
  $level1File = "$bundleDir\usr1.VIPA_cfg.tgz"
  $level1Dir = "$($level1File).dir"
  & $sevenZip x $level1File -o"$level1Dir"
  
  $level2File = "$level1Dir\usr1.VIPA_cfg.tar"
  $level2Dir = "$($level2File).dir"
  & $sevenZip x $level2File -o"$level2Dir"

  $level3File = "$level2Dir\pkg.cfg.tgz"
  $level3Dir = "$($level3File).dir"
  & $sevenZip x $level3File -o"$level3Dir"

  $level4File = "$level3Dir\pkg.cfg.tar"
  $level4Dir = "$($level4File).dir"
  & $sevenZip x $level4File -o"$level4Dir"

  # Copy/Replace files
  $versionUnderscore = $vipaVersion.Replace('.', '_')
  $sourceDir = "$localRepoDir\Verifone\VIPA\$versionUnderscore\Configurations\$processor\CONFIG\$configuration\VIPA_cfg"
  Copy-Item -Path "$sourceDir\*" -Destination "$level4Dir" -Force
  
  # Going backward
  Write-Host "Creating pkg.cfg.tar..."
  & $sevenZip a -ttar "$tempDir\pkg.cfg.tar" "$level4Dir\*"
  
  Write-Host "Creating pkg.cfg.tgz..."
  & $sevenZip a -tgzip "$tempDir\pkg.cfg.tgz" "$tempDir\pkg.cfg.tar"
  
  Write-Host "Removing $level3Dir..."
  Remove-Item -Path "$level3Dir" -Force -Recurse

  # Replace pkg.bincfg.tgz
  Write-Host "Copying pkg.cfg.tgz to $level2Dir..."
  Copy-File "$tempDir" "pkg.cfg.tgz" "$level2Dir"
  
  Write-Host "Creating usr1.VIPA_cfg.tar..."
  & $sevenZip a -ttar "$tempDir\usr1.VIPA_cfg.tar" "$level2Dir\*"

  Write-Host "Creating usr1.VIPA_cfg.tgz..."
  & $sevenZip a -tgzip "$tempDir\usr1.VIPA_cfg.tgz" "$tempDir\usr1.VIPA_cfg.tar"

  Write-Host "Removing $level1Dir..."
  Remove-Item -Path "$level1Dir" -Force -Recurse

  # Replace usr1.VIPA_cfg.tgz
  Copy-File "$tempDir" "usr1.VIPA_cfg.tgz" "$bundleDir"
}

function Update-EmvConfigPackage
{
  # Relevant files are in usr1.VIPA_cfg_emv.tgz/usr1.VIPA_cfg_emv.tar/pkg.cfgemv.tgz/pkg.cfgemv.tar
  #                   and usr1.VIPA_cfg_emv.tgz/usr1.VIPA_cfg_emv.tar/pkg.desiredkernels.tgz/pkg.desiredkernels.tar
  
  # Extract nested archives
  $level1File = "$bundleDir\usr1.VIPA_cfg_emv.tgz"
  $level1Dir = "$($level1File).dir"
  & $sevenZip x $level1File -o"$level1Dir"
  
  $level2File = "$level1Dir\usr1.VIPA_cfg_emv.tar"
  $level2Dir = "$($level2File).dir"
  & $sevenZip x $level2File -o"$level2Dir"

  $level3File = "$level2Dir\pkg.cfgemv.tgz"
  $level3Dir = "$($level3File).dir"
  & $sevenZip x $level3File -o"$level3Dir"

  $level4File = "$level3Dir\pkg.cfgemv.tar"
  $level4Dir = "$($level4File).dir"
  & $sevenZip x $level4File -o"$level4Dir"

  $level3FileKernel = "$level2Dir\pkg.desiredkernels.tgz"
  $level3DirKernel = "$($level3FileKernel).dir"
  & $sevenZip x $level3FileKernel -o"$level3DirKernel"

  $level4FileKernel = "$level3DirKernel\pkg.desiredkernels.tar"
  $level4DirKernel = "$($level4FileKernel).dir"
  & $sevenZip x $level4FileKernel -o"$level4DirKernel"

  # Copy/Replace files
  $versionUnderscore = $vipaVersion.Replace('.', '_')
  $sourceDir = "$localRepoDir\Verifone\VIPA\$versionUnderscore\Configurations\$processor\CONFIG\$configuration\VIPA_emv"
  Copy-Item -Path "$sourceDir\cfgemv\PROD\*" -Destination "$level4Dir" -Force
  Copy-Item -Path "$sourceDir\desiredkernels\*" -Destination "$level4DirKernel" -Force
  
  # Going backward
  Write-Host "Creating pkg.desiredkernels.tar..."
  & $sevenZip a -ttar "$tempDir\pkg.desiredkernels.tar" "$level4DirKernel\*"

  Write-Host "Creating pkg.desiredkernels.tgz.."
  & $sevenZip a -tgzip "$tempDir\pkg.desiredkernels.tgz" "$tempDir\pkg.desiredkernels.tar"
  
  Write-Host "Creating pkg.cfgemv.tar..."
  & $sevenZip a -ttar "$tempDir\pkg.cfgemv.tar" "$level4Dir\*"

  Write-Host "Creating pkg.cfgemv.tgz.."
  & $sevenZip a -tgzip "$tempDir\pkg.cfgemv.tgz" "$tempDir\pkg.cfgemv.tar"
  
  Write-Host "Removing $level3Dir..."
  Remove-Item -Path "$level3Dir" -Force -Recurse

  # Replace pkg.desiredkernels.tgz
  Copy-File "$tempDir" "pkg.desiredkernels.tgz" "$level2Dir"
  
  # Replace pkg.cfgemv.tgz
  Copy-File "$tempDir" "pkg.cfgemv.tgz" "$level2Dir"
  
  Write-Host "Creating usr1.VIPA_cfg_emv.tar..."
  & $sevenZip a -ttar "$tempDir\usr1.VIPA_cfg_emv.tar" "$level2Dir\*"

  Write-Host "Creating usr1.VIPA_cfg_emv.tgz..."
  & $sevenZip a -tgzip "$tempDir\usr1.VIPA_cfg_emv.tgz" "$tempDir\usr1.VIPA_cfg_emv.tar"

  Write-Host "Removing $level1Dir..."
  Remove-Item -Path "$level1Dir" -Force -Recurse

  # Replace usr1.VIPA_cfg_emv.tgz
  Copy-File "$tempDir" "usr1.VIPA_cfg_emv.tgz" "$bundleDir"
}

function Create-FinalBundle
{
  Write-Host "Creating bundle..."
  
  $deviceLowercase = $device.ToLower()
  $versionUnderscore = $vipaVersion.Replace('.', '_')

  # Content of vipar_ver.txt: sphere.sphere.vipa....m400.6_8_2_32.230706
  $vipaDate = Get-Content -Path "$downloadDir\vipa_ver.txt"
  $vipaDate = $vipaDate.SubString($vipaDate.LastIndexOf('.') + 1)
  
  # EMV version is in EMVConfiguration repo, Verifone\VIPA\6_8_2_32\Configurations\CHASE\CONFIG\attended\VIPA_cfg\emv_ver.txt
  # Content of emv_ver.txt: sphere.sphere.emv.attended.CHASE...6_8_2_32.230828
  $emvDate = Get-Content -Path "$localRepoDir\Verifone\VIPA\$versionUnderscore\Configurations\CHASE\CONFIG\attended\VIPA_cfg\emv_ver.txt"
  $emvDate = $emvDate.SubString($emvDate.LastIndexOf('.') + 1)

  # Bundle name format: sphere.sphere.consolidated.attended.CHASE..m400.6_8_2_32.230706.230828.tar
  $finalBundle = "sphere.sphere.consolidated.$($configuration).$($processor)..$($deviceLowercase).$($versionUnderscore).$($vipaDate).$($emvDate).tar"
  
  # Create the final bundle and put it in the staging directory
  & $sevenZip a -ttar "$stagingDir\$finalBundle" "$bundleDir\*"
  
  # Output the final bundle name for use in subseqent tasks
  Write-Host "##vso[task.setvariable variable=filename;isOutput=true;]$finalBundle"
}


#------------------------------------------------------------------------------
#  Script Main
#------------------------------------------------------------------------------

Initialize-Variables

Extract-VipaSource
Remove-Files
Copy-SphereFiles
Update-VersionPackage
Update-WwwPackage
Update-ConfigPackage
Update-EmvConfigPackage

Create-FinalBundle