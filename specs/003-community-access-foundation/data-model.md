# Data Model: Community Access Foundation

No new database tables are planned. This feature uses existing entities from Admin Community Foundation.

## Community

Existing B2B school/community account.

### Fields Used

- `Id`: community identifier
- `Name`: display name
- `Slug`: stable community slug
- `Status`: Active or Suspended

### Validation Rules

- Current-user community list returns community details only through the authenticated user's Active CommunityUser membership.
- Community status is returned as data; this feature does not add community-status gating beyond membership access.

## CommunityUser

Existing membership connecting a user to a community.

### Fields Used

- `CommunityId`
- `UserId`
- `Role`: Owner, Teacher, or Student
- `Status`: Active, Pending, or Removed

### Validation Rules

- Access succeeds only when `Status = Active`.
- Pending and Removed memberships fail access checks.
- Role checks require `Status = Active` and at least one matching required role.
- Role checks with no required roles fail.
- Current-user community list returns only Active memberships.

## User

Existing shared login identity.

### Fields Used

- `Id`
- authenticated user id from the current request

### Validation Rules

- Current-user community list must use the authenticated user's id.
- One user's memberships must not be returned to another user.
- `IsPlatformAdmin` is not used as a community role substitute.

## Community Access Decision

Reusable decision for whether a user can access a community.

### Inputs

- `UserId`
- `CommunityId`

### Output

- true only when an Active CommunityUser exists for the user/community pair.

## Community Role Decision

Reusable decision for whether a user has an allowed role in a community.

### Inputs

- `UserId`
- `CommunityId`
- one or more required roles

### Output

- true only when an Active CommunityUser exists and the membership role is in the required role set.
