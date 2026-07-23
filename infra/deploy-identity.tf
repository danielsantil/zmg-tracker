resource "azurerm_user_assigned_identity" "deploy" {
  name                = "zmg-deploy"
  resource_group_name = azurerm_resource_group.zmg.name
  location            = azurerm_resource_group.zmg.location
}

resource "azurerm_federated_identity_credential" "github_production" {
  name                      = "github-production"
  user_assigned_identity_id = azurerm_user_assigned_identity.deploy.id
  issuer                    = "https://token.actions.githubusercontent.com"
  audience                  = ["api://AzureADTokenExchange"]
  subject                   = "repo:danielsantil/zmg-tracker:environment:production"
}

resource "azurerm_role_assignment" "deploy_container_app" {
  scope                = azurerm_container_app.zmg.id
  role_definition_name = "Container Apps Contributor"
  principal_id         = azurerm_user_assigned_identity.deploy.principal_id
}