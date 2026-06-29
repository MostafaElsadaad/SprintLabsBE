# Quickstart: Owner Student License Management

## Prerequisites

- Database contains at least one community.
- Community has a `CommunityLicense` with `MaxStudents > 0`.
- Test user has Active Owner membership in the community.
- At least one Active grade and Active class exist in the community.
- The class belongs to the selected grade.

## Build and Test

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln
```

## Migration Validation

After implementation, create and inspect the migration:

```powershell
dotnet ef migrations add OwnerStudentLicenseManagement --project Infrastructure --startup-project API
```

Expected migration scope:

- Creates `StudentLicenses`.
- Adds configured foreign keys and indexes.
- Does not alter existing auth, teacher, grade/class, question, or payment tables except model snapshot metadata required by EF.

Apply locally only when ready:

```powershell
dotnet ef database update --project Infrastructure --startup-project API
```

## Manual API Scenarios

Routes use the existing versioned controller shape: `/api/v1/Communities/...`.

### 1. Add Student License

```http
POST /api/v1/Communities/{communityId}/student-licenses
Authorization: Bearer <owner-token>
Content-Type: application/json

{
  "email": "student@example.com",
  "gradeId": 1,
  "classId": 1
}
```

Expected:

- Response contains Pending license.
- `emailChangeCount` is `0`.
- `assignedByUserId` is the Owner user id.
- `CommunityLicense.UsedStudents` increments by `1`.
- No Student `CommunityUser` is created.

### 2. Capacity Rejection

Set `UsedStudents == MaxStudents`, then repeat add.

Expected:

- Request is rejected.
- No `StudentLicense` is created.
- `UsedStudents` is unchanged.

### 3. Duplicate Email Rejection

Add a Pending license for `student@example.com`, then add ` Student@Example.com ` again.

Expected:

- Second request is rejected.
- Only one non-revoked license exists for the normalized email.

### 4. List Licenses

```http
GET /api/v1/Communities/{communityId}/student-licenses?status=Pending&gradeId=1&classId=1&search=student
Authorization: Bearer <owner-token>
```

Expected:

- Only route-community licenses matching filters are returned.
- Response includes license id, email, status, grade summary, class summary, email change count, assigned-by summary/id, activation date, and created date.

### 5. Update Pending License

```http
PATCH /api/v1/Communities/{communityId}/student-licenses/{licenseId}
Authorization: Bearer <owner-token>
Content-Type: application/json

{
  "email": "new-student@example.com",
  "gradeId": 1,
  "classId": 1
}
```

Expected:

- Pending license email changes if within `StudentEmailChangeLimit`.
- `EmailChangeCount` increments only when normalized email changes.
- Grade/class updates when both belong to the community and class belongs to grade.
- `UsedStudents` is unchanged.

### 6. Reject Invalid Grade/Class

Use a grade from another community, a class from another community, a deleted class, or a class under a different grade.

Expected:

- Request is rejected.
- License and `UsedStudents` remain unchanged.

### 7. Revoke Pending License

```http
DELETE /api/v1/Communities/{communityId}/student-licenses/{licenseId}
Authorization: Bearer <owner-token>
```

Expected:

- License status becomes Revoked.
- License row remains stored.
- `UsedStudents` decreases by `1`.

### 8. Revoke Active License With Student Access

Prepare an Active license with linked `UserId` and a matching active `CommunityUser` where `Role = Student`.

Expected:

- License status becomes Revoked.
- `UsedStudents` decreases by `1`.
- Matching Student community access is set to Removed.
- Owner/Teacher roles and other community memberships for that user are unchanged.

### 9. Authorization Checks

Repeat add/list/update/revoke as:

- Active Teacher
- Active Student
- Pending Owner
- Removed Owner
- Platform admin without Owner membership
- No membership

Expected:

- Requests are denied.

## Out-of-Scope Confirmation

Manual validation should not require:

- Student login activation
- Email sending
- Bulk import
- Student dashboard
- Analytics
- Payments
- Parent accounts
- Teacher management
- Grade/class management
