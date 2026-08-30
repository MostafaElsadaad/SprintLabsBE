# Data Model: Trusted Current-Community Resolution

**Feature**: [spec.md](./spec.md)  
**Decision**: Reuse the existing model unchanged; no migration

## Existing Entities

### User

The authenticated identity and canonical serialization key for staff-membership writes.

Relevant existing fields:

| Field | Use in this feature |
|---|---|
| `Id` | Trusted `userId` subject and row locked before a staff membership becomes current. |
| `Status` | Existing login/handler checks continue denying suspended or inactive users. |
| `IsPlatformAdmin` | Authorizes Admin routes only through existing checks; does not resolve a Communities tenant. |
| `IsTeacherAccount` | Existing Teacher identity eligibility; grants no community access by itself. |
| Identity password/email/lockout fields | Existing login eligibility remains unchanged. |

No User field is added or changed. CommunityId is not stored on User and is not added to the JWT.

### Community

The tenant boundary for staff operations.

Relevant existing fields:

| Field | Rule |
|---|---|
| `Id` | Internally populated on commands/queries after trusted resolution. |
| `Name`, `Slug` | Existing profile behavior is preserved. |
| `Status` | A resolved staff community must be `Active`; suspended communities do not grant staff context. |

### CommunityUser

The persisted authorization source of truth connecting a User to a Community.

Relevant existing fields:

| Field | Rule |
|---|---|
| `CommunityId` | Tenant owned by the membership. |
| `UserId` | Authenticated identity linked to the membership. |
| `Role` | `Owner` and `Teacher` are staff; `Student` is not staff. |
| `Status` | `Pending` and `Active` are current for invariant detection; only `Active` grants access; `Removed` is historical. |
| `CreatedAt`, `UpdatedAt` | Existing audit behavior is preserved during create/restore/activate/remove operations. |

Existing constraints/indexes remain:

- Unique `(CommunityId, UserId)` prevents duplicate rows for one user in one community.
- Index on `UserId` supports user membership lookup.
- Composite `(UserId, Role, Status, CommunityId)` supports current staff evaluation.

No plain or conditional unique UserId constraint is added in this feature.

### TeacherInvitation

The one-time setup lifecycle associated with a Pending Owner or Teacher membership.

Relevant rules:

- Issue/reissue may create or restore one compatible Pending membership.
- Completion may change only that Pending membership to Active.
- Combined current staff state is rechecked while the User row is locked before either operation changes membership, invitation, identity, or capacity data.
- Conflicts leave invitation acceptance/revocation and raw-token lifecycle unchanged through rollback.

### CommunityLicense

Existing community capacity record.

Relevant rule: Teacher seat reservation/release behavior remains unchanged. A rejected invariant check must not increment or decrement `UsedTeachers`.

### Community-Owned Resources

`Grade`, `Class`, `StudentLicense`, and student/player associations retain their existing `CommunityId` relationships. Existing handlers continue filtering resource IDs by the internally resolved CommunityId.

## Derived Concepts

### Staff Membership

```text
Role == Owner OR Role == Teacher
```

### Current Staff Membership

```text
Staff Membership AND (Status == Pending OR Status == Active)
```

### Valid Current Staff Community

A User has a valid resolvable staff community only when:

1. Current staff memberships reference exactly one distinct CommunityId.
2. The membership in that community is Active.
3. The Community is Active.

Removed and Student memberships do not participate. Community status is checked after conflict detection so a current staff row in a suspended second community cannot be hidden.

## Invariant

For every User:

```text
COUNT(DISTINCT CommunityId)
WHERE Role IN (Owner, Teacher)
  AND Status IN (Pending, Active)
<= 1
```

All membership writers enforce this rule inside a User-serialized transaction. Read paths fail closed when legacy data violates it.

## Staff Membership State Transitions

| From | Operation | To | Conditions |
|---|---|---|---|
| No target row | Teacher/Owner invitation issue | Pending matching role | No current staff in another community; target role authorized; capacity and identity rules pass. |
| Removed matching role | Invitation issue | Pending matching role | No current staff in another community; existing compatible row reused. |
| Pending matching role | Invitation reissue | Pending matching role | Same community; no other current staff community; prior usable invitation superseded; no extra seat. |
| Pending matching role | Invitation completion | Active matching role | Valid invitation; User lock held; no other current staff community; identity/password rules pass. |
| Pending Teacher | Eligible provider-login activation | Active Teacher | Exactly one current staff community; matching verified email/invitation rule; activation is atomic. |
| No target row | Platform Admin owner assignment | Active Owner | No current staff in another community. |
| Removed Owner | Platform Admin owner assignment | Active Owner | No current staff in another community. |
| Removed incompatible role | Explicit Platform Admin owner assignment | Active Owner | Existing AssignOwner row-reuse behavior; no other current staff community. |
| Active/Pending Owner | Repeated Platform Admin owner assignment | Active Owner | Idempotent same-community assignment. |
| Active/Pending Teacher or Student | Platform Admin owner assignment | Rejected | Current access is not silently overwritten. |
| Pending/Active Teacher | Teacher removal | Removed Teacher | Existing removal, invitation revocation, and seat-release behavior remains. |
| Removed staff in another community | Later authorized assignment/invitation | Pending or Active target role | Removed history does not block reassignment. |

## Authentication Read Decisions

| Current staff state | Communities resolver | Community login (non-admin) | Refresh (non-admin) | Platform Admin login/refresh |
|---|---|---|---|---|
| None | Fail | Generic failure | No rotation | Allowed by existing Admin eligibility. |
| One Pending | Fail | Generic failure | No rotation | Allowed; no staff tenant selected. |
| One Active, Active Community | Resolve | Issue role/account tokens | Rotate with persisted role | Allowed as Admin; Communities access still membership-based. |
| One Active, Suspended Community | Fail | Generic failure | No rotation | Allowed as Admin; no staff tenant selected. |
| Multiple distinct current communities | Fail | Controlled conflict, no tokens | No rotation | Conflict before Admin bypass. |
| One valid Active plus Removed/Student elsewhere | Resolve | Issue role/account tokens | Rotate | Allowed under existing Admin eligibility. |

## Schema Decision

No model or migration change is planned. A conditional generated-column unique index is deferred because it would reject legacy conflicts during deployment, while this feature explicitly retains those records and makes them fail closed until separate remediation.
