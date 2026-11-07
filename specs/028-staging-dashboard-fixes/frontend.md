# Dashboard integration handoff

Frontend checkout: `K:/Projects/react/sprint-labs-dashboard`, branch
`codex/staging-dashboard-fixes`. Backend changes are in the clean publication
worktree, preserving unrelated edits in the original backend checkout.

Deploy the dashboard with `/dashboard/` as its Vite base and router basename.
New email links target `/auth/register` and `/auth/reset-password` under that base.
The dashboard also accepts the old `/invitations/teacher/setup`,
`/invitations/community-admin/setup` and `/reset-password` routes for compatible
existing links that already have the correct host and base path.

Send `teacherId` to the roster/export endpoints alongside all visible filters;
do not filter an already paginated page in the browser. Enrollment status remains
ACTIVE/PENDING/INACTIVE, with pending counted separately. School settings, student
enrollment/edit/move, teacher name/title edit and in-app messaging reuse existing
APIs. Login email remains read-only; removing teachers is not an activation toggle.

No seed or schema work is needed. Keep attendance, match scheduling/Play Now and
game telemetry integration deferred. Student messaging requires a defined API/UI
contract and remains disabled. After deployment retest all three roles, deep-link
refresh, real fresh email links, invitation resend/cancel and isolated CRUD/import/
export actions before declaring public staging acceptance complete.
