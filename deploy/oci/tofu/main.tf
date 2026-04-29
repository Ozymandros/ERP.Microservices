module "compartment" {
  source = "./modules/compartment"

  tenancy_ocid              = var.tenancy_ocid
  existing_compartment_ocid = var.existing_compartment_ocid
  compartment_name          = local.compartment_name
  compartment_description   = var.compartment_description
  freeform_tags             = local.base_tags
}

module "network" {
  source = "./modules/network"

  compartment_ocid  = module.compartment.compartment_ocid
  name_prefix       = local.name_prefix
  vcn_cidr          = var.vcn_cidr
  public_subnet_cidr  = var.public_subnet_cidr
  private_subnet_cidr = var.private_subnet_cidr
  freeform_tags     = local.base_tags
}

module "oke" {
  source = "./modules/oke"

  compartment_ocid   = module.compartment.compartment_ocid
  name_prefix        = local.name_prefix
  vcn_ocid           = module.network.vcn_ocid
  kubernetes_version = var.kubernetes_version
  node_pool_size     = var.node_pool_size
  node_shape         = var.node_shape
  node_ocpus         = var.node_ocpus
  node_memory_gbs    = var.node_memory_gbs
  image_id           = var.image_id
  public_subnet_ocid = module.network.public_subnet_ocid
  private_subnet_ocid = module.network.private_subnet_ocid
  freeform_tags      = local.base_tags
}
