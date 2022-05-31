<# ------------------------------------------------------------------------------------------------------------------
Function to build and publish the netcore appication
$projectFilePath : Relative path of the project file from Solution directory.
$publishedDirectory : Path of the publish files directory.
$publishZip : Zipped folder of the solution to be deployed.
------------------------------------------------------------------------------------------------------------------ #>
function PublishProject($projectFilePath, $publishedDirectory, $publishZip) {
  try {
    # Publishing Project - Release Mode
    dotnet publish $projectFilePath -c Release --verbosity  minimal

    CreateZip $publishedDirectory $publishZip
  }
  catch {
    Write-Host ("Exception in PublishProject: " + $_.Exception.Message) -ForegroundColor Red
  }
}

<# ------------------------------------------------------------------------------------------------------------------
Function to create zip file
$sourcePath : Path of the published files directory.
$publishZip : Zipped folder of the solution to be deployed.
------------------------------------------------------------------------------------------------------------------ #>

function CreateZip($sourcePath, $publishZip) {
  # Remove publish, if exists
  if (Test-Path $publishZip) {
    Remove-Item $publishZip
  }

  # Publish Zip to working directory
  Add-Type -assembly "system.io.compression.filesystem" 
  [io.compression.zipfile]::CreateFromDirectory($sourcePath, $publishZip)
}

# ------------------------------------------------------------------------------------------------------------------

$solutionPath = (Get-Item -Path "..\src\" -Verbose).FullName;

# Set Path to Solution Directory 
Set-Location -Path $solutionPath

# Build - Release Mode
Write-Host ("============================================================================") -ForegroundColor DarkYellow
Write-Host ("Started cleaning the solution") -ForegroundColor Green
dotnet clean --configuration Release --verbosity  minimal
Write-Host ("Completed cleaning the solution") -ForegroundColor Green
Write-Host ("============================================================================") -ForegroundColor DarkYellow
Write-Host `n

Write-Host ("============================================================================") -ForegroundColor DarkYellow
Write-Host ("Building the Solution in Release Mode") -ForegroundColor Green
dotnet msbuild -p:Configuration=Release --verbosity minimal
Write-Host ("Completed building the solution") -ForegroundColor Green
Write-Host ("============================================================================") -ForegroundColor DarkYellow
Write-Host `n

# Publish the projects output and create zip files
Get-ChildItem -LiteralPath $solutionPath -Filter *.csproj -File -Recurse | ForEach-Object {
  $projectFolderName = $_.Name.TrimEnd("csproj").TrimEnd(".");
  if ($projectFolderName -ne "Shell.SPOCPI.Common" -and $projectFolderName -ne "Samples") {
    Write-Host ("============================================================================") -ForegroundColor DarkYellow
    Write-Host ("Started publishing the project: " + $projectFolderName) -ForegroundColor Green
      
    $publishPath = $solutionPath + "\" + $projectFolderName + "\" + "bin\Release\netcoreapp3.1\publish";
    $projectPath = $projectFolderName + "\" + $_.Name;
    $zipPath = $solutionPath + "..\deploy\" + $projectFolderName + ".zip"
    PublishProject $projectPath $publishPath $zipPath;
      
    Write-Host ("Completed publishing the project: " + $projectFolderName) -ForegroundColor Green
    Write-Host ("============================================================================") -ForegroundColor DarkYellow
    Write-Host `n
  }
}