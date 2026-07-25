terraform {
  required_version = ">= 1.9"

  # State lives in Azure Storage, in a resource group separate from the app's own
  # (zmg-rg) so a `terraform destroy` can't delete the state describing what it
  # destroys. The account has shared key access disabled, so auth is Azure AD only
  # — hence use_azuread_auth. Backend blocks can't take variables; nothing here is
  # a secret. See infra/README.md → "Remote state".
  backend "azurerm" {
    resource_group_name  = "zmg-tfstate-rg"
    storage_account_name = "zmgtfstate1"
    container_name       = "tfstate"
    key                  = "zmg.tfstate"
    use_azuread_auth     = true
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.81"
    }

    neon = {
      source  = "kislerdm/neon"
      version = "0.13.0"
    }

    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "~>5.12"
    }
  }
}