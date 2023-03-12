# play.inventory
Play Economy Inventory microservice.

## Create and publish package
```powershell
$version="1.0.3"
$owner="play-microservice"
$gh_pat="[GITHUB ACCESS TOKEN HERE]"
$nuget_src_name="Play Github"
$package_name="Play.Inventory.Contracts"

dotnet pack src\$package_name\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/play.inventory -o ..\packages

dotnet nuget push ..\packages\$package_name.$version.nupkg --api-key $gh_pat --source $nuget_src_name
```

## Build the docker image
```powershell
$env:GH_OWNER="play-microservice"
$env:GH_PAT="[GITHUB ACCESS TOKEN HERE]"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t play.inventory:$version .
```

## Run the docker image
### local version
```powershell
docker run -it --rm -p 5004:5004 --name inventory -e MongoDbSettings__Host=mongo -e RabbitMQSettings__Host=rabbitmq --network playinfra_default play.inventory:$version
```
### azure version
```powershell
$cosmosDbConnString="[CONN STRING HERE]"
$serviceBusConnString="[CONN STRING HERE]"
docker run -it --rm -p 5004:5004 --name inventory -e MongoDbSettings__ConnectionString=$cosmosDbConnString -e ServiceBusSettings__ConnectionString=$serviceBusConnString -e ServiceSettings__MessageBroker="SERVICEBUS" play.inventory:$version
```