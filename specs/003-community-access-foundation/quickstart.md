# Quickstart: Community Access Foundation

## Prerequisites

- Admin Community Foundation schema is applied.
- At least one community exists.
- At least one authenticated user has a JWT with the existing `userId` claim.
- The user has CommunityUser rows covering Active, Pending, and Removed states for validation.

## Build Check

```powershell
dotnet build SprintLabs.sln
```

Expected outcome: solution builds successfully.

## Validation Scenarios

Use the current user JWT as:

```text
Authorization: Bearer <user-token>
```

### 1. Unauthenticated request is rejected

```http
GET /api/v1/Users/me/communities
```

Expected outcome without token: 401.

### 2. Active memberships are returned

Create Active Owner, Teacher, and Student memberships for the authenticated user in different communities.

```http
GET /api/v1/Users/me/communities
```

Expected outcome: response includes those communities with community id, name, slug, community status, role, and `membershipStatus = Active`.

### 3. Pending and Removed memberships are excluded

Create Pending and Removed memberships for the authenticated user.

Expected outcome: those communities do not appear in the response.

### 4. Other users' memberships are excluded

Create an Active membership for another user.

Expected outcome: the other user's community does not appear for the authenticated user.

### 5. Reusable access checks

Call the community access check from a focused test or future handler scenario:

- Active membership returns true.
- Pending membership returns false.
- Removed membership returns false.
- Missing membership returns false.

### 6. Reusable role checks

Call the community role check from a focused test or future handler scenario:

- Active Owner passes required Owner.
- Active Teacher passes required Teacher or Owner/Teacher set.
- Active Student fails required Owner.
- Pending or Removed role membership fails even when role matches.
- Empty required role list fails.
