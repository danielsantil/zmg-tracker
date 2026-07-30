data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "zmg" {
  name     = "zmg-rg"
  location = var.location
}

resource "azurerm_log_analytics_workspace" "zmg" {
  name                = "workspace-zmgrgxjgf"
  resource_group_name = azurerm_resource_group.zmg.name
  location            = azurerm_resource_group.zmg.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_app_environment" "zmg" {
  name                = "zmg-env"
  resource_group_name = azurerm_resource_group.zmg.name
  location            = azurerm_resource_group.zmg.location
  logs_destination    = "azure-monitor"
  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
  }
}

resource "azurerm_monitor_diagnostic_setting" "zmg_env" {
  name                       = "zmg-env-to-workspace"
  target_resource_id         = azurerm_container_app_environment.zmg.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.zmg.id

  enabled_log { category = "ContainerAppConsoleLogs" }
  enabled_log { category = "ContainerAppSystemLogs" }
  enabled_log { category = "ContainerAppHTTPLogs" }
}

resource "azurerm_container_app" "zmg" {
  name                         = "zmg-app"
  resource_group_name          = azurerm_resource_group.zmg.name
  container_app_environment_id = azurerm_container_app_environment.zmg.id
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"
  max_inactive_revisions       = 100
  tags                         = {}

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id]
  }

  ingress {
    external_enabled           = true
    target_port                = 8080
    transport                  = "auto"
    allow_insecure_connections = false

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  registry {
    server               = "ghcr.io"
    username             = var.ghcr_username
    password_secret_name = "ghcrio-danielsantil"
  }

  secret {
    name  = "ghcrio-danielsantil"
    value = var.ghcr_token
  }

  template {
    min_replicas                     = 0
    max_replicas                     = 1
    cooldown_period_in_seconds       = 300
    polling_interval_in_seconds      = 30
    termination_grace_period_seconds = 0

    container {
      name   = "zmg-app"
      image  = var.container_image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "KeyVault__Uri"
        value = azurerm_key_vault.prod.vault_uri
      }

      env {
        name  = "AZURE_CLIENT_ID"
        value = azurerm_user_assigned_identity.app.client_id
      }

      env {
        name  = "Database__MigrateOnStartup"
        value = false
      }
    }
  }

  lifecycle {
    # Deploys ship a new tag via `az containerapp update`; Terraform must not revert it.
    ignore_changes = [template[0].container[0].image]
  }
}

resource "azurerm_user_assigned_identity" "app" {
  name                = "zmg-app-identity"
  resource_group_name = azurerm_resource_group.zmg.name
  location            = azurerm_resource_group.zmg.location
}

resource "azurerm_key_vault" "prod" {
  name                       = "zmg-prod-kv"
  resource_group_name        = azurerm_resource_group.zmg.name
  location                   = azurerm_resource_group.zmg.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = false
}

resource "azurerm_role_assignment" "app_kv_read" {
  scope                = azurerm_key_vault.prod.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.app.principal_id
}

resource "azurerm_role_assignment" "me_kv_read" {
  scope                = azurerm_key_vault.prod.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}