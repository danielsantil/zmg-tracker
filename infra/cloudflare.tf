resource "cloudflare_r2_bucket" "covers" {
  account_id    = var.r2_account_id
  name          = var.r2_bucket
  jurisdiction  = "default"
  location      = "ENAM"
  storage_class = "Standard"
}

# Binds app.zionmusicgroup.com to the zmg-tracker Worker (v2.10/M53). Cloudflare creates the
# proxied DNS record and issues the certificate itself, so this resource needs only Workers
# Scripts permissions — the token holds no DNS rights, deliberately. The zone and its three
# records stay hand-managed.
resource "cloudflare_workers_custom_domain" "app" {
  account_id = var.r2_account_id
  hostname   = "app.zionmusicgroup.com"
  service    = "zmg-tracker"
  zone_id    = "d9fa1a74f7901d35ada4efad42497d67"
  zone_name  = "zionmusicgroup.com"
}