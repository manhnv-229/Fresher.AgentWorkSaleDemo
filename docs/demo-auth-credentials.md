# Demo Auth Credentials

These accounts are seeded by `DatabaseSeeder` for local development only.

| Email | Password | Notes |
| --- | --- | --- |
| `admin@example.com` | `Password123!` | Global `SystemAdmin` |
| `manager@example.com` | `Password123!` | `AgentManager` in Tenant One, `AgentViewer` in Tenant Two |
| `viewer@example.com` | `Password123!` | `AgentViewer` in Tenant Two |

Seeded password values are stored as BCrypt hashes in the database.
