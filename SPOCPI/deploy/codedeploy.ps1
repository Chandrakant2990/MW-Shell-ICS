<# ------------------------------------------------------------------------------------------------------------------
Function to deploy an azure function app as well as configure application insight
$publishZip : Zipped folder of the solution to be deployed
$resourceGroupName : Name of the resource group
$functionAppName
$appType : Type of app deployment
------------------------------------------------------------------------------------------------------------------ #>
function DeployApp($publishZip, $resourceGroupName, $appName, $appType) {
  try {
    if ($appType -eq "FunctionApp") {
      Write-Host ("Deploying the azure function app : " + $appName) -ForegroundColor DarkYellow
      Write-Host ("====================================================================")
      az functionapp deployment source config-zip -g $resourceGroupName -n $appName --src $publishZip
    }
    if ($appType -eq "WebApp") {
      Write-Host ("Deploying the azure web app : " + $appName) -ForegroundColor DarkYellow
      Write-Host ("====================================================================")
      az webapp deployment source config-zip -g $resourceGroupName -n $appName --src $publishZip
    }
  }
  catch {
    Write-Host ("Error occurred while deploying the azure function/web app : " + $appName + " Message : " + $_.Exception.Message) -ForegroundColor Red
  }

  Write-Host ("Deployment Successful") -ForegroundColor DarkYellow
}

<# ------------------------------------------------------------------------------------------------------------------
Function to get Deployed function credentials from Publishin Profile
$appName : Name of the azure function app
$resourceGroup : Resource Group Name
------------------------------------------------------------------------------------------------------------------ #>
function getKuduCreds($appName, $resourceGroupName) {
  Write-Host "Getting Publish credentials" -ForegroundColor DarkYellow

  $user = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroupName `
    --query "[?publishMethod=='MSDeploy'].userName" -o tsv

  $pass = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroupName `
    --query "[?publishMethod=='MSDeploy'].userPWD" -o tsv

  $pair = "$($user):$($pass)"

  $encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))

  return $encodedCreds
}

<# ------------------------------------------------------------------------------------------------------------------
Function to get Deployed function key
$appName : Name of the azure web app
$webjobName : Web Job Name
$encodedCreds : Cedentials to get the token
$publishZip: Zipped folder of the solution to be deployed
------------------------------------------------------------------------------------------------------------------ #>
function webjobDeployment([string]$appName, [string]$webjobName, [string]$encodedCreds, [string]$publishZip) {
  Write-Host ("Deploying the webjob : " + $webjobName + " for Web App: " + $appName) -ForegroundColor DarkYellow
  Write-Host ("====================================================================")
  try {
    $ZipHeaders = @{
      Authorization         = "Basic {0}" -f $encodedCreds
      "Content-Disposition" = "attachment; filename=run.cmd"
    }

    # upload the job using the Kudu WebJobs API
    Invoke-WebRequest -Uri https://$appName.scm.azurewebsites.net/api/continuouswebjobs/$webjobName -Headers $ZipHeaders `
      -InFile $publishZip -ContentType "application/zip" -Method Put
     
    Write-Host ("Webjob deployed in Ready state") -ForegroundColor DarkYellow
    az webapp webjob continuous start --webjob-name $webjobName --name $appName --resource-group $parametersJson.parameters.resourceGroupName.value

    Write-Host ("Web Job now started in continous mode") -ForegroundColor DarkYellow
    Write-Host ("====================================================================")
  }
  catch {
    Write-Host ("Error occurred while deploying the web job : " + $webjobName + " Message : " + $_.Exception.Message) -ForegroundColor Red
  }
}

<# ------------------------------------------------- Function Block End ------------------------------------------------- #>

#cd "SPOCPI\deploy\"  

<# Global Variables Declaration #>
$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\codedeploy_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$folderpath = (Get-Item -Path "..\deploy").FullName
$parameterFilePath = $folderpath + "\azuredeploy.parameters.json"

#Reading Json Parameter file
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1

Import-Csv "services.csv" |`
    ForEach-Object {
      try {
		$appServiceName = $parametersJson.parameters.environment.value+$_.AppName
        if($_.Type -eq "FunctionApp") {
          Write-Host ("Deploying the azure function app: $appServiceName") -ForegroundColor Green
          Write-Host ("====================================================================")
          
          az functionapp deployment source config-zip -g $parametersJson.parameters.resourceGroupName.value -n $appServiceName --src $_.SourceFile
          
          Write-Host ("Deployed the azure function app: $appServiceName") -ForegroundColor Green
          Write-Host ("====================================================================")
        }
        elseif($_.Type -eq "WebApp") {
          Write-Host ("Deploying the azure web app: $appServiceName") -ForegroundColor Green
          Write-Host ("====================================================================")
          az webapp deployment source config-zip -g $parametersJson.parameters.resourceGroupName.value -n $appServiceName --src $_.SourceFile
          
          Write-Host ("Deployed the azure web app: $appServiceName") -ForegroundColor Green
          Write-Host ("====================================================================")
        }
        elseif($_.Type -eq "WebJob") {
          Write-Host ("Deploying the webjob : " + $_.JobName) -ForegroundColor Green
          Write-Host ("====================================================================")

          $kuduCredentials = getKuduCreds $appServiceName $parametersJson.parameters.resourceGroupName.value 
          webjobDeployment $appServiceName $_.JobName $kuduCredentials $_.SourceFile
          
          Write-Host ("Deployed the webjob : " + $_.JobName) -ForegroundColor Green
          Write-Host ("====================================================================")
        }
      }
      catch {
        Write-Host ("Error occurred while deploying "+$_.Type+": " + $appServiceName + ". Message :" + $_.Exception.Message) -ForegroundColor Red
      }
    }

Stop-Transcript 