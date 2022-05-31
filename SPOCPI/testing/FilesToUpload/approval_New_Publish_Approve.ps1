Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.dll"
Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.Runtime.dll"
   
#Parameters
$SiteURL = "https://msshelldevelopment.sharepoint.com/sites/SPOCPI"
$DocLibName ="Documents"
$User = "AlexW@msshelldevelopment.OnMicrosoft.com"

$Password = Read-Host -Prompt "Please enter your password" -AsSecureString
 
 
 
   
  
Try{
   

    #Setup the Context
    $Context = New-Object Microsoft.SharePoint.Client.ClientContext($SiteURL)
    $Creds = New-Object Microsoft.SharePoint.Client.SharePointOnlineCredentials($User,$Password)
    $Context.Credentials = $Creds

 
    #Get All Items from the List
    $List = $Context.web.Lists.GetByTitle($DocLibName)
    $ListItems = $List.GetItems([Microsoft.SharePoint.Client.CamlQuery]::CreateAllItemsQuery()) 
    $Context.Load($List)
   # $Context.Load($List.RootFolder)
    #$Context.Load($List.RootFolder.Folders)
    #$Context.Load($List.RootFolder.Files)
    
    $Context.ExecuteQuery()
   
    $Context.Load($ListItems)
   
     $Context.ExecuteQuery()
 
    ForEach($ListItem in $ListItems)
    {
        #Approve the File if "Content Approval is Turned-ON"
        If ($List.EnableModeration -eq $true)
        {
           If ($ListItem["_ModerationStatus"] -ne '0')
            { 
               
             
                #$ListItem.File.Approve("Approved by Admin")
                 $ListItem["_ModerationStatus"] = 0
                 $ListItem.Update()
                 $Context.ExecuteQuery()
                 Write-Host "File Approved: "$ListItem["FileLeafRef"] -ForegroundColor Yellow
          
            
            } 
            
  
            #Checkin the File if its checked-out
            If ($ListItem["CheckoutUser"] -ne $null)
            {
                $ListItem.File.CheckIn("Check-in by Admin", [Microsoft.SharePoint.Client.CheckinType]::MajorCheckIn)
                Write-Host "File Checked in: "$ListItem["FileLeafRef"] -ForegroundColor Cyan
                $Context.ExecuteQuery()
            }

          
 
            #Publish the File
        <#   If($List.EnableVersioning -and $List.EnableMinorVersions)
            {
                $ListItem.File.Publish("Published by Admin")
                $Context.ExecuteQuery()
                Write-Host -f Green "File published:" $ListItem["FileLeafRef"]
            }#>
      }
    }
}
Catch {
Write-host -f Red "Error:" $_.Exception.Message
}





