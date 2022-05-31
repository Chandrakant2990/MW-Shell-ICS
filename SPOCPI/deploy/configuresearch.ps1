<# Global Variables Declaration #>
$timeStamp = Get-Date -Format "ddmmyyyyhhmmss"
$transcriptfile = ((Get-Item -Path ".\" -Verbose).FullName + "\configuresearch_{0}.txt" -f $timeStamp)

Start-Transcript -Path $transcriptfile -IncludeInvocationHeader

$folderpath = (Get-Item -Path "..\deploy").FullName
$parameterFilePath = $folderpath + "\azuredeploy.parameters.json"

#Reading Json Parameter file
$parametersJson = Get-Content -Raw -Path $parameterFilePath | ConvertFrom-Json

#Login and select subscription
Invoke-Expression .\azlogin.ps1

$docTracStorageAccountName = $parametersJson.parameters.environment.value + $parametersJson.parameters.docTrackStorage.value
$webHooksStorageAccountName = $parametersJson.parameters.environment.value + $parametersJson.parameters.webHooksStorage.value
$analyticsStorageAccountName = $parametersJson.parameters.environment.value + $parametersJson.parameters.analyticsStorage.value

$searchServiceName = $parametersJson.parameters.environment.value + $parametersJson.parameters.searchName.value
$searchAdminKeys = az search admin-key show --resource-group $parametersJson.parameters.resourceGroupName.value --service-name $searchServiceName
$searchAdminPrimaryKey = ($searchAdminKeys | ConvertFrom-Json).primaryKey

Write-Host "Adding Dummy data into Storage Tables" -ForegroundColor Yellow
Write-Host "====================================================" -ForegroundColor Green

#Add Dummy data into tables
#documenttrackingtable
az storage entity insert --entity PartitionKey="spocpi" RowKey="dummy" DocumentCTag="" DocumentCTagChange="false" DocumentCTagChange@odata.type=Edm.Boolean DocumentETag="" DocumentETagChange="false" DocumentETagChange@odata.type=Edm.Boolean DriveId="" Extension="" IsDeleted="false" IsDeleted@odata.type=Edm.Boolean IsFolder="false" IsFolder@odata.type=Edm.Boolean FileSize="1" FileSize@odata.type=Edm.Int64 ListId="" ListItemId="" ListItemUniqueId="" Name="" SiteId="" SiteUrl="" Timestamp="" WebId="" WebUrl="" --table-name "DocumentTracking" --account-name $docTracStorageAccountName

#subscriptions table
az storage entity insert --entity PartitionKey="spocpi" RowKey="dummy" CreationDateTime="2019-09-17T10:08:09.415Z" CreationDateTime@odata.type=Edm.DateTime Description="" DriveId="" ExpirationDateTime="2019-09-17T10:08:09.415Z" ExpirationDateTime@odata.type=Edm.DateTime IsActive="True" IsActive@odata.type=Edm.Boolean LibraryUrl="" OutputQueue="" SiteId="" SiteUrl="" Status="" SubscriptionId="" Timestamp="" Error="" Parameters="" AutoIndex="false" AutoIndex@odata.type=Edm.Boolean --table-name "Subscriptions" --account-name $webHooksStorageAccountName

#azurecommonmetrics table
az storage entity insert --entity PartitionKey="spocpi" RowKey="dummy" CreationDateTime="2019-09-17T10:08:09.415Z" CreationDateTime@odata.type=Edm.DateTime Last01Hours="" Last24Hours="" TotalCount="" TotalCount@odata.type=Edm.Int32 --table-name "AzureCommonMetrics" --account-name $analyticsStorageAccountName

#azurelibrarymetrics table
az storage entity insert --entity PartitionKey="spocpi" RowKey="dummy" CreationDateTime="2019-09-17T10:08:09.415Z" CreationDateTime@odata.type=Edm.DateTime Extension="[]" IsFolder="[]" SiteUrl="https://dummysite" LibraryUrl="https://dummysite/dummylibrary" TotalRowCount="1" TotalRowCount@odata.type=Edm.Int32 --table-name "AzureLibraryMetrics" --account-name $analyticsStorageAccountName

#splibrarymetrics table
az storage entity insert --entity PartitionKey="spocpi" RowKey="dummy" CreationDateTime="2019-09-17T10:08:09.415Z" CreationDateTime@odata.type=Edm.DateTime FileType="[]" SiteUrl="https://dummysite" LibraryUrl="https://dummysite/dummylibrary" TotalRowCount="1" TotalRowCount@odata.type=Edm.Int32 --table-name "SharePointLibraryMetrics" --account-name $analyticsStorageAccountName

Write-Host "Added Dummy data into Storage Tables" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

Write-Host "Adding Search Indexes" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

#Create Search Indexes
Get-ChildItem ".\search\indexes" |  
    ForEach-Object {
        Write-Host "Processing Index file: $_.FullName" -ForegroundColor Green

        $indexDefinition = Get-Content $_.FullName -Raw

        $headers = @{
            'api-key' = $searchAdminPrimaryKey
            'Content-Type' = 'application/json' 
            'Accept' = 'application/json' }

        $url = $("https://$searchServiceName.search.windows.net/indexes?api-version=2019-05-06")

        Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $indexDefinition | ConvertTo-Json

        Write-Host "Processed Index file: $_.FullName" -ForegroundColor Green
    }

Write-Host "Added Search Indexes" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

Write-Host "Adding Data Sources" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

#Create Data Sources
Get-ChildItem ".\search\datasources" |  
    ForEach-Object {
        Write-Host "Processing Data Source file: $_.FullName" -ForegroundColor Green
		
		If ($_.FullName.Contains("subscription")) {
			$storageAccountName = $webHooksStorageAccountName
		}
		else {
			$storageAccountName = $docTracStorageAccountName
		}
		
        $storageConnectionString = (az storage account show-connection-string -g $parametersJson.parameters.resourceGroupName.value -n $storageAccountName | ConvertFrom-Json).ConnectionString
        $datasourceDefinition = (Get-Content $_.FullName -Raw).replace("[STORAGECONNECTION]", $storageConnectionString)

        $headers = @{
            'api-key' = $searchAdminPrimaryKey
            'Content-Type' = 'application/json' 
            'Accept' = 'application/json' }

        $url = $("https://$searchServiceName.search.windows.net/datasources?api-version=2019-05-06")

        Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $datasourceDefinition | ConvertTo-Json

        Write-Host "Processed Data Source file: $_.FullName" -ForegroundColor Green
    }

Write-Host "Added Data Sources" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

Write-Host "Adding Indexers" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

#Create Indexer
Get-ChildItem ".\search\indexers" |  
    ForEach-Object {
        Write-Host "Processing Indexer file: $_.FullName" -ForegroundColor Green

        $indexerDefinition = Get-Content $_.FullName -Raw

        $headers = @{
            'api-key' = $searchAdminPrimaryKey
            'Content-Type' = 'application/json' 
            'Accept' = 'application/json' }

        $url = $("https://$searchServiceName.search.windows.net/indexers?api-version=2019-05-06")

        Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $indexerDefinition | ConvertTo-Json

        Write-Host "Processed Indexer file: $_.FullName" -ForegroundColor Green
    }

Write-Host "Added Indexers." -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

Write-Host "Deleting Crawled Data." -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

$removeIndexJson = Get-Content ".\search\removeindex\index.json" 

#Delete the crawled data as indexes are created already.
Get-ChildItem ".\search\indexes" |  
    ForEach-Object {
        $indexDefinition = Get-Content $_.FullName -Raw
        $indexName = ($indexDefinition | Out-String | ConvertFrom-Json).name
        
        Write-Host "Deleting Index: $indexName" -ForegroundColor Green 

        $headers = @{
            'api-key' = $searchAdminPrimaryKey
            'Content-Type' = 'application/json' 
            'Accept' = 'application/json' }

        $url = $("https://$searchServiceName.search.windows.net/indexes/$indexName/docs/index?api-version=2019-05-06")

        Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $removeIndexJson | ConvertTo-Json

        Write-Host "Deleted Index: $indexName" -ForegroundColor Green 
    }

Write-Host "Deleted Crawled Data." -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green