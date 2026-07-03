# Data Model: B2C Player Profile Support

## Existing Entities Used

### User

Represents the authenticated login identity.

Relevant fields:

| Field | Use |
|-------|-----|
| Id | Current user id from authentication claims |
| GoogleId | Google identity link created/found during login |
| Email | Login identity and profile display |
| Name | Login identity and profile display |
| AvatarUrl | Optional profile display |
| Status | Suspended users remain denied |

Validation rules:

- B2C login creates or finds a User.
- User does not need a CommunityUser membership for B2C login/profile access.
- User does not need a StudentLicense for B2C login/profile access.

### PlayerProfile / Player

Represents the game profile linked to a User.

Relevant fields:

| Field | Use |
|-------|-----|
| Id | Player profile identifier returned to clients |
| UserId | Links the profile to the authenticated user |
| GoogleId | Existing login/profile linkage |
| Email | Profile display |
| Name | Profile display |
| AvatarUrl | Optional profile display |
| Age | Nullable editable field |
| Grade | Nullable editable field |
| SchoolName | Nullable editable field |
| Gold | Existing progression display |
| Experience | Existing progression display |
| Level | Existing progression display |
| CreatedAt | Existing audit field |
| UpdatedAt | Updated when profile is edited |

Validation rules:

- Current player profile reads must find by authenticated user id.
- Current player profile updates must find by authenticated user id.
- Updates must not accept a profile id or user id from the client.
- Age, Grade, and SchoolName may be null.
- Clearly invalid Age and Grade values must be rejected.

### Community Membership

Represents B2B community access.

Validation rules:

- B2C login, player profile read, and player profile update must not require this entity.
- This feature must not create or modify community memberships.

### Student License

Represents a B2B student seat and activation source.

Validation rules:

- B2C login, player profile read, and player profile update must not require this entity.
- This feature must not create or modify student licenses.

### Analytics Context

Future reporting boundary rather than a stored entity in this feature.

| Context | CommunityId |
|---------|-------------|
| B2C gameplay/profile activity | null |
| B2B school activity | route or membership community id |

Validation rules:

- No analytics tables, events, or calculations are added in this feature.
- Future analytics designs should not assume `CommunityId` is required for every game activity.

## Relationships

```text
User 1 -> 0..1 PlayerProfile
User 1 -> 0..* CommunityMembership
Community 1 -> 0..* StudentLicense
StudentLicense 0..1 -> User
StudentLicense 0..1 -> PlayerProfile
```

## State Transitions

### B2C Google Login

```text
No User/Profile -> User + PlayerProfile
Existing User without PlayerProfile -> Existing User + new PlayerProfile
Existing User/Profile -> Existing User/Profile reused
```

No CommunityUser or StudentLicense transition is required for B2C login.

### Profile Update

```text
PlayerProfile(age, grade, schoolName) -> PlayerProfile(updated age, grade, schoolName)
```

Only the current user's PlayerProfile can transition.

## Persistence Notes

- No new table is required.
- No migration is required because current `Player` storage already has `Age`, `Grade`, and `SchoolName`.
- Existing B2B activation and community access tables remain unchanged.
