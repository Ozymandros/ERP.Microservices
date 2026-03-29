# CRM Architecture

## High-level integration

```mermaid
flowchart LR
  apiGateway[ErpApiGateway]
  crmService[crm-service]
  salesService[sales-service]
  authService[auth-service]
  daprPubsub[(Dapr_pubsub)]

  apiGateway --> crmService
  apiGateway --> salesService

  crmService --> authService
  salesService --> authService

  salesService -->|"publish:sales.customer.created/updated"| daprPubsub
  crmService -->|"subscribe:sales.customer.created/updated"| daprPubsub

  crmService -->|"publish:crm.lead.*"| daprPubsub
  crmService -->|"publish:crm.opportunity.*"| daprPubsub
  crmService -->|"publish:crm.activity.*"| daprPubsub
```

## Notes
- Customers remain canonical in **Sales**; CRM references `CustomerId`.
- CRM publishes domain events via Dapr `pubsub` for downstream automation (notifications/workflow can be added later without changing CRM endpoints).

