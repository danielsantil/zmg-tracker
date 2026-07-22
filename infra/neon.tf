resource "neon_project" "zmg" {
  name                      = "zmg-tracker"
  org_id                    = "org-dry-hat-93148411"
  region_id                 = "aws-us-east-1"
  pg_version                = 18
  compute_provisioner       = "k8s-neonvm"
  history_retention_seconds = 21600
  store_password            = "yes"

  branch {
    name          = "production"
    database_name = "neondb"
    role_name     = "neondb_owner"
  }

  default_endpoint_settings {
    autoscaling_limit_min_cu = 0.25
    autoscaling_limit_max_cu = 2
    suspend_timeout_seconds  = 0
  }

  maintenance_window {
    start_time = "05:00"
    end_time   = "06:00"
    weekdays   = [3]
  }
}

locals {
  # Npgsql wants keyword=value; Neon's connection_uri is a postgresql://URI it can't parse.
  # Composed from the project's own attributes (tfstate) so the secret follows the database.
  neon_connection_string = join(";", [
    "Host=${neon_project.zmg.database_host_pooler}",
    "Database=${neon_project.zmg.database_name}",
    "Username=${neon_project.zmg.database_user}",
    "Password=${neon_project.zmg.database_password}",
    "SSL Mode=VerifyFull",
    "Channel Binding=Require"
  ])
}