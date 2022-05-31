<# Global Variables Declaration #>
$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\exportappsettings_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$parameterFilePath = ".\azuredeploy.parameters.json"

#Reading Json Parameter file
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1

#Load Common Helpers 
Import-Module .\commonhelpers.psm1

Import-Csv "services.csv" | Sort-Object AppName -Unique |`
    ForEach-Object {
	$appserviceName = $parametersJson.parameters.environment.value+$_.AppName
    try {
        $appSettingsFilePath = "appsettings\" + $_.AppName + ".appsettings.json"
        Write-Host ("====================================================================")
        Write-Host ("Exporting app settings for app:$appserviceName") -Fore Yellow
        $appSettings = az webapp config appsettings list --name $appserviceName --resource-group $parametersJson.parameters.resourceGroupName.value
        $appSettings | Set-Content -Path $appSettingsFilePath         
        Write-Host ("Exported app settings for app:$appserviceName") -ForegroundColor Green
        Write-Host ("====================================================================")
    }
    catch {
        Write-Host ("Error occurred while exporting app settings for app:" + $appserviceName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
    }
}