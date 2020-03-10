param(
    [string] [Parameter(Mandatory=$true)] $AppName,
    [string] [Parameter(Mandatory=$true)] $SubId,
    [string] [Parameter(Mandatory=$true)] $KeyVaultName,
    [string] [Parameter(Mandatory=$true)] $KeyVaultSecretName,
    [string] [Parameter(Mandatory=$true)] $Password,
    [string] [Parameter(Mandatory=$true)] $CertDNSName
)

az group create --name $AppName --location eastus    
az keyvault create -g $AppName -n $AppName-kv --location eastus --enabled-for-deployment

Connect-AzureRmAccount
Set-AzureRmContext -subscriptionid "$SubId"

.\New-ServiceFabricClusterCertificate.ps1 -Password "$Password" -CertDNSName "$AppName" -KeyVaultName "$AppName-kv" -KeyVaultSecretName "$KeyVaultSecretName"

New-AzureRmResourceGroupDeployment -ResourceGroupName "$AppName" -TemplateFile "AzureDeploy.json" -TemplateParameterFile ".\AzureDeploy.Parameters.json"