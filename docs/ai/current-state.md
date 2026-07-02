# SprintLabs Current State

## Project
SprintLabs backend, .NET Core API, CQRS/MediatR, EF Core/MySQL, repository pattern, BaseResponse style.

## AI workflow
Use Spec Kit + Codex skills + AGENTS.md.
For each feature:
1. Create branch
2. Run $speckit-specify
3. Run $speckit-plan
4. Run $speckit-tasks
5. Implement from spec/plan/tasks
6. Build/test
7. Commit

## Completed multi-tenancy features

### 001 User Identity Foundation
- Added Users table.
- Linked Player/Profile to User.
- Google login creates/finds User.
- Login returns UserId and PlayerProfileId.
- Added /api/users/me.
- Added /api/users/me/player-profile.

### 002 Admin Community Foundation
- Added Communities.
- Added CommunityUsers.
- Added CommunityLicenses.
- Platform admin uses Users.IsPlatformAdmin.
- Added admin community create/list.
- Added owner assignment.
- Added license limit update.

### 003 Community Access Foundation
- Added community access service.
- CanAccessCommunity(userId, communityId).
- HasCommunityRole(userId, communityId, roles).
- Added /api/users/me/communities.

### 004 Owner Community Profile
- Added GET /api/communities/{communityId}.
- Added PATCH /api/communities/{communityId}.
- GET requires active community membership.
- PATCH requires active Owner role.

### 005 Owner Teacher Management
- Owner can invite teachers.
- Owner can list teachers.
- Owner can remove teachers by setting status Removed.
- Teacher seats use CommunityLicense MaxTeachers/UsedTeachers.
- Pending + Active teachers count as UsedTeachers.
- Teacher pending membership activates on Google login.
- Existing logged-in teacher invite should become Active immediately.

### 006 Community Grades and Classes
- Added Grades.
- Added Classes.
- Owner/Teacher can create/list grades.
- Owner/Teacher can create/list/update/delete classes.
- Classes are soft deleted.
- Grade/class must belong to route community.

### 007 Owner Student License Management
- Added StudentLicenses.
- Owner can add/list/update/revoke student licenses.
- Pending + Active student licenses count as UsedStudents.
- Revoked licenses do not count.
- Grade/class validation is required.
- Pending email can be changed within StudentEmailChangeLimit.
- Revoke sets status Revoked and decrements UsedStudents once.

### 008 Student License Activation on Login
- Google login detects pending StudentLicenses by email.
- Creates/finds PlayerProfile.
- Activates StudentLicense.
- Sets UserId, PlayerProfileId, ActivatedAt.
- Creates/restores CommunityUser Role=Student Status=Active.
- UsedStudents is not incremented during activation.
- Existing logged-in student added later should activate immediately during student license creation.

## Important business rules
- Backend never trusts frontend role claims.
- Platform admin = Users.IsPlatformAdmin.
- Community access = Active CommunityUser.
- Owner/Teacher/Student permissions come from CommunityUser.Role + Status.
- Pending/Removed memberships do not grant access.
- Student/teacher invite capacity is reserved when invite/license is created, not when login activation happens.
- Login activation must never increment UsedStudents or UsedTeachers again.

## Current next feature
Continue after student activation flow. Next likely feature should be student listing or student access endpoints, depending on roadmap.

## Commands
Build:
dotnet build

Apply migration:
dotnet ef database update

Check repo:
git status