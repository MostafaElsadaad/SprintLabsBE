# Data Model: Admin Community Foundation

## Community

Represents a B2B school or community account.

### Fields

- `Id`: unique identifier
- `Name`: required display name
- `Slug`: required unique URL/business identifier
- `Status`: `Active` or `Suspended`
- `CreatedAt`: required creation timestamp
- `UpdatedAt`: optional last update timestamp

### Relationships

- One Community has zero or more CommunityUsers.
- One Community has zero or one CommunityLicense during staged setup, with a unique license per community.

### Validation Rules

- Name is required and should follow existing max-length conventions for names.
- Slug is required, trimmed, and unique case-insensitively.
- New communities start as `Active`.
- Duplicate slugs are rejected before creating a community.

### State Transitions

- `Active` is the initial state.
- `Suspended` is reserved for future admin workflows; no suspend endpoint is included in this feature.

## CommunityUser

Represents a user's membership in a community.

### Fields

- `Id`: unique identifier
- `CommunityId`: required community identifier
- `UserId`: required user identifier
- `Role`: `Owner`, `Teacher`, or `Student`
- `Status`: `Active`, `Pending`, or `Removed`
- `CreatedAt`: required creation timestamp
- `UpdatedAt`: optional last update timestamp

### Relationships

- Many CommunityUsers belong to one Community.
- Many CommunityUsers reference one User by `UserId`.
- The pair `CommunityId + UserId` is unique.

### Validation Rules

- Community must exist before membership is created or updated.
- User must exist before membership is created or updated.
- Owner assignment sets `Role = Owner` and `Status = Active`.
- Reassigning the same user to the same community updates the existing membership instead of creating a duplicate.
- Teacher and Student role values are modeled but not created by this feature.

### State Transitions

- New owner membership starts as `Active`.
- Existing `Pending` or `Removed` membership for the same user/community can be restored to `Active` owner by owner assignment.

## CommunityLicense

Represents license limits for one community.

### Fields

- `Id`: unique identifier
- `CommunityId`: required community identifier
- `MaxStudents`: non-negative maximum student count
- `UsedStudents`: non-negative used student count
- `MaxTeachers`: non-negative maximum teacher count
- `UsedTeachers`: non-negative used teacher count
- `StudentEmailChangeLimit`: non-negative allowed student email change limit
- `CreatedAt`: required creation timestamp
- `UpdatedAt`: optional last update timestamp

### Relationships

- One CommunityLicense belongs to one Community.
- Each Community has at most one CommunityLicense.

### Validation Rules

- Community must exist before a license is created or updated.
- New license records initialize `UsedStudents = 0` and `UsedTeachers = 0`.
- License limit updates preserve existing used counts.
- `MaxStudents` cannot be lower than `UsedStudents`.
- `MaxTeachers` cannot be lower than `UsedTeachers`.
- All limit values must be non-negative whole numbers.

## User

Existing shared login identity from User Identity Foundation.

### Fields Used By This Feature

- `Id`
- `Email`
- `Name`
- `IsPlatformAdmin`
- `Status`
- `GoogleId`
- `AvatarUrl`

### Relationships

- One User can have zero or more CommunityUsers.

### Validation Rules

- Admin actions require the authenticated user to exist, be active, and have `IsPlatformAdmin = true`.
- Owner assignment finds an existing user by normalized email before creating a new one.
- A user created during owner assignment has no GoogleId, defaults to `Active`, and has `IsPlatformAdmin = false`.
