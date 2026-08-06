# SprintLabs Authentication and Invitation Test Guide

Import `SprintLabs.Authentication.postman_collection.json` and set the secret password/token variables locally. Do not commit credentials or tokens.

1. Log in as the seeded Platform Admin with `POST /api/v1/Account/community-login`; verify `data.role` is `PlatformAdmin` and no `community` is returned.
2. Create a community through `POST /api/v1/admin/communities` using the Platform Admin access token. The response saves `data.id` as `communityId`.
3. Copy the owner invitation token from Mailpit and complete it through `POST /api/v1/Account/community-register` with token, name, and password.
4. Configure the community license through `PATCH /api/v1/admin/communities/{communityId}/licenses`.
5. Log in through `community-login` with the new Community Admin credentials; verify role `CommunityAdmin`, then invite a teacher through `POST /api/v1/Communities/teachers/invite` (no community ID is sent).
6. Copy the teacher invitation token from Mailpit and use the same `community-register` endpoint. The teacher then logs in through `community-login` with role `Teacher`.

All three login types use the same hashed refresh-token rotation and logout endpoints. Mailpit and API responses provide everything required; database credentials are not needed for this flow.
