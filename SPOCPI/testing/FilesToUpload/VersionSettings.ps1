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

 $Ctx.Load($List)
 $Ctx.ExecuteQuery()
 $List.EnableVersioning = $false
 
 if ($List.EnableVersioning -eq $false) 
        {
        #Enable Versioning Settings
        write-host "Applying Settings on List: $($List.Title)"
        $List.EnableVersioning = $true
        $List.MajorVersionLimit = 5 #No. of versions - versioning best practices
        $List.EnableMinorVersions = $true #Applicable only to Libraries
        $List.MajorWithMinorVersionsLimit = 5 #No. of Drafts in Lists

 
        $List.Update() 
        write-host Versioning enabled for: $List.Title
       }