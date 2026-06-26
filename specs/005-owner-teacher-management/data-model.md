# Data Model: Owner Teacher Management

## Overview

This feature uses the existing `User`, `Community`, `CommunityUser`, and `CommunityLicense` data. It introduces no new tables, columns, indexes, relationships, or migrations unless implementation discovers the existing schema does not contain the specified fields.

## User

Represents the login identity invited as a teacher.

| Field | Use in this feature | Validation |
|---|---|---|
| Id | Target teacher identity and owner identity | Existing identifier |
| Email | Invite lookup and login activation match | Required, trimmed, compared case-insensitively |
| Name | Created or displayed teacher name | Required for invited user creation |
| IsPlatformAdmin | Must remain false for invite-created users | Invite must not grant admin access |
| Status | Existing account status behavior | Existing authentication/user checks apply |

### User Rules

- Invite finds users by normalized email.
- Invite creates a basic user only when no user exists for the normalized email.
- Created teacher users default to active, non-platform-admin identity records according to existing user conventions.
- Login activation uses the resolved user id after Google email lookup or creation.

## CommunityUser

Represents a user's membership in a community.

| Field | Use in this feature | Validation |
|---|---|---|
| Id | Membership identity | Existing identifier |
| CommunityId | Target community scope | Must match route community id |
| UserId | Teacher or owner identity | Must refer to existing user |
| Role | Teacher-management target and Owner authorization | Owner manages; Teacher is created/listed/removed |
| Status | Pending invite, Active teacher, Removed teacher | Pending/Active count as used teacher seats; Removed does not |
| CreatedAt | Returned in teacher list | Existing timestamp |
| UpdatedAt | Updated on membership status changes if available | Existing timestamp behavior |

### Membership State Transitions

```text
No membership
    -> invite with capacity
Pending Teacher

Removed Teacher
    -> re-invite with capacity
Pending Teacher

Pending Teacher
    -> matching Google login
Active Teacher

Pending Teacher or Active Teacher
    -> owner remove
Removed Teacher
```

### Membership Rules

- The target membership role for invite, list, activation, and removal is `Teacher`.
- Owner memberships are never removed through this teacher-removal flow.
- Duplicate `CommunityUser` rows for the same user/community are not created.
- Removed Teacher memberships do not grant active community access.
- Pending Teacher memberships do not grant active community access until activation.

## CommunityLicense

Represents teacher capacity for a community.

| Field | Use in this feature | Validation |
|---|---|---|
| CommunityId | License scope | Must match route community id |
| MaxTeachers | Teacher seat capacity | Invite/restore rejected when usage is at or above this value |
| UsedTeachers | Pending plus Active teacher seats | Cannot exceed MaxTeachers or drop below zero |
| MaxStudents / UsedStudents | Unchanged | Out of scope |
| StudentEmailChangeLimit | Unchanged | Out of scope |

### Seat Accounting Rules

| Event | UsedTeachers change |
|---|---:|
| New Pending Teacher membership created | +1 |
| Removed Teacher restored to Pending | +1 |
| Duplicate invite for existing Pending Teacher | 0 |
| Duplicate invite for existing Active Teacher | 0 |
| Pending Teacher activated on login | 0 |
| Pending Teacher removed | -1 |
| Active Teacher removed | -1 |
| Removed Teacher removed again | 0 |

### Capacity Rules

- Invite and restore require `UsedTeachers < MaxTeachers`.
- Missing license capacity rejects invite/restore.
- Updates must not allow `UsedTeachers` to become negative.
- Updates must not allow `UsedTeachers` to exceed `MaxTeachers`.

## Community

Represents the school/community where teacher memberships are managed.

| Field | Use in this feature |
|---|---|
| Id | Route/resource identity |
| Name | Optional context in tests/docs; not changed |
| Slug | Optional context in tests/docs; not changed |
| Status | Not changed by this feature |

This feature does not change community profile data or community status.

## Authorization Matrix

| Requesting membership | Invite teacher | List teachers | Remove teacher |
|---|---:|---:|---:|
| Active Owner | Allow | Allow | Allow |
| Active Teacher | Deny | Deny | Deny |
| Active Student | Deny | Deny | Deny |
| Pending Owner | Deny | Deny | Deny |
| Removed Owner | Deny | Deny | Deny |
| No membership | Deny | Deny | Deny |
| Platform admin without Active Owner membership | Deny | Deny | Deny |

All owner-facing operations derive the requester from the authenticated token and never accept requester user id from the client.
