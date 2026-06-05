# Changelog

## v1.0.0 — 2026-05-27

Initial release.

- Four-phase pipeline: Discover, Plan, Apply, Verify
- Companion to export-cc-setup — imports kits produced by the export skill
- Collision detection with per-file resolution at CHECKPOINT 1
- Archive-based displacement — never deletes target files (`.claude/_archived/{YYYYMMDD}/`)
- CLAUDE.md merge protocol: kit sections layered into existing CLAUDE.md
- Settings.json deep merge (kit wins on key collision)
- Conventions index merge: existing entries commented out as examples
- Re-import support via `.kit-manifest.md` tracking
- Greenfield and existing project support
- docs/ placeholder skip logic (don't overwrite existing KB, architecture)
- `.claude/README.md` regeneration after merge
- Playbooks for CLAUDE.md merge and collision resolution

### Post-critic fixes (v1.0.0)

Applied after OMC critic review:
- **Major:** Re-import status handling in Phase 3 (`removed`, `user-modified`, `preserve`, `flag` resolutions)
- **Major:** Checksum column in kit-manifest for user-modified file detection
- **Major:** CHECKPOINT 1 "Removed from Kit" section for re-import
- **Minor:** FILL marker detection is now dynamic (scans for `<!-- FILL:` in template, not hardcoded to 3 sections)
- **Minor:** Classification validation in Phase 1 warns on unexpected MANIFEST entries
- **Minor:** Hard/soft required distinction in Phase 1 kit validation
- **Minor:** Section matching uses explicit alias table instead of vague "close variant"
- **Minor:** Hard Rule 10 acknowledges root-level file exception
- **Minor:** Commands section added to kit-manifest template
- **Minor:** Zero-collision handling aligned between error table and Phase 2
- **Minor:** Conventions index identification clarified with concrete criteria

### Post-user-review fixes (v1.0.0)

Applied after user review:
- **Major:** settings.json merge now uses per-shape rules (scalar: kit wins, object: recursive merge, array of hooks: concatenate, array of strings: union) — prevents destroying target hooks/permissions
- **Moderate:** FILL marker detection made fully dynamic — scans template for `<!-- FILL:` instead of hardcoding 3 sections
- **Moderate:** `## Architecture` moved to FILL marker section (project-specific, not pipeline) — also fixed in export's template-generation.md
- **Minor:** Checksum format specified (first 12 hex of SHA-256)
- **Minor:** Same-day archive counter defined once in collision-resolution.md, referenced elsewhere
- **Minor:** README regeneration steps made concrete (Glob counts, parenthetical update)
- **Minor:** settings.local.json exclusion documented explicitly
