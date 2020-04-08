# Check there are no unstaged changes
if (git status --porcelain) { throw "Git repo is not clean. Make sure any changes have been committed." }

# Get the version using nbgv  the dll
$version = nbgv get-version
$version = $version[0].Substring("Version:                      ".Length);
$vc = $version.Split(".")
$versionString = @($vc[0], $vc[1], $vc[2]) -join "."

$confirmation = Read-Host "Will create a tag with the version ""$versionString"". Enter ""y"" to proceed"
if ($confirmation -ne 'y') {
    Write-Host "Aborting!"
    Exit
}

Write-Host "Proceeding..."

# Create the tag
Write-Host "Adding tag..."
git tag -a "$versionString" -m "Tagging version $versionString"

# Push the tag
Write-Host "Pushing tag to origin..."
git push origin "$versionString"
