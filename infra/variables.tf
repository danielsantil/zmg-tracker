variable "subscription_id" {
  description = "Azure subscription that hosts the ZMG stack"
  type        = string
}

variable "location" {
  description = "Azure region for resources in this config"
  type        = string
  default     = "eastus"
}

variable "ghcr_username" {
  description = "GitHub user the container app uses to authenticate to GHCR"
  type        = string
  default     = "danielsantil"
}

variable "ghcr_token" {
  description = "GHCR read:packages token - registry password secret"
  type        = string
  sensitive   = true
}

variable "container_image" {
  description = "Container image. Bootstrap value only — real deploys CHANGE this value"
  type        = string
  default     = "ghcr.io/danielsantil/zmg-tracker:31c16e4"
}

variable "neon_api_key" {
  description = "Neon account API key"
  type        = string
  sensitive   = true
}

variable "cloudflare_api_token" {
  description = "Cloudflare API token with Account - Workers R2 Storage - Edit permission"
  type        = string
  sensitive   = true
}

variable "r2_account_id" {
  description = "Cloudflare account id that owns the R2 bucket"
  type        = string
}

variable "r2_bucket" {
  description = "R2 bucket for release cover images"
  type        = string
}