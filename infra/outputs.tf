output "app_url" {
  description = "Public URL of the container app"
  value       = "https://${azurerm_container_app.zmg.ingress[0].fqdn}"
}

output "deploy_client_id" {
  description = "AZURE_CLIENT_ID for the GitHub Actions deploy job - not a secret"
  value       = azurerm_user_assigned_identity.deploy.client_id
}

output "deploy_tenant_id" {
  description = "AZURE_TENANT_ID for the GitHub Actions deploy job"
  value       = azurerm_user_assigned_identity.deploy.tenant_id
}