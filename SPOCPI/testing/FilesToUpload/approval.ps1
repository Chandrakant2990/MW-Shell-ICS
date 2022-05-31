Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.dll"
Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.Runtime.dll"
   
#Parameters
$SiteURL = "https://msshelldevelopment.sharepoint.com/sites/SPOCPI"
$ListName ="Documents"
$User = "AlexW@msshelldevelopment.OnMicrosoft.com"

$Password = Read-Host -Prompt "Please enter your password" -AsSecureString
 

 
#Setup the Context
$Ctx = New-Object Microsoft.SharePoint.Client.ClientContext($SiteURL)
$Creds = New-Object Microsoft.SharePoint.Client.SharePointOnlineCredentials($User,$Password)
$Ctx.Credentials = $Creds

  
#Get the File and Approve
$List = $Ctx.web.Lists.GetByTitle($ListName)
 
#Query to get all items with "Pending" status
$Query = New-Object Microsoft.SharePoint.Client.CamlQuery
$Query.ViewXml = "<View Scope = 'RecursiveAll'><Query><Where><Eq><FieldRef Name='_ModerationStatus' /><Value Type='ModStat'>3</Value></Eq></Where></Query></View>"
$ListItems = $List.GetItems($Query)
$Ctx.Load($ListItems)
$Ctx.ExecuteQuery()
 
#Approve all pending List Items
ForEach($Item in $ListItems)
{
    $Item["_ModerationStatus"] = 0
    $Item.Update()
    $Ctx.ExecuteQuery()
    Write-host -f Green "Approved List Item:" $Item.Id
}

