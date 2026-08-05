# Authentication Frontend Handoff

Use `POST /api/v1/Account/community-login` for every password sign-in. The form has only identifier and password. Handle the returned trusted public role as follows: `PlatformAdmin` has no community, while `CommunityAdmin` and `Teacher` include one community.

Community Admin and Teacher invitation pages both submit `token`, `name`, and `password` to `POST /api/v1/Account/community-register`. The token decides the role and community; never include them in the request. On success, redirect to the shared login page because registration returns no session.

Show a generic sign-in error for HTTP 401, the safe lockout state for HTTP 423, and a generic expired/invalid invitation state for registration errors. Keep existing Google, refresh-token, logout, forgot-password, and reset-password experiences unchanged.
