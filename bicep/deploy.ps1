$rgName = "rg-stttcosmosdb-test"

az group create --name $rgName --location eastus
az deployment group create --resource-group $rgName --template-file main.bicep