# Owner Teacher Management API

## Feature Summary

This feature lets active community Owners invite, list, and remove teacher memberships for their community. It also activates Pending Teacher memberships during Google login when the resolved user matches the invited email.

No email delivery, teacher dashboard, student management, grades, classes, analytics, payments, or admin teacher-management APIs are included.

## Endpoints

| Method | Route | Required access |
|---|---|---|
| POST | `/api/v1/Communities/{communityId}/teachers/invite` | Authenticated user with Active Owner membership |
| GET | `/api/v1/Communities/{communityId}/teachers` | Authenticated user with Active Owner membership |
| DELETE | `/api/v1/Communities/{communityId}/teachers/{userId}` | Authenticated user with Active Owner membership |

All successful teacher-management responses use `BaseResponse<T>`.

## POST Invite Teacher

Request:

```json
{
  "email": "teacher@example.com",
  "name": "Teacher Name"
}
```

Behavior:

- Finds or creates a User by normalized email.
- Creates a `CommunityUser` with `Role = Teacher`.
- Uses `Status = Active` when the invited User already has a Google login identity.
- Uses `Status = Pending` when the invite creates or reuses a placeholder User without a Google login identity.
- Restores a Removed Teacher membership to Active or Pending based on the User's Google login identity when license capacity allows.
- Does not duplicate an existing Pending or Active Teacher membership.
- Increments `UsedTeachers` only when a new counted seat is created or restored.
- Rejects invite when `UsedTeachers >= MaxTeachers` or no license exists.

Success:

```json
{
  "data": {
    "userId": 20,
    "name": "Teacher Name",
    "email": "teacher@example.com",
    "status": "Pending",
    "createdAt": "2026-06-26T10:15:00Z"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

## GET List Teachers

No request body.

Success:

```json
{
  "data": [
    {
      "userId": 20,
      "name": "Teacher Name",
      "email": "teacher@example.com",
      "status": "Pending",
      "createdAt": "2026-06-26T10:15:00Z"
    }
  ],
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

The list returns Teacher memberships for the requested community only. Removed teachers are included with `status = "Removed"` so the frontend can distinguish removed access from active or pending seats.

## DELETE Remove Teacher

No request body.

Behavior:

- Finds the target Teacher membership by community id and user id.
- Rejects non-Teacher memberships, including Owners.
- Sets status to `Removed`.
- Decrements `UsedTeachers` only when the previous status was Pending or Active.
- Does not hard delete the membership.
- Repeating removal of an already Removed teacher does not decrement again.

Success:

```json
{
  "data": {
    "userId": 20,
    "name": "Teacher Name",
    "email": "teacher@example.com",
    "status": "Removed",
    "createdAt": "2026-06-26T10:15:00Z"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

## Google Login Activation

`POST /api/v1/Account/google-login` keeps the existing login contract. After the User is found or created by Google email, Pending Teacher memberships for that user are changed to Active.

Activation does not change `UsedTeachers`, because the Pending invite already reserved the seat.

## Common Errors

| HTTP | Meaning |
|---|---|
| 400 | Invalid id/input, invalid email/name, full teacher capacity, missing capacity, or non-Teacher target |
| 401 | Missing or invalid bearer token |
| 403 | Suspended user or authenticated user lacks Active Owner membership |
| 404 | Current user, target teacher user, membership, or required license not found |

Controlled errors use the existing `GenericException` and global error response style. Membership and role failures currently use `InvalidAccessToken`.

## Frontend Usage Notes

- Use the current membership's `communityId`; never send requester user id.
- Show teacher management only for Active Owner memberships.
- Still handle 403 because ownership can change server-side.
- Treat duplicate invite success as idempotent.
- Re-inviting a Pending Teacher who has since logged in may return `Active` without consuming another seat.
- Treat `Pending` and `Active` teachers as consuming seats.
- Treat `Removed` teachers as no access.

## Manual Test Steps

Follow [quickstart.md](./quickstart.md).

## Open Questions

- None for this implementation. Removed teachers are included in the list because the feature requests teacher status visibility.

## Implementation Notes / Spec Differences

- The implemented route is versioned and controller-based: `/api/v1/Communities/...`.
- No migration was added because the existing schema already contained the required entities, fields, relationships, and indexes.
- No custom repository or new permission framework was added.
