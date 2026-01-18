# Cost Optimization Guide

Aquest document analitza els costos actuals i proposa optimitzacions per reduir-los, especialment per entorns de desenvolupament.

## Anàlisi de Costos Actuals

### Container Apps (7 serveis + API Gateway)

**Configuració actual:**
- **Microservices** (6 serveis: auth, billing, inventory, orders, purchasing, sales):
  - Min Replicas: **2** (sempre executant)
  - Max Replicas: 5
  - CPU: 0.5 cores per replica
  - Memory: 1.0Gi per replica
  - **Cost aproximat**: ~$0.000012/vCPU-segona, ~$0.0000015/GiB-segona

- **API Gateway**:
  - Min Replicas: **2**
  - Max Replicas: 10
  - CPU: 1.0 core per replica
  - Memory: 2.0Gi per replica

- **Aspire Dashboard**:
  - Min Replicas: 1
  - Max Replicas: 2
  - CPU: 0.5 cores
  - Memory: 1.0Gi

**Càlcul mensual (24/7):**
```
Microservices (6 serveis × 2 replicas × 0.5 CPU × 1.0Gi):
  = 6 × 2 × 0.5 × 30 dies × 24 hores × 3600 segons
  = ~$15-20/mes

API Gateway (2 replicas × 1.0 CPU × 2.0Gi):
  = ~$8-10/mes

Total Container Apps: ~$23-30/mes
```

### SQL Databases

**Configuració actual:**
- 6 bases de dades (una per servei)
- SKU: Basic tier
- Max Size: 2 GB per base de dades

**Cost aproximat**: ~$5/mes per base de dades = **~$30/mes**

### Altres Recursos

- **Container Registry (Basic)**: ~$5/mes
- **Redis Cache (Basic)**: ~$15/mes
- **Log Analytics (PerGB2018)**: ~$2-5/mes (depenent de volum)
- **Application Insights**: Gratuït fins a 5GB/mes
- **App Configuration (Standard)**: ~$0.10/mes
- **Storage Account**: ~$0.02/mes

**Total altres recursos**: ~$22-27/mes

### **COST TOTAL ACTUAL: ~$75-87/mes**

## Problemes Identificats

### 🔴 Crític: Min Replicas massa alt

Tots els serveis tenen `minReplicas: 2`, el que significa que **sempre** hi ha 2 instàncies executant-se, fins i tot quan no hi ha trànsit. Això és innecessari per a:
- Entorns de desenvolupament
- Serveis amb baix trànsit
- Períodes de no ús

### 🟡 Alt: Múltiples bases de dades SQL

6 bases de dades separades augmenten significativament el cost. Per a dev/test, es podria considerar:
- Compartir una base de dades amb múltiples esquemes
- O reduir a 2-3 bases de dades compartides

## Recomanacions d'Optimització

### 1. Reduir Min Replicas (Estalvi: ~$15-20/mes)

**Per entorns de desenvolupament:**
```bicep
// Microservices
minReplicas: 1  // Reduir de 2 a 1
maxReplicas: 3  // Reduir de 5 a 3

// API Gateway
minReplicas: 1  // Reduir de 2 a 1
maxReplicas: 5  // Reduir de 10 a 5
```

**Per entorns de producció:**
- Mantenir `minReplicas: 2` per alta disponibilitat
- Considerar `minReplicas: 1` per serveis no crítics

### 2. Escalar a Zero (Estalvi: ~$20-25/mes)

Per entorns de desenvolupament que no s'utilitzen 24/7:
```bicep
minReplicas: 0  // Escalar a zero quan no hi ha trànsit
maxReplicas: 2  // Escalar fins a 2 quan hi ha demanda
```

**Nota**: El primer request després d'escalar a zero tindrà una latència inicial (~10-30 segons) mentre s'inicia el contenidor.

### 3. Reduir CPU/Memòria per Dev (Estalvi: ~$5-10/mes)

Per entorns de desenvolupament:
```bicep
// Microservices
cpu: '0.25'      // Reduir de 0.5 a 0.25
memory: '0.5Gi'  // Reduir de 1.0Gi a 0.5Gi

// API Gateway (mantenir més recursos)
cpu: '0.5'       // Reduir de 1.0 a 0.5
memory: '1.0Gi'  // Reduir de 2.0Gi a 1.0Gi
```

### 4. Consolidar Bases de Dades (Estalvi: ~$15-20/mes)

**Opció A: Una base de dades amb múltiples esquemes**
```sql
-- Una base de dades "MyAppDB" amb esquemes:
-- - Auth
-- - Billing
-- - Inventory
-- - Orders
-- - Purchasing
-- - Sales
```

**Opció B: 2-3 bases de dades compartides**
- `MyAppCoreDB`: Auth, Billing
- `MyAppOperationsDB`: Inventory, Orders, Purchasing, Sales

### 5. Utilitzar Consumption Plan per Redis (Estalvi: ~$10/mes)

Si és possible, considerar Azure Cache for Redis (Consumption) en lloc de Basic tier.

### 6. Reduir Retenció de Logs (Estalvi: ~$2-5/mes)

```bicep
// Log Analytics
retentionInDays: 7  // Reduir de 30 a 7 dies per dev
```

## Configuració Recomanada per Entorn Dev

### Container Apps
```bicep
// Microservices
minReplicas: 0      // Escalar a zero
maxReplicas: 2
cpu: '0.25'
memory: '0.5Gi'

// API Gateway
minReplicas: 0
maxReplicas: 3
cpu: '0.5'
memory: '1.0Gi'
```

### SQL Databases
- Opció 1: 1 base de dades amb múltiples esquemes
- Opció 2: 2 bases de dades compartides

### Altres
- Log Analytics retention: 7 dies
- Container Registry: Basic (mantenir)
- Redis: Considerar Consumption tier si disponible

## Estalvi Estimat

**Amb totes les optimitzacions aplicades:**

| Optimització | Estalvi Mensual |
|--------------|-----------------|
| Min Replicas: 2 → 0 | $20-25 |
| CPU/Memòria reduïda | $5-10 |
| Consolidar DBs (6 → 1) | $20 |
| Retenció logs reduïda | $2-5 |
| **TOTAL ESTALVI** | **$47-60/mes** |

**Cost optimitzat per dev: ~$15-25/mes** (vs $75-87/mes actual)

## Configuració per Producció

Per entorns de producció, mantenir:
- `minReplicas: 2` per alta disponibilitat
- CPU/Memòria actuals (0.5 CPU, 1.0Gi)
- 6 bases de dades separades (per aïllament i seguretat)
- Retenció de logs: 30-90 dies

## Implementació

Per aplicar aquestes optimitzacions:

1. **Crear paràmetres d'entorn** a `main.bicep`:
   ```bicep
   @description('Environment type: dev, staging, prod')
   param environmentType string = 'dev'
   
   var isDev = environmentType == 'dev'
   var minReplicas = isDev ? 0 : 2
   var cpu = isDev ? '0.25' : '0.5'
   var memory = isDev ? '0.5Gi' : '1.0Gi'
   ```

2. **Passar paràmetres als serveis** des de `main.bicep`

3. **Actualitzar `azure.yaml`** per passar `environmentType`

## Monitoreig de Costos

- Utilitzar Azure Cost Management per monitorar costos reals
- Configurar alertes quan els costos superin un llindar
- Revisar mensualment i ajustar segons necessitat
