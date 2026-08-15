# Up Iz Up Mini — Project Handoff

## Project

- Path: `E:\Unity\Up Iz Up Mini`
- Unity: `6000.3.10f1`
- Reference project: `E:\Unity\Up iz up` — read-only
- Status: Coordination scaffold created; no gameplay implementation verified yet.
- Current owner: None
- Active task: None
- Last verified change: `MINI-000`
- Last known good commit: Not created yet

## Ownership protocol

Before editing, set:

```yaml
current_owner: Codex | Claude | User
active_task: MINI-###
claimed_at: ISO-8601 timestamp
reserved_files:
  - exact/path
```

After verification, append a change entry, update the verification results, and return the owner and active task to `None`.

## Current blockers

- No Mini gameplay scene has been created.
- No asset has been approved/imported for the Mini yet.
- Git has not yet been initialized for the Mini project.
- The precise Grand Bay map anchors must be copied into `Docs/MAP-ANCHORS.json` from verified research/user references.

## Change record

### MINI-000 — Project and coordination scaffold

- Date: 2026-08-15
- Owner: Codex
- Request: Create the separate Mini project structure and a handoff strategy for Claude Work.
- Implementation: Created a clean Unity 6 project and the coordination/design documentation scaffold.
- Files changed: Coordination and documentation files at the project root and under `Docs/` and `Coordination/`.
- Scene/prefab changes: None.
- Verification: Unity project reports editor version `6000.3.10f1`; documentation structure validated.
- Known issues: Gameplay and assets are intentionally not implemented yet.
- Next action: Claude or Codex claims `MINI-001` and builds the camera/world proof of concept.

## Required change-entry format

```text
### MINI-### — Short title

- Date:
- Owner:
- Request:
- Acceptance criteria:
- Implementation:
- Files changed:
- Scene/prefab changes:
- Verification commands:
- Verification results:
- Known issues:
- Next action:
```

