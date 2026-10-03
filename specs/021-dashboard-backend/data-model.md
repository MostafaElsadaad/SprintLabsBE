# Data model
## CommunityUser
Add TeacherTitle string (max 32), default TEACHER; accepts TEACHER/LEAD_TEACHER. Codes derived from UserId, not a secret/identity credential.
## Notification
Id long; CommunityId; RecipientUserId; SenderUserId; Title max 200; Body max 4000; CreatedAt UTC; IsRead false.
Foreign keys to community/users with restrictive deletes; index CommunityId/RecipientUserId/CreatedAt. Never expose another recipient.
## StaffActivity
Id long; CommunityId; TeacherUserId; ActorUserId; Label max 200; CreatedAt UTC.
Observed management events only; restrictive references and teacher/date index. Latest three plus membership joined timestamp when appropriate.
## Existing entities
Class/grade/license/invitation/user relationships unchanged. No Match/Question model change.
## State rules
Invitation pending → expired (derived), accepted or revoked. Cancel current pending invitation → removed pending teacher membership and release seat once. Superseded ID returns conflict. Repeat cancellation succeeds without new seat release.
Class archive uses existing Deleted; teacher deactivate uses existing Removed; no new inactive account semantics.
