#------------------------------------------------------------------------------
# This script validates the bundle created by CreateBundle.ps1.
# The BundleVerifier locates at 
#   $(Agent.BuildDirectory)\_src\BundleVerifier\Application\bin\Release\netcoreapp3.1
#
# To run locally, uncomment the "Local overrides" block and change the values
# of those variables accordingly.
#------------------------------------------------------------------------------

param([string] $bundleFilename)


# Predevined and pipeline variables
$localRepoDir  = "${env:BUILD_REPOSITORY_LOCALPATH}"
$stagingDir    = "${env:BUILD_ARTIFACTSTAGINGDIRECTORY}"
$configuration = "${env:CONFIGURATION}"
$device        = "${env:DEVICE}"
$processor     = "${env:PROCESSOR}"
$vipaVersion   = "${env:VIPAVERSION}"
$downloadDir = "${env:AGENT_BUILDDIRECTORY}\_src"
$verifierDir

function Initialize-Variables
{
  if (($bundleFilename -eq "") -or ($bundleFilename -eq $null))
  {
    Write-Host "Final bundle filename is required." -ForegroundColor Red
    throw
  }
  
  # Local overrides
  #$script:localRepoDir = "C:\git\EMVConfiguration"
  #$script:stagingDir = "C:\tmp\_src\temp"
  #$script:configuration = "attendednopin"
  #$script:device = "M400"
  #$script:processor = "CHASE"
  #$script:vipaVersion = "M400"
  #$script:downloadDir = "C:\tmp\_src"
  
  $script:verifierDir = "$downloadDir\BundleVerifier\Application\bin\Release\net6.0"
  
  Write-Host "Bundle name: $bundleFilename"
}

function Create-AppSettings
{
  # Replace the appsettings.json in Verify.exe with a new for the newly created bundle
  $appSettings = Get-Content -Path "$PSScriptRoot\Verifier.appsettings.json"
  $appSettings = $appSettings.Replace("[BUNDLE_FILE]", $bundleFilename)
  
  $downloadDirTemp = $downloadDir.Replace("\", "\\")
  $localRepoDirTemp = $localRepoDir.Replace("\", "\\")
  $version_ = $vipaVersion.Replace('.', '_')
  
  $appSettings = $appSettings.Replace("[DOWNLOAD_DIR]", $downloadDirTemp).Replace("[LOCAL_REPO]", $localRepoDirTemp)
  $appSettings = $appSettings.Replace("[VERSION_]", $version_)
  $appSettings = $appSettings.Replace("[PROCESSOR]", $processor)
  $appSettings = $appSettings.Replace("[CONFIGURATION]", $configuration)
  
  $appSettings | Out-File "$verifierDir\appsettings.json" -Force
}

function Verify-Bundle
{
  $verifier = "$verifierDir\BundleValidator.exe"
  Set-Location "$verifierDir"

  & $verifier -BundleDir:"$stagingDir" -Pipeline:"true"
}

#------------------------------------------------------------------------------
#  Script Main
#------------------------------------------------------------------------------

Initialize-Variables

Create-AppSettings

Verify-Bundle