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
  name                       = "zmg-env"
  resource_group_name        = azurerm_resource_group.zmg.name
  location                   = azurerm_resource_group.zmg.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.zmg.id
  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
  }
}

resource "azurerm_container_app" "zmg" {
  name                         = "zmg-app"
  resource_group_name          = azurerm_resource_group.zmg.name
  container_app_environment_id = azurerm_container_app_environment.zmg.id
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"
  max_inactive_revisions       = 100
  tags                         = {}

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

  secret {
    name  = "neon-conn"
    value = local.neon_connection_string
  }

  secret {
    name  = "r2-access-key-id"
    value = var.r2_access_key_id
  }

  secret {
    name  = "r2-secret-access-key"
    value = var.r2_secret_access_key
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
        name        = "ConnectionStrings__Zmg"
        secret_name = "neon-conn"
      }

      env {
        name  = "R2__AccountId"
        value = var.r2_account_id
      }

      env {
        name        = "R2__AccessKeyId"
        secret_name = "r2-access-key-id"
      }

      env {
        name        = "R2__SecretAccessKey"
        secret_name = "r2-secret-access-key"
      }

      env {
        name  = "R2__Bucket"
        value = cloudflare_r2_bucket.covers.name
      }

      env {
        name  = "R2__PublicBaseUrl"
        value = var.r2_public_base_url
      }
    }
  }

  lifecycle {
    # Deploys ship a new tag via `az containerapp update`; Terraform must not revert it.
    ignore_changes = [template[0].container[0].image]
  }
}