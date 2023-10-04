#------------------------------------------------------------------------------
# This script uploads the bundle to LFS.
#
# To run locally, uncomment the "Local overrides" block and change the values
# of those variables accordingly.
#------------------------------------------------------------------------------

param([string] $bundleFilename)


# Predefined and pipeline variables
$localRepoDir  = "${env:BUILD_REPOSITORY_LOCALPATH}"
$stagingDir    = "${env:BUILD_ARTIFACTSTAGINGDIRECTORY}"
$configuration = "${env:CONFIGURATION}"
$device        = "${env:DEVICE}"
$processor     = "${env:PROCESSOR}"
$vipaVersion   = "${env:VIPAVERSION}"
$repoPAT       = "${env:PAT}"
$repoUser      = "IPADevTestLFSController"

$downloadDir = "${env:AGENT_BUILDDIRECTORY}\_src"
$repoUrl = "https://dev.azure.com/sphereclientsolutions/Integrated%20Applications/_apis/git/repositories/Verifone_Repo"
$header

function Initialize-Variables
{
  if (($bundleFilename -eq "") -or ($bundleFilename -eq $null))
  {
    Write-Host "Final bundle filename is required." -ForegroundColor Red
    throw
  }
  
  # Local overrides
  #$script:localRepoDir = "C:\git\EMVConfiguration"
  #$script:configuration = "attended"
  #$script:device = "M400"
  #$script:processor = "CHASE"
  #$script:vipaVersion = "6.8.2.32"
  #$script:stagingDir = "C:\tmp\_src\temp"
  #$script:downloadDir = "C:\tmp\_src"
  #$script:repoPAT = "[PUT YOUR PAT HERE]"
  #$script:repoUser = "[PUT YOUR GIT USERNAME HERE]"
  
  $token = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$repoPAT"))
  $script:header = @{ authorization = "Basic $token" }  
   
  Write-Host "Bundle name: $bundleFilename"
}

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

function Write-RequestStatus
{
  param([string] $message, [string] $statusCode)
  
  if ($statusCode -eq "200")
  {
    Write-Host "$message status code: $statusCode" -ForegroundColor Green
  }
  else
  {
    Write-Host "$message status code: $statusCode" -ForegroundColor Red
  }
}

function Get-LastestCommitSha
{
  $uri = "$repoUrl/commits?api-version=7.1&searchCriteria.`$top=1&"
  
  Write-Host "Getting latest commit SHA..."
  $response = Invoke-RestMethod -Uri $uri -Method GET -Headers $header -StatusCodeVariable "statusCode" -SkipHttpErrorCheck
  Write-RequestStatus "Getting latest commit SHA" $statusCode
  
  $sha = $response.value[0].commitId
  Write-Host "Latest commit SHA: $sha"
}

function Upload-Bundle-AzureApi
{
  $lastCommitSha = Get-LastestCommitSha

  $vipaVersionUnderscore = $vipaVersion.Replace('.', '_')
  # E.g.: Platforms/VIPA/6_8_2_32/JIRA/CHASE/M400/attended/[Bundle]
  $filePath = "Platforms/VIPA/$vipaVersionUnderscore/JIRA/$processor/$device/$configuration/$bundleFilename"
  $bundleBytes = Get-Content -Path "$stagingDir\$bundleFilename" -AsByteStream -Raw
  $encodedContent = [System.Convert]::ToBase64String($bundleBytes)
  
  $body = Get-Content -Path "$PSScriptRoot\UploadTemplate.json"
  $body = $body.Replace("[LASTEST_COMMIT_SHA]", $lastCommitSha)
  $body = $body.Replace("[COMMIT_COMMENT]", "Bundle $vipaVersion for $processor $device $configuration")
  $body = $body.Replace("[CHANGE_TYPE]", "add")
  $body = $body.Replace("[FILE_PATH]", $filePath)
  $body = $body.Replace("[FILE_CONTENT]", $encodedContent)
  
  $uri = "$repoUrl/pushes?api-version=7.1"
  
  Write-Host "Uploading bundle..."
  Invoke-RestMethod -Uri $uri -Method POST -Headers $header -Body $body -ContentType "application/json" -StatusCodeVariable "statusCode" -SkipHttpErrorCheck
  Write-RequestStatus "Uploading bundle" $statusCode
  # Error: The maximum request size of 26214400 bytes was exceeded.
}

function Upload-Bundle-Git
{
  $verifoneRepo = "https://$($repoUser):$($repoPAT)@sphereclientsolutions.visualstudio.com/Integrated%20Applications/_git/Verifone_Repo"
  Write-Host "Verifone repo: $verifoneRepo"
  
  $vipaVersionUnderscore = $vipaVersion.Replace('.', '_')
  $pathToAdd = "Platforms/VIPA/$vipaVersionUnderscore/JIRA/$processor/$device/$configuration"
  
  $verifoneLocalRepo = "$downloadDir\verifone"
  Create-Directory $verifoneLocalRepo $True
  
  Set-Location $verifoneLocalRepo

  # A very fast way to set up Verifone_Repo for upload
  git init
  git config --global user.email "$($repoUser)@spherecommerce.com"
  git config --global user.name "$repoUser"
  git clean -ffdx
  git sparse-checkout init --cone
  git sparse-checkout set "$pathToAdd"
  git remote add origin $verifoneRepo
  git fetch --depth=1 origin
  git checkout main
  
  # Create the JIRA folder structures similar to the KIF directory
  Create-Directory "$verifoneLocalRepo\Platforms" $False
  Create-Directory "$verifoneLocalRepo\Platforms\VIPA" $False
  Create-Directory "$verifoneLocalRepo\Platforms\VIPA\$vipaVersionUnderscore" $False
  Create-Directory "$verifoneLocalRepo\Platforms\VIPA\$vipaVersionUnderscore\JIRA" $False
  Create-Directory "$verifoneLocalRepo\Platforms\VIPA\$vipaVersionUnderscore\JIRA\$processor" $False
  Create-Directory "$verifoneLocalRepo\Platforms\VIPA\$vipaVersionUnderscore\JIRA\$processor\$device" $False
  Create-Directory "$verifoneLocalRepo\Platforms\VIPA\$vipaVersionUnderscore\JIRA\$processor\$device\$configuration" $False

  Copy-Item -Path "$stagingDir\$bundleFilename" -Destination "$verifoneLocalRepo\Platforms\VIPA\$vipaVersionUnderscore\JIRA\$processor\$device\$configuration" -Force
  
  git add "Platforms\VIPA\$vipaVersionUnderscore\JIRA\$processor\$device\$configuration\$bundleFilename"
  git commit -m "Bundle $vipaVersion for $processor $device $configuration"
  git push --set-upstream origin main
}

#------------------------------------------------------------------------------
#  Script Main
#------------------------------------------------------------------------------

Initialize-Variables

Upload-Bundle-Git