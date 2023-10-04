#------------------------------------------------------------------------------
# This script downloads files needed to create a bundle from Verifone_Repo.
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
$repoPAT       = "${env:PAT}"

$user = "IPADevTestLFSController"
$repoPAT
$repoName = "Verifone_Repo"
$sourceFile
$sourcePath
$vipaRepoUrl
$vipaVersion
$vipaPath

$downloadDir = "${env:AGENT_BUILDDIRECTORY}/_src"
$downloadUri = "https://dev.azure.com/sphereclientsolutions/Integrated%20Applications/_apis/git/repositories/$repoName/items?api-version=7.1&"

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
  #$script:localRepoDir = "C:\git\EMVConfiguration"
  #$script:configuration = "attended"
  #$script:device = "M400"
  #$script:processor = "CHASE"
  #$script:vipaVersion = "6.8.2.32"
  #$script:stagingDir = "C:\tmp\_src\temp"
  #$script:baseBundle = "VIPA_$($vipaVersion)_$($device)_release_install_sphere.tgz"
  #$script:downloadDir = "C:\tmp\_src"
  #$script:bundleDir = "$downloadDir\bundle"
  #$script:tempDir = "$downloadDir\temp"
  #$script:repoPAT = "[PUT YOUR PAT HERE]"

  $vipaVersionPath = $vipaVersion.Replace('.', '_')
  
  $script:vipaPath = "Platforms/VIPA/$vipaVersionPath"
  $script:sourcePath = "Platforms/VIPA/$vipaVersionPath/Source/$device"
  $script:sourceFile = "VIPA_$($vipaVersion)_$($device)_release_install_sphere.tgz"
  $script:vipaRepoUrl = "https://$user`:$repoPAT@sphereclientsolutions.visualstudio.com/Integrated%20Applications/_git/$repoName"

  Create-Directory $downloadDir $True
}

function Write-DownloadStatus
{
  param([string] $statusCode)
  
  if ($statusCode -eq "200")
  {
    Write-Host "Download status code: $statusCode" -ForegroundColor Green
  }
  else
  {
    Write-Host "Download status code: $statusCode" -ForegroundColor Red
  }
}

function Download-VipaSource-AzureApi
{
  # Ref: https://learn.microsoft.com/en-us/rest/api/azure/devops/git/items/get?view=azure-devops-rest-7.1&tabs=HTTP
  $uri = "$($downloadUri)path=/$sourcePath/$sourceFile&resolveLfs=true"
  $token = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$repoPAT"))
  $header = @{ authorization = "Basic $token" }
  
  Write-Host "Downloading VIPA source $sourceFile..."
  Invoke-RestMethod -Uri $uri -Method GET -Headers $header -OutFile "$downloadDir\$sourceFile" -StatusCodeVariable "statusCode" -SkipHttpErrorCheck
  Write-DownloadStatus $statusCode

  # Read the manifest and download additional artifacts
  $manifest = Get-Content -Path "$PSScriptRoot\manifest.json" | Out-String | ConvertFrom-Json
  $deviceLowercase = $device.ToLower()
  $vipaVersionPath = $vipaVersion.Replace('.', '_')
  
  foreach ($item in $manifest.add)
  {
    $file = $($item.name).Replace("[DEVICE_L]", $deviceLowercase).Replace("[DEVICE_U]", $device).Replace("[VERSION]", $vipaVersionPath).Replace("[PROCESSOR]", $processor).Replace("[CONFIGURATION]", $configuration)
    $path = $($item.source).Replace("[DEVICE_L]", $deviceLowercase).Replace("[DEVICE_U]", $device).Replace("[VERSION]", $vipaVersionPath).Replace("[PROCESSOR]", $processor).Replace("[CONFIGURATION]", $configuration)
    $uri = "$($downloadUri)path=/$path/$file&resolveLfs=true&download=true"
    
    if ($file.StartsWith("idleScreen") -and ($device -eq "UX301"))  # UX301 doesn't use our idle screen
    {
      continue
    }
    
    if (($file -eq "signature.html") -and (($device -eq "P200") -or ($device -eq "UX301"))) # Signature file is only for M400/P400
    {
      continue
    }
    
    Write-Host "Downloading $file..."
    Invoke-RestMethod -Uri $uri -Method GET -Headers $header -OutFile "$downloadDir\$file" -StatusCodeVariable "statusCode" -SkipHttpErrorCheck
    Write-DownloadStatus $statusCode
  }
  
  # Download the BundleVerifier solution as a zip file
  $uri = "$($downloadUri)scopePath=/Platforms/Tools/CSharp/BundleVerifier&download=true&`$format=zip"

  Write-Host "Downloading BundleVerifier solution..."
  Invoke-RestMethod -Uri $uri -Method GET -Headers $header -ContentType "application/zip" -OutFile "$downloadDir\Verifier.zip" -StatusCodeVariable "statusCode" -SkipHttpErrorCheck
  Write-DownloadStatus $statusCode
  
  # Extract BundleVerifier solution. It will be in BundleVerifier folder.
  $sevenZip = "$PSScriptRoot\7z.exe"
  & $sevenZip x "$downloadDir\Verifier.zip" -tzip -o"$downloadDir"
}

function Download-VipaSource-Git
{
  #TODO: sparse checkout still takes too long and pulls GiBs of data. Need to find a better way.
  #TODO: powershell Out-File overrides the file content. Need to append.

  Set-Location $downloadDir
  git init
  git lfs install --local
  git config core.sparsecheckout true
  
  $deviceLowercase = $device.ToLower()
  
  # Base bundle
  "$vipaPath/Source/$device" | Out-File ".git\info\sparse-checkout"

  # Language file
  "$vipaPath/SphereArtifacts/usr1.VIPA_www/eng" | Out-File ".git\info\sparse-checkout"

  # HTML files
  "$vipaPath/SphereArtifacts/usr1.VIPA_www/$device" | Out-File ".git\info\sparse-checkout"
  
  # Reboot
  "Platforms/VIPA/SphereArtifacts/config_hr_mm_ss_reboot-01.00.00.tgz" | Out-File ".git\info\sparse-checkout"
  "Platforms/VIPA/SphereArtifacts/config_hr_mm_ss_reboot-01.00.00.tgz.p7s" | Out-File ".git\info\sparse-checkout"
  
  # Idle screen
  "Platforms/VIPA/SphereArtifacts/$deviceLowercase" | Out-File ".git\info\sparse-checkout"
  
  git remote add origin $vipaRepoUrl
  #git pull origin main
}


#------------------------------------------------------------------------------
#  Script Main
#------------------------------------------------------------------------------

Initialize-Variables

Download-VipaSource-AzureApi
#Download-VipaSource-Git
