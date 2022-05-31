Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.dll"
Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.Runtime.dll"
   
#Parameters
$SiteURL = "https://msshelldevelopment.sharepoint.com/sites/SPOCPI"
$ListName ="Documents"
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
        #Approve the File if "Content Approval is Turned-ON
                
               
             
                  $Listitem["Status"] = "Completed"
                 $ListItem.Update()
                 $Ctx.ExecuteQuery()
 
        Write-host -f Green "File's Metadata has been Updated Successfully!"
            
            }
            
  
          
    
}
Catch {
Write-host -f Red "Error:" $_.Exception.Message
}





