---
name: repo-hygiene
description: Cleans tracked build artifacts (bin, obj, IDE caches) from git and ensures .gitignore standards.
---

# Repository Hygiene & Git Cleanup

When the user asks to "clean the repo", "untrack build files", or "clean git artifacts", use this skill.

## Objective
Ensure build artifacts, transient compiler files (`bin/`, `obj/`), IDE files (`.vs/`, `.idea/`), and OS files (`.DS_Store`) are not tracked by git and are properly covered by `.gitignore`.

## Procedure

### Step 1: Untrack Ignored Artifacts
Remove any tracked build or cache artifacts from git's index while preserving them on disk:

```bash
git rm -r --cached bin/ obj/ .vs/ .idea/ .DS_Store 2>/dev/null || true
```

### Step 2: Ensure `.gitignore` Contains Essential Patterns
Verify that `.gitignore` at the repository root contains at least:
```gitignore
# Build results
bin/
obj/
TestResults/

# Visual Studio / Rider / JetBrains
.vs/
.idea/
*.user
*.suo

# OS generated
.DS_Store
Thumbs.db
```

### Step 3: Check Git Status
Run `git status` to verify:
- Build output directories (`obj/`, `bin/`) are staged as deleted from git.
- No new uncommitted build files are listed under untracked files.
- Report the cleaned status back to the user.
