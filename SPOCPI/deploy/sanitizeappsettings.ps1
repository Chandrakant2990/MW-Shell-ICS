<# Global Variables Declaration #>
$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\sanitizeappsettings_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$parameterFilePath = ".\azuredeploy.parameters.json"

#Reading Json Parameter file
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1

#Load Common Helpers 
Import-Module .\commonhelpers.psm1

Get-ChildItem "./appsettings/" -Filter *.json | 
Foreach-Object {
    $appSettingsJson = Get-Content $_.FullName -Raw
    $fileName = $_.FullName

    Write-Host ("====================================================================")
    Write-Host ("Sanitizing app settings:$fileName") -Fore Yellow

    $keyCount = ($appSettingsJson | ConvertFrom-Json).Count
    $appSettingsArray = $appSettingsJson | ConvertFrom-Json
    $appSettingsFinalArray = $appSettingsArray
    $keyvaultName = $parametersJson.parameters.environment.value + $parametersJson.parameters.keyVaultName.value
    for ($i = 0; $i -lt $keyCount; $i++) {
        If ($appSettingsArray[$i].name -eq "APPINSIGHTS_INSTRUMENTATIONKEY") {
            $appInsightsName = $parametersJson.parameters.environment.value + $parametersJson.parameters.appInsightName.value
            $appInsightsKey = az resource show -g $parametersJson.parameters.resourceGroupName.value -n $appInsightsName --resource-type "Microsoft.Insights/components" --query "properties.InstrumentationKey" -o tsv
            $appSettingsFinalArray[$i].value = $appInsightsKey;
        }
        ElseIf ($appSettingsArray[$i].name -eq "ANCM_ADDITIONAL_ERROR_PAGE_LINK") {
            $adminUiName = $parametersJson.parameters.environment.value + $parametersJson.parameters.adminUIWebAppName.value;
            $appSettingsFinalArray[$i].value = "https://$adminUiName.scm.azurewebsites.net/detectors";
        }
        ElseIf ($appSettingsArray[$i].name -eq "ConnectionStrings:ConfigurationConnectionString") {
            $appSettingsFinalArray[$i].value = "@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/WebHooksStoreConnectionString/)"
        }
        ElseIf ($appSettingsArray[$i].name -eq "AzureWebJobsStorage") 
        {
            If($fileName -contains "spocpiwebhooksmanagerapp") {
                $appSettingsFinalArray[$i].value = "@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/WebHooksStoreConnectionString/)"
            } Else {
                $appSettingsFinalArray[$i].value = "@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/DocTrackStoreConnectionString/)"
            }    
        }
        ElseIf ($appSettingsArray[$i].name -eq "DocTrackConnectionString") {
            $appSettingsFinalArray[$i].value = "@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/ServiceBusConnectionString/)"
        }
        ElseIf ($appSettingsArray[$i].name -eq "ConnectionStrings:RedisConnectionString") {
            $appSettingsFinalArray[$i].value = "@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/RedisConnectionString/)"
        }
        ElseIf ($appSettingsArray[$i].name -eq "CPServiceBusConnectionString" -or $appSettingsArray[$i].name -eq "ServiceBusConnection") {
            $appSettingsFinalArray[$i].value = "@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/ServiceBusConnectionString/)"
        }
        ElseIf ($appSettingsArray[$i].name -eq "WEBSITE_HTTPLOGGING_CONTAINER_URL" -or 
                $appSettingsArray[$i].name -eq "DIAGNOSTICS_AZUREBLOBCONTAINERSASURL")   {
            $appSettingsFinalArray[$i].value = ""
        }
		ElseIf ($appSettingsArray[$i].name -eq "AzureAd:Domain")   {
            $appSettingsFinalArray[$i].value = $parametersJson.parameters.tenantName.value
        } 
		ElseIf ($appSettingsArray[$i].name -eq "AzureAd:TenantId")   {
            $appSettingsFinalArray[$i].value = $parametersJson.parameters.azureTenantId.value
        }		
    }
   
    $appSettingsFinalArray | ConvertTo-Json | Set-Content -Path $_.FullName  
    
    Write-Host ("Sanitized appsettings:$fileName") -ForegroundColor Green
    Write-Host ("====================================================================")     
}