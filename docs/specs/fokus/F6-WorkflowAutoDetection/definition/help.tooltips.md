# Workflow Auto-Detection — Tooltip Help

Sibling of `docs/features/WorkflowAutoDetection/spec.md`. Each section provides tooltip text (under 150 chars) shown on info icon hover.

---

## Workflow Stages

The ordered list of status phases your team's tickets move through from start to done.

---

## Auto-Detection

Analyzes your synced transition history to propose an ordered workflow pipeline automatically.

---

## Confidence Summary

Shows how much data backs the detection: transitions analyzed, tickets covered, sprints spanned.

---

## Sidelined Statuses ("Other Statuses")

Statuses found in your data but excluded from the main pipeline — add them manually if needed.

---

## Re-Detect Button

Runs a fresh detection using current sync data, replacing the displayed proposal.

---

## Bug Exclusion

Detection ignores bug tickets — their chaotic status paths would distort the pipeline order.

---

## Forward-Flow Scoring

Stages are ordered by how strongly their transitions point toward done statuses.

---

## Done Status Anchoring

Your configured done statuses anchor the tail of the detected pipeline.

---

## Manual Entry

Type workflow stages by hand if you already know your pipeline or prefer not to use detection.

---

## Unsaved Changes

Navigating away without saving discards the current proposal and restores your saved stages.
