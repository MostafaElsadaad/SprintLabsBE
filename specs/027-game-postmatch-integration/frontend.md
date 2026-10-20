# Game client behavior

The dedicated server sends one post-match bulk result for all registered human profiles. Only it holds the GameServer credential. Player devices never calculate or post persisted XP/RP. They receive their own saved reward over Mirror and refresh the existing authenticated profile store. Match score remains a separate gameplay value. XP/level and optional RP/tier labels bind to backend facts.

Show an unconfirmed/pending state until the backend accepts results. The server retains immutable failed reports for later retry; do not fabricate rewards, resubmit per-player completions or send missions. Offline DEV identities are untracked. API rollout must precede a server build using typed history, as indicated by supportsQuestionHistory during registration. No new dashboard page or mission UI.
