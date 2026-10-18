# Outputs print after apply and can be read later with `terraform output`.
# They save hardcoding generated names into scripts and documentation.

output "resource_group_name" {
  description = "Resource group holding every SafeRide resource"
  value       = azurerm_resource_group.main.name
}

output "acr_name" {
  description = "Container registry name, for `az acr login`"
  value       = azurerm_container_registry.main.name
}

output "acr_login_server" {
  description = "Registry hostname, the prefix for every image tag"
  value       = azurerm_container_registry.main.login_server
}

output "aks_cluster_name" {
  description = "Cluster name, for `az aks get-credentials`"
  value       = azurerm_kubernetes_cluster.main.name
}

output "storage_account_name" {
  description = "Storage account replacing Azurite"
  value       = azurerm_storage_account.media.name
}