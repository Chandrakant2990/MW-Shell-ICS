# Login
try {
    $Subscriptions = az account list --output table --all
    if (!($Subscriptions)) {
      az account clear
      az login
    }
  }
  catch {
    az account clear
    az login
  }
  
  if (!($Subscriptions)) {
    Write-Host "Login failed or there are no subscriptions available with your account." -ForegroundColor Red
    exit
  }
  
  $folderpath = (Get-Item -Path "..\deploy").FullName
  $parameterFilePath = $folderpath + "\azuredeploy.parameters.json"

  #Read Json Parameter file
  $parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

  $subscriptionId = $parametersJson.parameters.subscriptionId.value
  $subscription = az account set --subscription $subscriptionId
  
  Write-Host ("Completed selecting Azure subscription using 'AzureSubscriptionId' = " + $subscriptionId) -ForegroundColor DarkYellow
  Write-Host ("====================================================================")
  
  az account set --subscription $subscriptionId
  
  $resourceGroupExists = az group exists -n $parametersJson.parameters.resourceGroupName.value
  if($resourceGroupExists -eq $FALSE) {
    Write-Host ("Resource group " + $parametersJson.parameters.resourceGroupName.value + " does not exist. Terminating the script.") -ForegroundColor Red
    return;
  }