# Data Model: Community Student Viewing

## Existing Entities Used

### StudentLicense

The source of truth for the community student roster.

Relevant fields:

| Field | Use |
|-------|-----|
| Id | Returned as `licenseId` in list responses |
| CommunityId | Required tenant boundary for list and detail |
| Email | Student roster email and searchable pending-student identity |
| UserId | Nullable linked user identity for activated students |
| PlayerProfileId | Nullable linked player profile for activated students and detail lookup |
| GradeId | Grade filter and display relationship |
| ClassId | Class filter and display relationship |
| Status | List filter and detail license status |
| ActivatedAt | Nullable activation date |
| CreatedAt | Roster created date |

Validation rules:

- List queries must restrict records to the route `CommunityId`.
- Pending, Active, and Revoked licenses can appear in the list unless a status filter is provided.
- Detail queries must use a non-revoked license in the route community linked to the requested `PlayerProfileId`.
- Student viewing must not change any `StudentLicense` field.

### User

The optional login identity linked to an activated student license.

Relevant fields:

| Field | Use |
|-------|-----|
| Id | Returned as nullable `userId` |
| Email | Search and detail/list display fallback when linked |
| Name | Search and display fallback when linked |
| AvatarUrl | Nullable display field when player avatar is unavailable |

Validation rules:

- User data is optional for pending licenses.
- Missing user data must not exclude a pending student from list results.

### Player

The optional game profile linked to an activated student license.

Relevant fields:

| Field | Use |
|-------|-----|
| Id | Returned as nullable `playerProfileId`; used as detail route identifier |
| UserId | Supports linked identity consistency checks when available |
| Email | Search/display fallback when available |
| Name | Search and detail display |
| AvatarUrl | Nullable display field |
| Gold | Detail progression field |
| Experience | Detail progression field |
| Level | Detail progression field |

Validation rules:

- Player data is optional for pending licenses.
- Detail cannot return a player unless a matching non-revoked student license in the route community links to that player.

### Grade

The community-owned grade used for filtering and display.

Relevant fields:

| Field | Use |
|-------|-----|
| Id | Filter and response `gradeId` |
| CommunityId | Validates grade belongs to route community |
| Name | Response `gradeName` |

Validation rules:

- When `gradeId` is supplied, it must belong to the route community.

### Class

The community-owned class used for filtering and display.

Relevant fields:

| Field | Use |
|-------|-----|
| Id | Filter and response `classId` |
| CommunityId | Validates class belongs to route community |
| GradeId | Validates class belongs to supplied grade when both filters are present |
| Name | Response `className` |
| Status | Ensures deleted classes are not accepted as filters |

Validation rules:

- When `classId` is supplied, it must belong to the route community.
- Deleted classes must not be accepted as valid list filters.
- When both `gradeId` and `classId` are supplied, class `GradeId` must match `gradeId`.

### CommunityUser

The membership record used for authorization.

Relevant fields:

| Field | Use |
|-------|-----|
| CommunityId | Must match the route community |
| UserId | Must match the authenticated user |
| Role | Must be Owner or Teacher |
| Status | Must be Active |

Validation rules:

- Owner and Teacher can view student list/detail only when membership is Active.
- Student, Pending, Removed, no-membership, and platform-admin-only users are denied.

### StudentAnalyticsPlaceholder

A response-only object reserved for future analytics.

Fields should use null, zero, or default values only. Suggested initial contract:

| Field | Value |
|-------|-------|
| completedAssignments | 0 |
| averageScore | null |
| lastActivityAt | null |
| matchesPlayed | 0 |
| winRate | null |

Validation rules:

- No analytics calculations are performed in this feature.
- Placeholder values must be stable and clearly non-authoritative.

## Relationships

```text
Community 1 -> 0..* StudentLicense
StudentLicense 0..1 -> User
StudentLicense 0..1 -> Player
StudentLicense * -> 1 Grade
StudentLicense * -> 1 Class
Grade 1 -> 0..* Class
Community 1 -> 0..* CommunityUser
User 1 -> 0..* CommunityUser
```

## Query Behavior

### Student List

- Start from `StudentLicense` filtered by route `CommunityId`.
- Validate optional grade/class filters before applying them.
- Apply optional status, grade, class, and search filters.
- Search must include license email and, when linked, user email, user name, and player name.
- Page the filtered result using the existing `PagedRequest`/`PagedResponse` behavior.
- Project to DTO fields only.

### Student Detail

- Start from `StudentLicense` filtered by route `CommunityId`, requested `PlayerProfileId`, and non-revoked status.
- Include or join Grade, Class, Player, and User data needed for the DTO.
- Return not found when no matching student license exists.
- Return placeholder analytics only.

## Persistence Notes

- No new table is required.
- No migration is required.
- No entity data is created, updated, revoked, activated, or deleted by this feature.
