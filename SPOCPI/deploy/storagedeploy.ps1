<# Global Variables Declaration #>
$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\storagedeploy_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$folderpath = (Get-Item -Path "..\deploy").FullName
$parameterFilePath = $folderpath + "\azuredeploy.parameters.json"
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1

#Load Common Helpers 
Import-Module .\commonhelpers.psm1

#Reading Json Parameter file
$webHooksStorageAccountName = $parametersJson.parameters.environment.value + $parametersJson.parameters.webHooksStorage.value

#Create storage tables in Web Hooks 
$webHooksTables = $parametersJson.parameters.webHooksStorageTableNames.value
foreach ($storageTableName in $webHooksTables) {
    az storage table create --name $storageTableName --account-name $webHooksStorageAccountName
    Write-Host "Storage Table"$storageTableName" created successfully" -ForegroundColor Green
}

$docTrackStorageAccountName = $parametersJson.parameters.environment.value + $parametersJson.parameters.docTrackStorage.value

#Create storage tables in Doc Track
$docTrackTables = $parametersJson.parameters.docTrackStorageTableNames.value
foreach ($storageTableName in $docTrackTables) {
    az storage table create --name $storageTableName --account-name $docTrackStorageAccountName
    Write-Host "Storage Table"$storageTableName" created successfully" -ForegroundColor Green
}

$analyticsStorageAccountName = $parametersJson.parameters.environment.value + $parametersJson.parameters.analyticsStorage.value

#Create storage tables in Analytics storage
$analyticsStorageTables = $parametersJson.parameters.analyticsStorageTableNames.value
foreach ($storageTableName in $analyticsStorageTables) {
    az storage table create --name $storageTableName --account-name $analyticsStorageAccountName
    Write-Host "Storage Table"$storageTableName" created successfully" -ForegroundColor Green
}

#Dump the Configuration data into Configuration table
Import-Csv "tables/Configuration.typed.csv" |`
  ForEach-Object {
  try { 
    $partitionKey = $_.PartitionKey;
    $rowKey = $_.RowKey;
    $configKey = $_.ConfigKey;
    $configValue = $_.ConfigValue;
    $restricted = $_.Restricted;
    Add-ConfigurationEntity $partitionKey $rowKey $configKey $configValue $restricted $webHooksStorageAccountName
  }
  catch {
    Write-Host ("Error occurred while creating the configuration key -  $_.RowKey.Message: $_.Exception.Message") -ForegroundColor Red
  }
}

$appInsightsName = $parametersJson.parameters.environment.value + $parametersJson.appInsightName.value
$appInsightsKey = az resource show -g $parametersJson.parameters.resourceGroupName.value -n $appInsightsName --resource-type "Microsoft.Insights/components" --query "properties.InstrumentationKey" -o tsv

$spocpikeyvaultName = $parametersJson.parameters.environment.value + $parametersJson.parameters.keyVaultName.value
$notificationReceiverFunctionName = $parametersJson.parameters.environment.value + $parametersJson.parameters.notificationReceiverFunctionApp.value
$notificationReceiverFunctionKey = Get-FunctionKey $parametersJson.parameters.subscriptionId.value $parametersJson.parameters.resourceGroupName.value $notificationReceiverFunctionName "NotificationReceiver"

$appClientId = "https://$spocpikeyvaultName.vault.azure.net/secrets/AADAppClientId"
$appClientSecret = "https://$spocpikeyvaultName.vault.azure.net/secrets/AADAppClientSecret"
$cpDriveDeltaStoreConnString = "https://$spocpikeyvaultName.vault.azure.net/secrets/DocTrackStoreConnectionString"
$cpServiceBusConnectionString = "https://$spocpikeyvaultName.vault.azure.net/secrets/ServiceBusConnectionString"
$redisConnectionString = "https://$spocpikeyvaultName.vault.azure.net/secrets/RedisConnectionString"
$searchServiceName = $parametersJson.parameters.environment.value + $parametersJson.parameters.searchName.value
$searchUpdateKey = "https://$spocpikeyvaultName.vault.azure.net/secrets/SearchUpdateKey"
$webHooksStoreConnectionString = "https://$spocpikeyvaultName.vault.azure.net/secrets/WebHooksStoreConnectionString"
$webHookNotificationUrl = "https://$notificationReceiverFunctionName.azurewebsites.net/api/NotificationReceiver?code=$notificationReceiverFunctionKey"

# Add dynamic configuration entities
Add-ConfigurationEntity $partitionKey "AppInsightsInstrumentationKey" "AppInsightsInstrumentationKey" $appInsightsKey $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "AADAppClientId" "AADAppClientId" $appClientId $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "AADAppClientSecret" "AADAppClientSecret" $appClientSecret $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "CPDriveDeltaStoreConnString" "CPDriveDeltaStoreConnString" $cpDriveDeltaStoreConnString $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "CPServiceBusConnectionString" "CPServiceBusConnectionString" $cpServiceBusConnectionString $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "RedisConnectionString" "RedisConnectionString" $redisConnectionString $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "SearchServiceName" "SearchServiceName" $searchServiceName $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "SearchUpdateKey" "SearchUpdateKey" $searchUpdateKey $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "TenantId" "TenantId" $parametersJson.parameters.azureTenantId.value $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "TenantName" "TenantName" $parametersJson.parameters.tenantName.value $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "WebHooksStoreConnectionString" "WebHooksStoreConnectionString" $webHooksStoreConnectionString $true $webHooksStorageAccountName
Add-ConfigurationEntity $partitionKey "WebhookNotificationUrl" "WebhookNotificationUrl" $webHookNotificationUrl $true $webHooksStorageAccountName