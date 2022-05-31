# Global Variables Declaration
$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\configurekeyvault_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$folderpath = (Get-Item -Path "..\deploy").FullName
$parameterFilePath = $folderpath + "\azuredeploy.parameters.json"

#Reading Json Parameter file
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1

#Load Common Helpers 
Import-Module .\commonhelpers.psm1

$keyVaultName = $parametersJson.parameters.environment.value + $parametersJson.parameters.keyVaultName.value

#Add Permissions for az Cli in KeyVault 
Write-Host "=============================================================="
Write-Host "Adding az Cli required permissions to KeyVault" -ForegroundColor Yellow
Write-Host "=============================================================="

az keyvault set-policy --name $keyVaultName --object-id 04b07795-8ddb-461a-bbee-02f9e1bf7b46 --certificate-permissions get list create delete --key-permissions get list create delete --secret-permissions get set list delete

$currentUserId = ((az ad signed-in-user show) | ConvertFrom-Json).objectId
az keyvault set-policy --name $keyVaultName --object-id $currentUserId --certificate-permissions get list create delete --key-permissions get list create delete --secret-permissions get set list delete

Write-Host "=============================================================="
Write-Host "Added az Cli required permissions to KeyVault" -ForegroundColor Green
Write-Host "=============================================================="

$resourceGroupName = $parametersJson.parameters.resourceGroupName.value
$environmentName = $parametersJson.parameters.environment.value
$secretExpiryDate = ((get-date).ToUniversalTime().AddYears(2)).ToString("yyyy-MM-ddTHH:mm:ssZ")

Add-KeyVaultSecret "AADLoginAppClientId" $parametersJson.parameters.AADLoginAppClientId.value $keyVaultName $secretExpiryDate
Add-KeyVaultSecret "AADLoginAppClientSecret" $parametersJson.parameters.AADLoginAppClientSecret.value $keyVaultName $secretExpiryDate

$searchServiceName = $parametersJson.parameters.environment.value + $parametersJson.parameters.searchName.value
$searchAdminPrimaryKey = Get-SearchServicePrimaryKey $searchServiceName $resourceGroupName
Add-KeyVaultSecret "SearchUpdateKey" $searchAdminPrimaryKey $keyVaultName $secretExpiryDate

$webHooksStoreName = $parametersJson.parameters.environment.value + $parametersJson.parameters.webHooksStorage.value
$webHooksConnectionString = Get-StorageConnectionString $webHooksStoreName $resourceGroupName
Add-KeyVaultSecret "WebHooksStoreConnectionString" $webHooksConnectionString $keyVaultName $secretExpiryDate
  
$docTrackStoreName = $parametersJson.parameters.environment.value + $parametersJson.parameters.docTrackStorage.value
$docTrackConnectionString = Get-StorageConnectionString $docTrackStoreName $resourceGroupName
Add-KeyVaultSecret "DocTrackStoreConnectionString" $docTrackConnectionString $keyVaultName $secretExpiryDate

$redisCacheName = $parametersJson.parameters.environment.value + $parametersJson.parameters.redisCacheName.value
$redisConnectionString = Get-RedisConnectionString $redisCacheName $resourceGroupName
Add-KeyVaultSecret "RedisConnectionString" $redisConnectionString $keyVaultName $secretExpiryDate

$serviceBusNamespace = $parametersJson.parameters.environment.value + $parametersJson.parameters.serviceBusNamespaceName.value
$serviceBusConnectionString = Get-ServiceBusConnectionString $serviceBusNamespace "RootManageSharedAccessKey" $resourceGroupName
Add-KeyVaultSecret "ServiceBusConnectionString" $serviceBusConnectionString $keyVaultName $secretExpiryDate

Import-Csv "keyvault/keyvaultappprincipals.csv" |`
    ForEach-Object {
    try {
        $functionAppName = $environmentName + $_.AppName
        Write-Host "=============================================================="
        Write-Host ("Creating System Assigned managed identity to the Function App: " + $functionAppName ) -ForegroundColor Yellow
        az functionapp identity assign -n $functionAppName -g $resourceGroupName
        Write-Host ("Created System Assigned managed identity to the Function App: " + $_.AppName ) -ForegroundColor Green
        Write-Host "=============================================================="

        Write-Host "=============================================================="
        Write-Host ("Adding app principal to the Keyvault : " + $functionAppName ) -ForegroundColor Yellow
        $principalId = az functionapp identity show -n $functionAppName -g $resourceGroupName --query principalId 
        az keyvault set-policy -n $keyvaultName -g $resourceGroupName --object-id $principalId --secret-permissions get
        Write-Host ("Added app principal to the Keyvault : " + $functionAppName ) -ForegroundColor Green
        Write-Host "=============================================================="
    }
    catch {
        Write-Host ("Error occurred while adding secrets to keyvault " + $keyVaultName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
    }
}

Write-Host ("Completed Keyvault configuration.") -ForegroundColor Green
