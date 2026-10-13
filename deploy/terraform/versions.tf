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
}

# Configure the Azure provider so Terraform can manage Azure resources.
provider "azurerm" {
  # Required configuration block for the Azure provider.
  # Additional Azure provider settings can be added here.
  features {}

  # Terraform uses your Azure CLI login to authenticate with Azure.
  # Run 'az login' before running Terraform.
  # Terraform uses this login to determine which Azure account
  # and subscription to access, unless configured otherwise.
}
