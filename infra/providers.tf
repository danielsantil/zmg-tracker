provider "azurerm" {
  features {}

  subscription_id = var.subscription_id
}

provider "neon" {
  api_key = var.neon_api_key
}

provider "cloudflare" {
  api_token = var.cloudflare_api_token
}