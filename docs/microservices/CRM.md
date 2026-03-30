# CRM Service (`crm-service`)

## Overview

The CRM service adds **Leads**, **Opportunities (pipeline)**, and **Activities** to the ERP as a first-class microservice with its own database (`CrmDb`). Customers remain canonical in **Sales** (`sales-service`).

## API (v1)

Base route: `api/crm/*` (typically exposed through the gateway under `/crm/api/crm/*`).

### Leads
- `GET api/crm/leads` (supports `QuerySpec` filtering/search/pagination)
- `GET api/crm/leads/{id}`
- `POST api/crm/leads`
- `PUT api/crm/leads/{id}`
- `POST api/crm/leads/{id}/qualify`
- `DELETE api/crm/leads/{id}`

### Opportunities
- `GET api/crm/opportunities` (supports `QuerySpec`)
- `GET api/crm/opportunities/{id}`
- `POST api/crm/opportunities`
- `PUT api/crm/opportunities/{id}/forecast`
- `POST api/crm/opportunities/{id}/move-stage`
- `POST api/crm/opportunities/{id}/mark-won`
- `POST api/crm/opportunities/{id}/mark-lost`

#### Converting an opportunity to a Sales quote
`POST api/crm/opportunities/{id}/mark-won` accepts a `MarkOpportunityWonRequest`. If `ConvertToQuote=true`, CRM will synchronously invoke `sales-service` to create a quote (`POST api/sales/orders/quotes`) and store `ConvertedSalesQuoteId/Number` on the opportunity.

### Activities
- `GET api/crm/activities` (supports `QuerySpec`)
- `GET api/crm/activities/{id}`
- `POST api/crm/activities`
- `POST api/crm/activities/{id}/complete`

## Events (Dapr pub/sub)

PubSub component: `pubsub`

### Published by CRM
- `crm.lead.created`, `crm.lead.updated`, `crm.lead.qualified`
- `crm.opportunity.created`, `crm.opportunity.stage-changed`, `crm.opportunity.won`, `crm.opportunity.lost`
- `crm.activity.created`, `crm.activity.completed`

### Subscribed by CRM
- `sales.customer.created`
- `sales.customer.updated`

## Permissions

Controllers use `[HasPermission(\"CRM\", \"<Action>\")]` with standard actions: `Read`, `Create`, `Update`, `Delete`.

