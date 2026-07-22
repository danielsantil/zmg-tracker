terraform {
  required_version = ">= 1.9"

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