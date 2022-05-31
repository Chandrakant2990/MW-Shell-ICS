
#Specify tenant admin and site URL
$User = "AlexW@msshelldevelopment.OnMicrosoft.com"
$SiteURL = "https://msshelldevelopment.sharepoint.com/sites/SPOCPI"


#Add references to SharePoint client assemblies and authenticate to Office 365 site - required for CSOM
Add-Type -Path "D:\Projects\Shell Search\DEV\EnterpriseSearch\SPOCPI\Testing\FilesToUpload\Library\Microsoft.SharePoint.Client.dll"  
Add-Type -Path "D:\Projects\Shell Search\DEV\EnterpriseSearch\SPOCPI\Testing\FilesToUpload\Library\Microsoft.SharePoint.Client.Runtime.dll"  
Add-PSSnapin microsoft.sharepoint.powershell -ErrorAction SilentlyContinue

<#
    Create Folder Function.
#>
function PopulateFolder($ListRootFolder, $FolderRelativePath)
{
    #split the FolderRelativePath passed into chunks (between the backslashes) so that we can check if the folder structure exists
    $PathChunks = $FolderRelativePath.substring(1).split("\")

    #Make sure we start with a fresh WorkingFolder for every folder passed to the function
    if($WorkingFolder)
    {
        Remove-Variable WorkingFolder
    }

    #Start with the root folder of the list, load this into context
    $WorkingFolder = $ListRootFolder
    $Context.load($WorkingFolder)
    $Context.ExecuteQuery()

    #Load the folders of the current working folder into context
    $Context.load(($WorkingFolder.folders))
    $Context.executeQuery()

   
    #Set the FileSource folder equal to the absolute path of the folder that passed to the function
    $FileSource = $Folder + $FolderRelativePath
    
    #Loop through the folder chunks, ensuring that the correct folder hierarchy exists in the destination
    foreach($Chunk in $PathChunks)
    {
        #Check to find out if a subfolder exists in the current folder that matches the patch chunk being evaluated
        if($WorkingFolder.folders | ? {$_.name -eq $Chunk})
        {
            #Log the status to the PowerShell host window
            Write-Host "Folder $Chunk Exists in" $WorkingFolder.name -ForegroundColor Green

            #Since we will be evaluating other chunks in the path, set the working folder to the current folder and load this into context.
            $WorkingFolder = $WorkingFolder.folders | ? {$_.name -eq $Chunk}
            $Context.load($WorkingFolder)
            $Context.load($WorkingFolder.folders)
            $Context.ExecuteQuery()            
        }
        else
        {
            #If the folder doesn't exist, Log a message indicating that the folder doesn't exist, and another message indicating that it is being created
            Write-Host "Folder $Chunk Does Not Exist in" $WorkingFolder.name -ForegroundColor Yellow
            Write-Host "Creating Folder $Chunk in" $WorkingFolder.name -ForegroundColor Green
            
            #Load the working folder into context and create a subfolder with a name equal to the chunk being evaluated, and load this into context
            $Context.load($WorkingFolder)
            $Context.load($WorkingFolder.folders)
            $Context.ExecuteQuery()
            $WorkingFolder= $WorkingFolder.folders.add($Chunk)
            $Context.load($WorkingFolder)
            $Context.load($WorkingFolder.folders)
            $Context.ExecuteQuery()
        }
    }

	return $WorkingFolder
  }

<#
	Upload File - This function performs the actual file upload
#>
function UploadFile($DestinationFolder, $File)
{
    #Get the datastream of the file, assign it to a variable
    $FileStream = New-Object IO.FileStream($File.FullName,[System.IO.FileMode]::Open)

    #Create an instance of a FileCreationInformation object
    $FileCreationInfo = New-Object Microsoft.SharePoint.Client.FileCreationInformation

    #Indicate whether or not you would like to overwrite files in the event of a conflict
    $FileCreationInfo.Overwrite = $True

    #Make the datastream of the file you wish to create equal to the datastream of the source file 
    $FileCreationInfo.ContentStream = $FileStream

    #Make the URL of the file equal to the $File variable which was passed to the function.  This will be equal to the source file name
    $FileCreationInfo.url = $File

    #Add the file to the destination folder which was passed to the function, using the FileCreationInformation supplied.  Assign this to a variable so that it can be loaded into context.
    $Upload = $DestinationFolder.Files.Add($FileCreationInfo)
    if($Checkin)
    {
        $Context.Load($Upload)
        $Context.ExecuteQuery()
        if($Upload.CheckOutType -ne "none")
        {
            $Upload.CheckIn("Checked in by Administrator", [Microsoft.SharePoint.Client.CheckinType]::MajorCheckIn)
        }
    }
    $Context.Load($Upload)
    $Context.ExecuteQuery()
}

#Get a recursive list 

$Folder = "C:\FilesToUpload"
$DocLibName = "PerformanceTest"

$AllFolders = Get-ChildItem -Recurse -Directory -Path $Folder

#Get a list of all files that exist directly at the root of the folder supplied by the operator
#$FilesInRoot = Get-ChildItem -Path $Folder | Where { ! $_.PSIsContainer }
  
$Password = Read-Host -Prompt "Please enter your password" -AsSecureString
#Bind to site collection
$Context = New-Object Microsoft.SharePoint.Client.ClientContext($SiteURL)
$Creds = New-Object Microsoft.SharePoint.Client.SharePointOnlineCredentials($User,$Password)
$Context.Credentials = $Creds


#Retrieve list
$List = $Context.Web.Lists.GetByTitle($DocLibName)
$Context.Load($List)
#----------------
$Context.Load($List.RootFolder);
$Context.Load($List.RootFolder.Folders);
$Context.Load($List.RootFolder.Files);
#----------------------------------------------------

$Context.ExecuteQuery();



#Upload all files in the root of the folder supplied by the operator
#Foreach ($File in ($FilesInRoot))
#{
#
#    #Notify the operator that the file is being uploaded to a specific location
#    Write-Host "Uploading file " $File.Name "to" $DocLibName -ForegroundColor Cyan

#    #Upload the file
#    UploadFile($list.RootFolder) $File   

#}

#Loop through all folders (recursive) that exist within the folder supplied by the operator
foreach($CurrentFolder in $AllFolders)
{
    #Set the FolderRelativePath by removing the path of the folder supplied by the operator from the fullname of the folder
    $FolderRelativePath = ($CurrentFolder.FullName).Substring($Folder.Length)
    
    #Call the PopulateFolder function for the current folder, which will ensure that the folder exists and upload all files in the folder to the appropriate location
    $newFolder = PopulateFolder ($list.RootFolder) $FolderRelativePath

    $dir = $CurrentFolder.FullName

    $files = Get-ChildItem $dir | Where { ! $_.PSIsContainer }

    for ($i=0; $i -lt $files.Count; $i++) 
    {
        $outfile = $files[$i].FullName

        #Notify the operator that the file is being uploaded to a specific location
        Write-Host "Uploading file " $outfile "to" $DocLibName$FolderRelativePath  -ForegroundColor Cyan        
        
        #Upload the file
        UploadFile($newFolder) $files[$i]
    }
}







