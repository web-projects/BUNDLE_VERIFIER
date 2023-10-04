#------------------------------------------------------------------------------
#  List all applicable variables for the record of a build
#------------------------------------------------------------------------------

Get-Date
Write-Host "Build Variables:"
Write-Host "  Configuarion = ${env:CONFIGURATION}"
Write-Host "  Device = ${env:DEVICE}"
Write-Host "  Processor = ${env:PROCESSOR}"
Write-Host "  VIPA version = ${env:VIPAVERSION}"
Write-Host "  LFS Upload = ${env:LFSUPLOAD}"

# Do some basic validations

if ((${env:CONFIGURATION} -eq "") -or (${env:CONFIGURATION} -eq $null))
{
  Write-Host "Bundle configuarion is required." -ForegroundColor Red
  throw
}

if ((${env:DEVICE}-eq "") -or (${env:DEVICE} -eq $null))
{
  Write-Host "Device model is required." -ForegroundColor Red
  throw
}

if ((${env:PROCESSOR}-eq "") -or (${env:PROCESSOR} -eq $null))
{
  Write-Host "Bundle processor is required." -ForegroundColor Red
  throw
}

if ((${env:VIPAVERSION} -eq "") -or (${env:VIPAVERSION} -eq $null))
{
  Write-Host "VIPA version is required." -ForegroundColor Red
  throw
}
