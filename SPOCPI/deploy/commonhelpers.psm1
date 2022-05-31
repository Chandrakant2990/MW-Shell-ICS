#Get Function Host Key
function Get-FunctionHostKey {
    Param([string]$subscriptionId, [string]$resourceGroupName, [string]$functionAppName) 
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Web/sites/$functionAppName"
    try {
        return (az rest --method post --uri "https://management.azure.com$resourceId/host/default/listKeys?api-version=2018-11-01" | ConvertFrom-Json).functionKeys.default
    }
    catch {
        Write-Host ("Error occurred while retrieving Key for the webapp/function:$functionAppName. Message :$_.Exception.Message") -ForegroundColor Red
    } 
}

# Get Function Key
function Get-FunctionKey {
    Param([string]$subscriptionId, [string]$resourceGroupName, [string]$functionAppName, [string]$functionName)  
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Web/sites/$functionAppName"
    try {
        return (az rest --method post --uri "https://management.azure.com$resourceId/functions/$functionName/listKeys?api-version=2018-02-01" | ConvertFrom-Json).default
    }
    catch {
        Write-Host ("Error occurred while retrieving Key for the function:$functionName. Message :$_.Exception.Message") -ForegroundColor Red
    }
}

# Get Cognitive Service Public Key
function Get-CognitiveServiceKey {
    Param([string]$serviceName, [string]$resourceGroupName)  
    try {
        return (az cognitiveservices account keys list --name $serviceName --resource-group $resourceGroupName | ConvertFrom-Json).key1
    }
    catch {
        Write-Host ("Error occurred while retrieving PublicKey for the service:" + $serviceName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
        return "";
    }
}

# Get Storage Account Connection String
function Get-StorageConnectionString {
    Param([string]$storageAccountName, [string]$resourceGroupName)  
    try {
        return  (az storage account show-connection-string --name $storageAccountName --resource-group $resourceGroupName | ConvertFrom-Json).connectionString
    }
    catch {
        Write-Host ("Error occurred while retrieving connectionstring for the storage account:" + $storageAccountName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
        return "";
    }
}

# Get Storage Account Key
function Get-StorageKey {
    Param([string]$storageAccountName, [string]$resourceGroupName)
    try {
        return  (az storage account keys list -g $resourceGroupName -n $storageAccountName | ConvertFrom-Json)[0].value 
    }
    catch {
        Write-Host ("Error occurred while retrieving key for the storage account:" + $storageAccountName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
        return "";
    }
}

# Get Redis Cache Connection String
function Get-RedisConnectionString {
    Param([string]$redisName, [string]$resourceGroupName)
    try {
        $primaryKey = (az redis list-keys --name $redisName --resource-group $resourceGroupName | ConvertFrom-Json).primaryKey 
        return "$redisName.redis.cache.windows.net:6380,password=$primaryKey,ssl=True,abortConnect=False,ConnectRetry=3,ConnectTimeout=5000,SyncTimeout=5000";
    }
    catch {
        Write-Host ("Error occurred while retrieving key for the redis cache:" + $redisName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
        return "";
    }
}

# Get Service Bus Namespace Connection String
function Get-ServiceBusConnectionString {
    Param([string]$namespaceName, [string]$sharedAccessKeyName, [string]$resourceGroupName)
    try {
        return $(az servicebus namespace authorization-rule keys list --resource-group $resourceGroupName --namespace-name $namespaceName --name $sharedAccessKeyName --query primaryConnectionString --output tsv);
    }
    catch {
        Write-Host ("Error occurred while retrieving connectionstring for the servicebus namespace:" + $namespaceName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
        return "";
    }
}

function Get-SearchServicePrimaryKey {
    Param([string]$searchServiceName, [string]$resourceGroupName)
    try {
        $searchAdminKeys = az search admin-key show --resource-group $resourceGroupName --service-name $searchServiceName --only-show-errors
        return ($searchAdminKeys | ConvertFrom-Json).primaryKey
    }
    catch {
        Write-Host ("Error occurred while retrieving primary key for search service - $searchServiceName. Message - $_.Exception.Message") -ForegroundColor Red
        return "";
    }
}

# Add Configuration Entity
function Add-ConfigurationEntity {
    Param([string]$partitionKey, [string]$rowKey, [string]$configKey, [string]$configValue, [string]$isRestricted, [string]$storageAccountName)  

    Write-Host "=============================================================="
    Write-Host "Adding Configuration Entity with ConfigKey - $configKey" -ForegroundColor Yellow
  
    if ($isRestricted -eq $true -Or $isRestricted -eq $false) {
        az storage entity insert --entity PartitionKey=$partitionKey RowKey=$rowKey ConfigKey=$configKey ConfigValue=$configValue ConfigValue@odata.type=Edm.String Restricted=$isRestricted Restricted@odata.type=Edm.Boolean --table-name "Configuration" --account-name $storageAccountName
    }
    else {
        az storage entity insert --entity PartitionKey=$partitionKey RowKey=$rowKey ConfigKey=$configKey ConfigValue=$configValue ConfigValue@odata.type=Edm.String Restricted=$false Restricted@odata.type=Edm.Boolean --table-name "Configuration" --account-name $storageAccountName
    }

    Write-Host "Added Configuration Entity with ConfigKey - $configKey" -ForegroundColor Green
    Write-Host "=============================================================="
}

# Add Key Vault Secret 
function Add-KeyVaultSecret {
    Param([string]$keyName, [string]$secret, [string]$keyVaultName, [string]$expiryDate)
    Write-Host "==============================================================" 
    Write-Host ("Adding Secret to the Keyvault - $keyName") -ForegroundColor Yellow

    az keyvault secret set --name $keyName --value $secret --vault-name $keyVaultName
	az keyvault secret set-attributes --vault-name $keyVaultName --name $keyName --expires $expiryDate

    Write-Host ("Added Secret to the Keyvault - $keyName") -ForegroundColor Green
    Write-Host "=============================================================="
}

Export-ModuleMember -Function 'Get-FunctionHostKey'
Export-ModuleMember -Function 'Get-FunctionKey'
Export-ModuleMember -Function 'Get-CognitiveServiceKey'
Export-ModuleMember -Function 'Get-StorageConnectionString'
Export-ModuleMember -Function 'Get-StorageKey'
Export-ModuleMember -Function 'Get-RedisConnectionString'
Export-ModuleMember -Function 'Get-ServiceBusConnectionString'
Export-ModuleMember -Function 'Get-SearchServicePrimaryKey'
Export-ModuleMember -Function 'Add-ConfigurationEntity'
Export-ModuleMember -Function 'Add-KeyVaultSecret'