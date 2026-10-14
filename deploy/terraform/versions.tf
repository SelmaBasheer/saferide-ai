terraform {
  # This project requires Terraform CLI version 1.9.0 or newer.
  # If someone uses an older version, Terraform will show an error.
  required_version = ">= 1.9.0"

  # List the providers this project needs.
  # A provider is a plugin that allows Terraform to communicate
  # with an external platform, such as Microsoft Azure.
  required_providers {
    azurerm = {
      # Download the official Azure provider maintained by HashiCorp.
      source = "hashicorp/azurerm"

      # Allow Azure provider versions from 4.0 up to, but not including, 5.0.
      # This avoids automatically upgrading to a potentially incompatible
      # major version.
      version = "~> 4.0"
    }
  }

  # Where Terraform keeps its record of what it has built. Remote rather than
  # local because the file holds secrets in plain text, and because blob
  # leasing stops two applies running at once.
  backend "azurerm" {
    resource_group_name  = "rg-saferide-tfstate"
    storage_account_name = "saferidetfstate30"
    container_name       = "tfstate"
    key                  = "saferide-dev.tfstate"
    use_azuread_auth     = true
  }
}

# Configure the Azure provider so Terraform can manage Azure resources.
provider "azurerm" {
  # Required configuration block for the Azure provider.
  # Additional Azure provider settings can be added here.
  features {
    resource_group {
      # Enabling container insights makes Azure create a "solution" resource
      # that Terraform never declared. By default Terraform refuses to delete a
      # resource group holding resources it does not manage. Everything in
      # rg-saferide-dev is ours, so let destroy clear it out.
      prevent_deletion_if_contains_resources = false
    }
  }

  # Terraform uses your Azure CLI login to authenticate with Azure.
  # Run 'az login' before running Terraform.
  # Terraform uses this login to determine which Azure account
  # and subscription to access, unless configured otherwise.
}
