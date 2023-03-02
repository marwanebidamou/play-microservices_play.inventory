# play.inventory
Play Economy Inventory microservice.

## Create and publish package
```powershell
$version="1.0.2"
$owner="play-microservice"
$gh_pat="[GITHUB ACCESS TOKEN HERE]"
$nuget_src_name="Play Github"
$package_name="Play.Inventory.Contracts"

dotnet pack src\$package_name\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/play.inventory -o ..\packages

dotnet nuget push ..\packages\$package_name.$version.nupkg --api-key $gh_pat --source $nuget_src_name
```