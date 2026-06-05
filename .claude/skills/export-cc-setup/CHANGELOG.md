# Changelog

## v1.0.0 — 2026-05-27

Initial release.

- Four-phase pipeline: Discover, Classify, Package, Verify
- Stack fingerprint derived from CLAUDE.md (not hardcoded)
- Three-way classification: portable / stack-specific / leaked
- Two interactive checkpoints (classification approval, final verification)
- Playbooks for stack detection, remediation rules, and template generation
- Disk-persisted state for resumability between phases
- settings.json handling (classified by scan, not auto-cleaned)
- INSTALL.md and MANIFEST.md generation for kit recipients

### Post-critic fixes (v1.0.0)

Applied after OMC critic review:
- **Major:** Added settings.json handling (was completely missing — hooks wouldn't fire without it)
- **Major:** Fixed classification contradiction ("always portable" vs scan-everything — now single source of truth in Phase 2)
- **Minor:** Added CHANGELOG.md
- **Minor:** Added disk state persistence (phase-1.yaml, phase-2.yaml) for resumability
- **Minor:** INSTALL.md skill checklist now generated dynamically from excluded skills
- **Minor:** Added .claude/commands/ to inventory
- **Minor:** Removed Program.cs from alias table (overly aggressive)
- **Minor:** Added binary file handling (copy as-is, skip fingerprint scan)
- **Minor:** Added settings.json hook-reference check to Phase 4 verification

### Post-review design change (v1.0.0)

Replaced "mixed → auto-clean" with "leaked → block + remediate":
- **Breaking:** Removed Category 3 "Mixed (clean and include)" — replaced with "Leaked (block — remediate in source)"
- **Breaking:** Removed `MIXED_THRESHOLD` config constant — replaced with `LEAK_THRESHOLD` (same default 20%, different semantics: files above threshold are excluded, files between 0% and threshold are blocked)
- **Breaking:** Removed `CLEAN_MARKER` config constant — no more auto-cleaning
- **New:** Phase 2 now generates remediation reports for leaked files with per-line fix proposals
- **New:** Phase 3 has a hard gate — refuses to proceed if any leaked files remain
- **New:** `playbooks/remediation-rules.md` replaces `playbooks/cleaning-rules.md` — maps each leak type to its target file in the source project (indirection pattern)
- **New:** INSTALL.md includes boy-scout guardrails note for coding-conventions.md
- **Removed:** `playbooks/cleaning-rules.md` — no longer needed (source must be clean, not patched at export time)
- **Design rationale:** The source project's pipeline layer should be 0% stack-coupled at all times. Auto-cleaning during export masks drift. Fixing at the source keeps both the source project and all future exports clean.
