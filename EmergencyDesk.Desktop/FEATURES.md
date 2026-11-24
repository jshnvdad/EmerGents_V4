# Dispatch-desk features added

| Feature | Where | Notes |
|---|---|---|
| Real-time alert popup | `Views/AlertWindow.*` | Topmost, flashing border, repeating sound. Shows highest-priority unacknowledged report. Acknowledge / Open / Remind me later. |
| Incident queue / inbox | "Incident queue" page | Chips: Open, New, Acknowledged, In progress, Escalated, Closed. Sorted: escalated, priority, longest waiting. |
| Auto-assignment rules | "Rules & SLA" page | Type -> region -> workload. Region = place names on a responder (Responders page) matched against the incident address. |
| Acknowledge | Popup + queue details | Stops re-alerts. Assigning a responder / changing status also counts as acknowledging. |
| Alert escalation | Rules & SLA | Unacknowledged after N s -> "Escalated": red row, banner on every page, popup overrides "remind me later". |
| Duplicate detection | Queue flags + details | Same phone, or same type within R metres, inside a time window. "Merge duplicates" closes extras as Cancelled and links them. |
| Priority scoring | Queue "Priority" column | 0-100 from type + description keywords (critical / vulnerable person / serious), multiple reports, unclear location, waiting time. Hover for the reasons. |
| SLA timers | Queue columns + details + popup | Live countdown for time-to-acknowledge and time-to-dispatch; turns amber, then red "OVERDUE". |

Rules and per-incident desk records are stored in `%LocalAppData%\EmergencyDesk\` (`rules.json`, `state.json`).

## Optional server support (the desk works without it)

Without these, acknowledgement is per-desk: other desks still see the report as new.

```
PUT api/incidents/{id}/acknowledge     body: { "acknowledgedBy": "Dana" }
```
Add to the incident JSON (all optional): `acknowledgedAt`, `acknowledgedBy`, `dispatchedAt` (UTC).
When present, the desk uses them, so SLA timers and "stop re-alerting" are shared by every operator.
