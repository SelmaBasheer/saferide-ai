# Locals are values computed once and reused. Unlike variables they are not
# inputs — nobody sets them from outside. They exist to stop the same
# expression being repeated and drifting out of sync.
locals {
  base = "${var.project_name}-${var.environment}"

  # Applied to every resource. Tags are how you later answer "what is this and
  # who made it" in the portal, and how Cost Management groups spending.
  tags = {
    project     = "SafeRide"
    environment = var.environment
    managed_by  = "Terraform"
  }
}

# A resource group is a folder. Everything SafeRide needs goes in this one, so
# `terraform destroy` — or deleting this group — removes all of it at once.
# The Terraform state lives in a different group on purpose, so destroying the
# infrastructure can never destroy the record of it.
resource "azurerm_resource_group" "main" {
  name     = "rg-${local.base}"
  location = var.location
  tags     = local.tags
}

# Your private Docker registry. CI pushes eleven images here; the cluster pulls
# from here. Basic is the cheapest tier — 10 GiB, plenty for eleven images.
resource "azurerm_container_registry" "main" {
  # Alphanumeric only, no hyphens, globally unique. Hence the suffix.
  name                = "${var.project_name}${var.environment}acr${var.unique_suffix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"

  # An admin user is a username and password that can push and pull. Disabled
  # because the cluster will authenticate with a managed identity instead, so
  # no credential exists to leak.
  admin_enabled = false

  tags = local.tags
}

# Where container logs and cluster metrics land. Without this, reading a
# crashed pod's logs means catching it before Kubernetes discards them.
resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.base}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"

  # Billed per GB ingested and per day retained. Thirty days is the shortest
  # retention Azure allows on this tier.
  retention_in_days = 30

  tags = local.tags
}

# The cluster itself.
resource "azurerm_kubernetes_cluster" "main" {
  name                = "aks-${local.base}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  # Becomes part of the cluster's API server hostname.
  dns_prefix = local.base

  # Free tier: Azure runs the control plane — the brain that schedules pods —
  # at no charge, with no uptime guarantee. Standard costs about $73/month and
  # buys an SLA. For a dev cluster, Free.
  sku_tier = "Free"

  default_node_pool {
    name = "system"

    # Two nodes rather than one: eleven services plus five data stores plus
    # Kubernetes' own system pods do not fit comfortably in a single node's
    # 8 GiB. This is where the money goes.
    node_count = var.node_count
    vm_size    = var.node_vm_size

    # Each node gets a managed disk, billed by size. The default is 128 GiB,
    # which is far more than container images need.
    os_disk_size_gb = 64
  }

  identity {
    # Azure creates an identity for the cluster and manages its credentials.
    # This is what lets the next resource grant it registry access without any
    # password existing anywhere.
    type = "SystemAssigned"
  }

  # Ships container logs to the workspace above.
  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  }

  tags = local.tags
}

# Creating a registry and a cluster does not let one talk to the other. Without
# this, every pod fails with ImagePullBackOff and the reason is buried in the
# pod's events.
#
# kubelet_identity is the identity the node agent uses when pulling images,
# which is distinct from the cluster's own identity — a common and confusing
# mix-up.
resource "azurerm_role_assignment" "aks_acr_pull" {
  scope                            = azurerm_container_registry.main.id
  role_definition_name             = "AcrPull"
  principal_id                     = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
  skip_service_principal_aad_check = true
}

# Replaces Azurite. Azurite is the local emulator; in Azure you use the real
# service, so the connection string changes and nothing else does.
resource "azurerm_storage_account" "media" {
  name                     = "${var.project_name}${var.environment}sa${var.unique_suffix}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  # Profile photos and bus documents are served directly to the browser by
  # URL, so the containers holding them must be publicly readable.
  allow_nested_items_to_be_public = true

  tags = local.tags
}