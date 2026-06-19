# Feature Brief: <Feature Name>

## Epic
<Which epic this belongs to>

## Goal
<What user/business problem this solves>

## User story
As a <role>, I want <capability>, so that <benefit>.

## Scope
### In scope
- <item>

### Out of scope
- <item>

## Existing code to inspect
- `<path>`

## API contract
### Endpoint
`METHOD /api/v1/<controller>/<route>`

### Request
```json
{}
```

### Success response
```json
{}
```

### Error cases
- 400: <case>
- 401/403: <case>
- 404: <case>

## Database changes
### New tables
- <table>

### Changed tables
- <table>

### Indexes/constraints
- <constraint>

## Acceptance criteria
- [ ] <criterion>

## Verification
- [ ] `dotnet build SprintLabs.sln`
- [ ] `dotnet test SprintLabs.sln` if applicable
- [ ] Manual API test in Swagger/Postman
