output "app_url" {
  description = "Public URL of the container app"
  value       = "https://${azurerm_container_app.zmg.ingress[0].fqdn}"
}