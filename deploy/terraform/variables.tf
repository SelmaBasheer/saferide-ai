# Variables are inputs that we can use throughout our Terraform project.
# They make the configuration reusable.
# Instead of hardcoding values inside Azure resource definitions,
# we define them here and reuse them wherever needed.

variable "project_name" {
  description = "Short name used as a prefix for every resource"
  type        = string
  default     = "saferide"
}

variable "environment" {
  description = "Which environment this is: dev, staging, prod"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region. Central India, because that is where your quota is."
  type        = string
  default     = "centralindia"
}

variable "node_count" {
  # Specifies how many virtual machines will act as worker nodes
  # in our Azure Kubernetes Service (AKS) cluster.
  #
  # SafeRide has several microservices and supporting data stores.
  # Two nodes provide more computing capacity than one node,
  # although the actual capacity depends on workload and resource limits.

  description = <<-EOT
    Number of AKS worker nodes.
    The default is two to provide more capacity for SafeRide's
    microservices, data stores and Kubernetes system workloads.
  EOT
  type        = number
  default     = 2
}

variable "node_vm_size" {
  description = <<-EOT
    Standard_D2s_v4: 2 vCPU, 8 GiB. Chosen because your subscription's quota
    for the DSv5 family is zero, so the usual Standard_D2s_v5 cannot be
    created. DSv4 has a limit of 10 vCPUs, and two nodes uses four.
  EOT
  type        = string
  default     = "Standard_D2s_v4"
}