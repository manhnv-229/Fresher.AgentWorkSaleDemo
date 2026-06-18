## 1. Tenant Create Entry Point

- [ ] 1.1 Update the tenant workspace actions in the agent catalog UI to show a create-agent button when a tenant is selected.
- [ ] 1.2 Make the tenant create modal title and labels clearly reflect that the new agent will belong to the selected tenant.

## 2. Context-Aware Create Flow

- [ ] 2.1 Bind tenant create modal state to the current selected tenant instead of asking the user to choose tenant scope again.
- [ ] 2.2 Reuse the existing `POST /api/tenants/{tenantId}/agents` flow so tenant create requests always target the selected tenant id.
- [ ] 2.3 Keep the internal create flow isolated so internal-agent creation still uses the internal endpoint only.

## 3. Post-Create Refresh and Verification

- [ ] 3.1 Refresh the selected tenant's current filtered/paged agent list after a successful tenant-scoped create.
- [ ] 3.2 Verify the selected tenant remains active after create and the new agent appears in that tenant's list.
- [ ] 3.3 Verify tenant-scoped creation does not place the new agent in internal results or another tenant's results.
