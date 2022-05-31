# Start - Global Variables Declaration

$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$random = Get-Random -Maximum 1000
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\azuredeploy_{0}.txt" -f $timeStamp)
Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$deploymentName = "shellscript" + $random
$folderpath = (Get-Item -Path "..\deploy").FullName
$templateFilePath = $folderpath + "\azuredeploy.json"
$parameterFilePath = $folderpath + "\azuredeploy.parameters.json"

$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json
$environment = $parametersJson.parameters.environment.value

# End - Global Variables Declaration

Write-Host ("Template File Path: " + $templateFilePath) -ForegroundColor DarkYellow
Write-Host ("Parameter File Path: " + $parameterFilePath) -ForegroundColor DarkYellow

#Login and select subscription
Invoke-Expression .\azlogin.ps1

$resourceGroupExists = az group exists -n $parametersJson.parameters.resourceGroupName.value
if($resourceGroupExists -eq $FALSE){
  az group create --name $parametersJson.parameters.resourceGroupName.value --location $parametersJson.parameters.location.value
}

az deployment group create --resource-group $parametersJson.parameters.resourceGroupName.value --template-file ./azuredeploy.json --parameters ./azuredeploy.parameters.json

Stop-Transcript