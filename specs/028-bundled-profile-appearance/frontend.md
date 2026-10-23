# Bundled profile appearance client integration

Implemented backend contract: [api.md](api.md). Images belong to the Unity project under `Assets/_Project/Backend_Data/Avatar` and `Assets/_Project/Backend_Data/ProfilePic` and must be included in the game build through its local asset catalogue.

## Profile section and actions

The existing authenticated profile section presents independent avatar and profile-picture selections. No table columns or additional dashboard pages are required. The existing gameplay character/model selection remains a separate concern.

| User action | API call | Fields |
| --- | --- | --- |
| Open/refresh profile | GET `/api/v1/Users/me/player-profile` | Read both selected paths alongside existing profile fields. |
| Save avatar | POST `/api/v1/PlayerProfiles/me/appearance` | `avatarAssetPath` with the exact local catalogue path; omit the other choice to preserve it. |
| Save profile picture | POST `/api/v1/PlayerProfiles/me/appearance` | `profilePictureAssetPath` with the exact local catalogue path; omit the other choice to preserve it. |
| Save both | Same POST, or existing PATCH `/api/v1/PlayerProfiles/me` | Supply both fields. |
| Clear a selection | Same POST/PATCH | Explicit JSON `null` for that field; omit the other field. |

Only the signed-in user's profile is editable. Do not send an editable identity field, XP, gold, or progression. The server derives the owner from the bearer credential. Suspended accounts cannot access this feature. B2C players do not need a school/community membership.

## Validation and display

Send complete project-relative paths with forward slashes and exact root capitalization. Use only the respective image folder, maximum 512 characters, and png/jpg/jpeg extensions; the API document specifies supported nested segment characters. Empty string is invalid; use null for reset. No file picker, upload, OS path, or remotely hosted image is part of this feature.

Resolve each returned selection through the game's bundled catalogue. A valid server selection may be absent in an older client build. Treat an unknown/missing local entry as an unavailable selection and use the existing local/default presentation fallback. For the profile picture, retain `pictureUrl` as the existing remote fallback when no bundled selection can be resolved. Do not rewrite the remote URL field with a project path.

## Loading, empty, and error states

- Load current profile before showing a persisted selection; null means no custom selection.
- While saving, show pending feedback and avoid overlapping writes to the same profile.
- Adopt selections from the successful response or refresh the profile after saving. A failed save must not be represented as persisted.
- On 400 keep the last saved state and show the selection validation failure.
- On 401 follow the existing sign-in/session-expiry flow; on 403 show the existing suspended-account state.
- On 404 show the existing unavailable-profile state. Network/server failures should permit retry without clearing saved selections.

Backend checks cover HTTP save/read, persistence through a new relational context, omitted/null semantics, root/path validation, ownership, authentication and progression preservation. Actual Unity build inclusion and visual layout are verified in the Unity portion of this work.
