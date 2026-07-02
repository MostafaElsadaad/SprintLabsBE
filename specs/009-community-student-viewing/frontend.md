# Community Student Viewing Frontend Guide

## Required Frontend Pages or Sections

### Community Students List

Show a roster table for community Owners and Teachers.

Expected table columns:

| Column | Source |
|---|---|
| Student email | `email` |
| Name | `playerName`, nullable |
| Status | `status` |
| Grade | `gradeName` |
| Class | `className` |
| Activated | `activatedAt`, nullable |
| Created | `createdAt` |

### Student Detail

Show profile, license placement, progression, and placeholder analytics for an activated student with a player profile.

Expected sections:

| Section | Fields |
|---|---|
| Profile | `name`, `email`, `avatarUrl` |
| Progression | `gold`, `experience`, `level` |
| Placement | `licenseStatus`, `gradeName`, `className`, `activatedAt`, `createdAt` |
| Analytics | Placeholder values only |

## User Actions

| Action | API |
|---|---|
| Open student roster | `GET /api/v1/Communities/{communityId}/students` |
| Filter roster | Same endpoint with `gradeId`, `classId`, `status`, `search` |
| Change page | Same endpoint with `pageNumber`, `pageSize` |
| Open student detail | `GET /api/v1/Communities/{communityId}/students/{playerProfileId}` |

## Forms and Fields

List filters:

| Field | Rules |
|---|---|
| Grade | Optional; choose from route community grades. |
| Class | Optional; choose from route community classes; class must match selected grade when grade is selected. |
| Status | Optional; `Pending`, `Active`, `Revoked`. |
| Search | Optional text; matches email/name fields where available. |
| Page number | Positive integer. |
| Page size | Positive integer. |

No create, update, revoke, import, export, assignment, report, dashboard, or analytics configuration UI is part of this feature.

## Loading, Empty, and Error States

- Show table loading while the roster request is pending.
- Show an empty roster state when the paginated response has zero records.
- Show filter-empty state separately when filters return no matches.
- For Pending students, allow blank name/avatar/detail action because `playerProfileId` may be null.
- Disable or hide detail navigation when `playerProfileId` is null.
- On 401, clear session or route to sign-in.
- On 403, show no-access messaging for the selected community.
- On 404 from detail, show student unavailable or refresh the roster.

## Permissions and Visibility Rules

- Only Active Owners and Active Teachers can view roster and detail.
- Students, Pending members, Removed members, no-membership users, and platform-admin-only users should not see student viewing navigation.
- Frontend visibility is advisory only; backend remains the source of truth.

## Open Questions

- None for this feature. Real analytics display remains a future feature.
