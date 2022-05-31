
Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.dll"
Add-Type -Path "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\16\ISAPI\Microsoft.SharePoint.Client.Runtime.dll"
Add-PSSnapin Microsoft.SharePoint.PowerShell -ErrorAction SilentlyContinue

#Parameters
$SiteURL = "https://msshelldevelopment.sharepoint.com/sites/SPOCPI"

#https://msshelldevelopment.sharepoint.com/:f:/r/sites/SPOCPI/Documents/Folder1?csf=1&e=1UTRaN
$FolderToDelete="Folder1"
 
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
    $Context.Load($List)
    $ListItems = $List.GetItems([Microsoft.SharePoint.Client.CamlQuery]::CreateAllItemsQuery()) 
    $Context.Load($List)
    $Context.ExecuteQuery()
   
    $Context.Load($ListItems)
   
    $Context.ExecuteQuery()
    write-host "Total Number of List Items found:"$ListItems.Count
    if ($ListItems.Count -gt 0)
     { #Loop through each item and delete
         For ($i = $ListItems.Count-1; $i -ge 0; $i--) 
        { 

        $ListItems[$i].DeleteObject() 
         
        } 
        $Context.ExecuteQuery() 
        Write-Host "All List Items deleted Successfully!" } 


    
  <#  ForEach($ListItem in $ListItems)
    {
        #Approve the File if "Content Approval is Turned-ON"
      
                #$ListItem.File.Approve("Approved by Admin")
                 $List.GetItemById($ListItem.Id).DeleteObject();
                 $ListItem.Update();
                 
                Write-host "Deleted Item: $($ListItem.id)" -foregroundcolor Red
          
            
            }#>


      }


 
    #Get all Sub folders
    #$folders = $Context.web.Folders[$list].SubFolders

 
   <# Foreach ($folder in $folders)
    {
        if ($folder.Name -match $FolderToDelete)
        {
            $Context.web.Folders[$list].SubFolders.Delete($folder);
            Write-Host "Folder has been deleted!"
        } 
    } #>
    


Catch {
    write-host -f Red "Error deleting Folder!" $_.Exception.Message
}


