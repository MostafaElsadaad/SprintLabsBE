# Data Model: Owner Community Profile

## Overview

This feature uses the existing `Community`, `CommunityUser`, and `User` data. It introduces no tables, columns, indexes, relationships, or migrations.

## Community

Represents the profile being viewed or edited.

| Field | Type | Use in this feature | Validation |
|---|---|---|---|
| Id | 64-bit identifier | Route/resource identity | Must be positive |
| Name | String | Returned and updated | Required, trimmed, existing maximum 200 characters |
| Slug | String | Returned and updated | Required, trimmed, lowercase, unique, existing maximum 120 characters |
| Status | CommunityStatus | Returned read-only | Existing values: Active, Suspended |
| CreatedAt | Timestamp | Unchanged | Existing behavior |
| UpdatedAt | Nullable timestamp | Set on successful PATCH | Current UTC timestamp |

### Existing Constraints

- `Slug` has a unique database index.
- A community can have many memberships.
- No profile update in this feature changes Status.

## CommunityUser

Represents authorization for the exact user/community pair.

| Field | Use in this feature |
|---|---|
| CommunityId | Must match the requested community |
| UserId | Must match the authenticated user |
| Role | Owner permits PATCH; Owner, Teacher, and Student permit GET |
| Status | Must be Active for either operation |

### Authorization Matrix

| Membership | GET profile | PATCH profile |
|---|---:|---:|
| Active Owner | Allow | Allow |
| Active Teacher | Allow | Deny |
| Active Student | Allow | Deny |
| Pending Owner/Teacher/Student | Deny | Deny |
| Removed Owner/Teacher/Student | Deny | Deny |
| No membership | Deny | Deny |
| Platform admin without membership | Deny | Deny |

## User

Represents the authenticated identity.

| Condition | Outcome |
|---|---|
| User exists and is Active | Continue to community authorization |
| User does not exist | Controlled not-found outcome |
| User is Suspended | Controlled forbidden outcome |

## Update Rules

1. Validate the authenticated user and positive community id.
2. Require an Active Owner membership.
3. Trim the submitted name.
4. Trim and lowercase the submitted slug.
5. Reject an empty normalized name or slug.
6. Reject a matching slug on any other community.
7. Allow the same normalized slug on the current community.
8. Update Name, Slug, and UpdatedAt together.
9. Save once so a rejected request does not partially change the stored profile.

## State Transitions

This feature does not change community or membership statuses.

The only state transition is:

```text
Existing Community Profile
    -> valid Active Owner update
Updated Name + Updated Slug + UpdatedAt
```

All failed authorization or validation paths leave the community unchanged.
