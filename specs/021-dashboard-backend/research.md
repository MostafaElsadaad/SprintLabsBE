# Research
## Ownership
Decision: reuse current staff-community resolver plus caller account state checks; owner manages, teachers read only assigned active classes.
Rationale: existing student helpers permit whole-community teacher reads, so cannot be reused for the new assigned-class views.
Alternatives: global admin bypass rejected; teacher-provided community ID rejected.
## Results and questions
Decision: defer match/result/quiz endpoints and preserve question schema.
Rationale: user clarified game results and question schema update come later.
Alternatives: inferred historical class attribution and synthetic quiz titles rejected.
## Data integrity
Decision: use existing Identity and teacher lifecycle services for transaction-sensitive mutations; repositories for ordinary reads.
Rationale: invitation/seat and profile/email changes cross existing persistence/trust boundaries.
Alternatives: controller EF access and one-line feature repositories rejected.
## Frontend adaptation
Decision: versioned endpoints and BaseResponse; frontend items/page/pageSize/total payload; owners retain plural teachers and teacher grade display joins assigned grade labels.
Rationale: user approved existing conventions and multiple-teacher behavior.
## Activity
Decision: store only actual management events from new actions; expose dates from observed events and joined-platform fallback. No login or gameplay inference.
Rationale: no existing activity stream. Week periods are UTC Monday-based; public numeric gameplay fields use user-requested zero placeholders; missing dates remain null.


## Route consolidation — 2026-10-03
Use existing Communities roster/create/update/archive/remove APIs rather than keep duplicate dashboard routes. New missing operations live beside them. CommunityDashboard is reduced to four shared reads. HTTP contract tests verify exact requested fields, owner plural teachers and teacher assigned-class scope.
