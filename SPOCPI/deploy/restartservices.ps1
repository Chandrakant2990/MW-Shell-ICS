<# Global Variables Declaration #>

$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\restartservices_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$folderpath = (Get-Item -Path "..\deploy").FullName
$parameterFilePath = $folderpath + "\azuredeploy.parameters.json"

#Reading Json Parameter file
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1

Import-Csv "services.csv" |`
    ForEach-Object {
    $appServiceName = $parametersJson.parameters.environment.value + $_.AppName
    try {
            if ($_.Type -eq "FunctionApp") {
                Write-Host "=============================================================="
                Write-Host ("Restarting azure function app:" + $appServiceName) -ForegroundColor Yellow
				az functionapp restart --name $appServiceName --resource-group $parametersJson.parameters.resourceGroupName.value
                Write-Host ("Restarted azure function app:" + $appServiceName) -ForegroundColor Green
				Write-Host "=============================================================="
            }
            elseif ($_.Type -eq "WebApp" -or $_.Type -eq "WebJob") {
                Write-Host "=============================================================="
                Write-Host ("Restarting webapp:"+ $appServiceName) -ForegroundColor Yellow
				az webapp restart --name $appServiceName --resource-group $parametersJson.parameters.resourceGroupName.value
                Write-Host ("Restarted webapp:"+ $appServiceName) -ForegroundColor Green
				Write-Host "=============================================================="
            }
    }
    catch {
        Write-Host ("Error occurred while restarting "+ $appServiceName + "Message:"+ $_.Exception.Message) -ForegroundColor Red
    }
}