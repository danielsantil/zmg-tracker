import {
  to = azurerm_resource_group.zmg
  id = "/subscriptions/90d6b1cb-6d6c-4ed6-af8e-dbb8319f9983/resourceGroups/zmg-rg"
}

import {
  to = azurerm_log_analytics_workspace.zmg
  id = "/subscriptions/90d6b1cb-6d6c-4ed6-af8e-dbb8319f9983/resourceGroups/zmg-rg/providers/Microsoft.OperationalInsights/workspaces/workspace-zmgrgxjgf"
}

import {
  to = azurerm_container_app_environment.zmg
  id = "/subscriptions/90d6b1cb-6d6c-4ed6-af8e-dbb8319f9983/resourceGroups/zmg-rg/providers/Microsoft.App/managedEnvironments/zmg-env"
}

import {
  to = azurerm_container_app.zmg
  id = "/subscriptions/90d6b1cb-6d6c-4ed6-af8e-dbb8319f9983/resourceGroups/zmg-rg/providers/Microsoft.App/containerApps/zmg-app"
}

import {
  to = neon_project.zmg
  id = "square-violet-64616931"
}

import {
  to = cloudflare_r2_bucket.covers
  id = "aa697a66b815f21d509abc14613b070d/zmg-covers/default"
}