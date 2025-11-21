# Authentication microservice with Keycloak

## Overview

**Authentication** is a standalone microservice for authentication and authorization, built using .NET 9 and integrated with **Keycloak**. 
While it is part of the broader **Document Management System (DMS)**, it is designed to work **independently** and can be used in **any modern system** 
that requires secure identity management. 
It integrates with Keycloak for identity access and user management, and supports both user and machine-to-machine authentication.

---

## Features

🔒 User Authentication / Authorization ✅

🔑 Role-Based Access Control (RBAC) ✅

🔐 Multi-Factor Authentication (MFA) ✅

📧 Email-based Link Verification ✅

📱 Sms based Verification

🌐 GSIS (www1.gsis.gr) Integration

👥 User Provisioning (Auto-Creating Users in Keycloak) ✅

🛡️ GDPR compliance through data anonymization.

🔗 Social Logins: (e.g. Google, Facebook, Apple ID)

📊 Admin Dashboard (optional UI)

---

## 🗃️ Database: PostgreSQL

This service uses **PostgreSQL** to persist data, such as: UserProfiles & TotpSecrets

---

## 📜 Logging - Serilog

This microservice uses **Serilog** for structured logging.
Serilog is configured to log to various sinks, including console, file, Seq, Elastic. 
The configuration can be found in the `appsettings.json` file.

---

## 🚀 Tech Stack

- .NET 9
- Keycloak
- PostgreSQL
- Otp.NET
- IMemoryCache
- Serilog for logging
- Clean Architecture (SOLID)

---

## Keycloak Configuration

### 1. Create Realm
![Create Realm](images/0.CreateRealm.png)

### 2. Create Client
![Create Client](images/1.CreateClient.png)

### 3. Configure Client
![Configure Client](images/2.ConfigureClient.png)

### 4. Assign ServiceAccount Roles to Client
![Assign ServiceAccount](images/3.AssignServiceAccountRoles2Client.png)

### 5. Client-ClientScopes
![Client Scopes](images/4.Client-ClientScopes.png)

### 6. Configure a new Mapper
![Mapper](images/5.ConfigureNewMapper.png)

### 7. Add Mapper Audience
![Mapper Audience](images/6.AddMapperAudience.png)

### 8. Keep Client Secret
![Client Secret](images/7.KeepClientSecret.png)

### 9. Realm Settings User Profile
![Realm Settings](images/8.RealmSettingsUserProfile.png)

### 10. FirstName & LastName required Field Off
![User Profile](images/9.FirstNameRequiredFieldOff.png)

### 11. Create User
![Create User](images/10.CreateUser.png)

### 12. Set Password
![SetUp Password](images/11.SetUpPassword.png)

---

## MFA-First Login Flow with TOTP

This microservice handles **authentication and MFA (TOTP)** using:

- Keycloak (for token issuance and identity provider)
- TOTP (Time-based One-Time Password) as the MFA method
- Custom UI (not using Keycloak login screens)
- `IMemoryCache` for secure temporary state

### 🔐 TOTP Setup (One-time per user)

1. `POST /mfa/setup`  
   → Generates TOTP secret, QR code URI, and setup token  
   → Stores temporary secret in `IMemoryCache`

2. `POST /mfa/verify-setup`  
   → Validates 6-digit code  
   → If correct, stores TOTP secret to database  
   → Removes from cache


### 🔑 MFA Login Flow

1. `POST /auth/login`  
   → Validates username/password via Keycloak  
   → If MFA required:
     - Creates a `setup_token`
     - Stores `username`, `password`, `userId` in cache  
   → Returns `mfa_required = true` or token

2. `POST /mfa/verify-login`  
   → Validates 6-digit TOTP code  
   → If correct, issues Keycloak token using cached login  
   → Returns `access_token`, `refresh_token`

---


### Resolving https access problems

# 1. Connect to the Keycloak database container
```bash
docker exec -it keycloak-db \
  psql -U keycloakuser -d keycloakdb
```

# 2. Inside the psql shell, update the realm to disable SSL requirement
```bash
UPDATE realm SET ssl_required = 'NONE' WHERE name = 'master';
```

# 3. Exit psql (type: \q) and restart the Keycloak container
```bash
docker restart keycloak
```

### Exporting Realm settings

```bash
docker run \
  --rm \
  --name keycloak_exporter \
  --network archium-network \
  -v /Volumes/tor-data/source/repos/archium/authentication-service/exports/:/tmp/keycloak-export:Z \
  -e KC_DB=postgres \
  -e KC_DB_USERNAME=keycloakuser \
  -e KC_DB_PASSWORD=keycloakpass \
  -e KC_DB_URL_HOST=keycloak-db \
  -e KC_DB_URL_DATABASE=keycloakdb \
  -e KC_DB_URL_PORT=5432 \
  quay.io/keycloak/keycloak:26.3.4 \
  export \
  --realm DMSRealm \
  --file /tmp/keycloak-export/realm-export.json \
  --users same_file
  ```