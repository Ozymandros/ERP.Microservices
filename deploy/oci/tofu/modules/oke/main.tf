resource "oci_containerengine_cluster" "this" {
  compartment_id     = var.compartment_ocid
  name               = "${var.name_prefix}-oke"
  vcn_id             = var.vcn_ocid
  kubernetes_version = var.kubernetes_version
  freeform_tags      = var.freeform_tags

  endpoint_config {
    is_public_ip_enabled = true
    subnet_id            = var.public_subnet_ocid
  }

  options {
    service_lb_subnet_ids = [var.public_subnet_ocid]
    add_ons {
      is_kubernetes_dashboard_enabled = false
      is_tiller_enabled               = false
    }
    kubernetes_network_config {
      pods_cidr     = "10.244.0.0/16"
      services_cidr = "10.96.0.0/16"
    }
  }
}

resource "oci_containerengine_node_pool" "this" {
  cluster_id          = oci_containerengine_cluster.this.id
  compartment_id      = var.compartment_ocid
  kubernetes_version  = var.kubernetes_version
  name                = "${var.name_prefix}-np"
  node_shape          = var.node_shape
  freeform_tags       = var.freeform_tags

  node_config_details {
    size = var.node_pool_size
    placement_configs {
      availability_domain = data.oci_identity_availability_domains.ads.availability_domains[0].name
      subnet_id           = var.private_subnet_ocid
    }
  }

  node_shape_config {
    ocpus         = var.node_ocpus
    memory_in_gbs = var.node_memory_gbs
  }

  node_source_details {
    source_type = "IMAGE"
    image_id    = var.image_id != "" ? var.image_id : null
  }
}

data "oci_identity_availability_domains" "ads" {
  compartment_id = var.compartment_ocid
}
