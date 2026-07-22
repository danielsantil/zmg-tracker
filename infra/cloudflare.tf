resource "cloudflare_r2_bucket" "covers" {
  account_id    = var.r2_account_id
  name          = var.r2_bucket
  jurisdiction  = "default"
  location      = "ENAM"
  storage_class = "Standard"
}
