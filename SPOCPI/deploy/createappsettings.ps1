<# Global Variables Declaration #>
$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\createappsettings_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$folderpath = (Get-Item -Path "..\deploy").FullName
$parameterFilePath = $folderpath + "\azuredeploy.parameters.json"

#Reading Json Parameter file
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1
$resourceGroupName = $parametersJson.parameters.resourceGroupName.value
$keyvaultName = $parametersJson.parameters.environment.value + $parametersJson.parameters.keyVaultName.value

Import-Csv "services.csv" |`
    ForEach-Object {
    try {
        $appSettingsFilePath = "appsettings\" + $_.AppName + ".appsettings.json"
        if ((Test-Path -Path $appSettingsFilePath) -eq $true) {
            $appSettingsFileName = "@" + $appSettingsFilePath
			$appServiceName = $parametersJson.parameters.environment.value+$_.AppName
			
            if ($_.Type -eq "FunctionApp") {
                Write-Host ("Creating appsettings for the azure function app: $appServiceName") -ForegroundColor Yellow
                Write-Host ("====================================================================")
                az functionapp config appsettings set --name $appServiceName --resource-group $parametersJson.parameters.resourceGroupName.value --settings $appSettingsFileName
                Write-Host ("Created appsettings for the azure function app: $appServiceName") -ForegroundColor Green
                Write-Host ("====================================================================")
            }
            elseif ($_.Type -eq "WebApp" -or $_.Type -eq "WebJob") {
                Write-Host ("====================================================================")
                Write-Host ("Creating appsettings for the azure web app: $appServiceName") -ForegroundColor Yellow
                az webapp config appsettings set --name $appServiceName --resource-group $parametersJson.parameters.resourceGroupName.value --settings $appSettingsFileName
                Write-Host ("Created appsettings for the azure web app: $appServiceName") -ForegroundColor Green
                Write-Host ("====================================================================")
            }

            If($_.AppName -eq $parametersJson.parameters.webHooksManagerWebApp.value) {
                az webapp config connection-string set --connection-string-type "Custom" --name $appServiceName --resource-group $resourceGroupName --settings AzureWebJobsDashboard="@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/WebHooksStoreConnectionString/)"  
                az webapp config connection-string set --connection-string-type "Custom" --name $appServiceName --resource-group $resourceGroupName --settings AzureWebJobsStorage="@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/WebHooksStoreConnectionString/)"
            } 
            ElseIf($_.AppName -eq $parametersJson.parameters.changeprocessorWebApp.value) {
                az webapp config connection-string set --connection-string-type "Custom" --name $appServiceName --resource-group $resourceGroupName --settings AzureWebJobsDashboard="@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/DocTrackStoreConnectionString/)"  
                az webapp config connection-string set --connection-string-type "Custom" --name $appServiceName --resource-group $resourceGroupName --settings AzureWebJobsStorage="@Microsoft.KeyVault(SecretUri=https://$keyvaultName.vault.azure.net/secrets/DocTrackStoreConnectionString/)"
            }
        }        
    }
    catch {
        Write-Host ("Error occurred while deploying $_.Type: $appServiceName. Message :" + $_.Exception.Message) -ForegroundColor Red
    }
}


