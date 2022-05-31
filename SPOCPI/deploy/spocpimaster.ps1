<#
***********************************************************************************
Author: Shell SDU Team
Version: 1.0
***********************************************************************************
Master script, responsible for deploying and configuring SPOCPI related resources. Below are the scripts invoked and their outcome after execution

1. codepublish.ps1        - Builds the solution (if src folder is present and SDU solution is in file system) and publish the output as zip file in deploy folder
2. azuredeploy.ps1        - Responsible for creating app services in the resource group
3. codedeploy.ps1         - Deploys the zip files generated in step 1 to the app services   
4. configurekeyvault.ps1  - Creates secret for every function created and also adds the app services to Access Policies
5. storagedeploy.ps1      - Configures the storage account by creating required tables, configuration data and also blob containers
6. sanitizeappsettings.ps1 - Sanitize app settings - Replace dummy values in appsettings folder with actual values
7. createappsettings.ps1  - Creates app settings for all the app services
8. configuresearch.ps1    - Creates indexes, indexers, data sources in search service
9. restartservices.ps1    - Restart app services
***********************************************************************************
#>

# Invoke Powershell script 
function Invoke-PowershellScript {
    Param([string]$stepNumber, [string]$scriptPath, [string]$scriptFile)  
  
    Write-Host "***********************************************************************************" -ForegroundColor Green
    Write-Host "Step $stepNumber - Started the script execution: $scriptFile" -ForegroundColor Yellow
    Set-Location -Path $scriptPath
    Invoke-Expression $scriptFile
    Write-Host "Step $stepNumber - Completed the script execution: $scriptFile" -ForegroundColor Green
    Write-Host "***********************************************************************************" -ForegroundColor Green
  }
  
  # Invoke script in sequential fashion
  $timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
  $transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\spocpimaster_{0}.txt" -f $timeStamp)
  $scriptExecutionPath = (Get-Location).Path
  Start-Transcript -Path $transcriptfile -IncludeInvocationHeader
  
  Invoke-PowershellScript "1" $scriptExecutionPath ".\codepublish.ps1"
  Invoke-PowershellScript "2" $scriptExecutionPath ".\azuredeploy.ps1"
  Invoke-PowershellScript "3" $scriptExecutionPath ".\codedeploy.ps1"
  Invoke-PowershellScript "4" $scriptExecutionPath ".\configurekeyvault.ps1"
  Invoke-PowershellScript "5" $scriptExecutionPath ".\storagedeploy.ps1"
  Invoke-PowershellScript "6" $scriptExecutionPath ".\sanitizeappsettings.ps1"
  Invoke-PowershellScript "7" $scriptExecutionPath ".\createappsettings.ps1"
  Invoke-PowershellScript "8" $scriptExecutionPath ".\configuresearch.ps1"
  Invoke-PowershellScript "9" $scriptExecutionPath ".\restartservices.ps1"
  
  Stop-Transcript
  
  