# OpenUPM Update Command

Create a new version of the Favorite Assets package and publish it to OpenUPM.

## Usage

```
/openupm-update [version_type] [custom_version]
```

### Parameters

- `version_type`: Optional. One of `patch`, `minor`, `major`, or `custom`
  - `patch` (default): 1.0.0 → 1.0.1 (bug fixes)
  - `minor`: 1.0.0 → 1.1.0 (new features, backward compatible)
  - `major`: 1.0.0 → 2.0.0 (breaking changes)
  - `custom`: Use with `custom_version` parameter
- `custom_version`: Optional. Specify exact version (e.g., "1.2.3")

### Examples

```bash
/openupm-update                    # Increments patch version (1.0.0 → 1.0.1)
/openupm-update minor              # Increments minor version (1.0.0 → 1.1.0)
/openupm-update major              # Increments major version (1.0.0 → 2.0.0)
/openupm-update custom 1.2.3       # Sets version to 1.2.3
```

## What this command does:

### 1. **Pre-Update Validation**
- Checks for uncommitted changes in git
- Verifies current branch (should be main/master)
- Confirms package.json exists and is valid
- Validates current working directory

### 2. **Version Management**
- Reads current version from `Packages/FavoriteAssets/package.json`
- Calculates new version based on semantic versioning rules
- Updates package.json with new version
- Updates CHANGELOG.md with new version section

### 3. **Git Operations**
- Commits the version changes with standardized message
- Creates annotated git tag with version (e.g., `v1.0.1`)
- Pushes commits and tags to origin

### 4. **OpenUPM Integration**
- Git tags automatically trigger OpenUPM build pipeline
- New version becomes available on OpenUPM registry
- Updates README badges if needed

### 5. **Post-Update Actions**
- Displays new version information
- Provides links to monitor OpenUPM build status
- Shows commands to verify the update

## Files Updated

This command will modify:
- `Packages/FavoriteAssets/package.json` - Version number
- `CHANGELOG.md` - New version entry with timestamp
- Git repository - New commit and tag

## Semantic Versioning Rules

Following [SemVer](https://semver.org/):

- **PATCH** (1.0.0 → 1.0.1): Bug fixes, no API changes
- **MINOR** (1.0.0 → 1.1.0): New features, backward compatible
- **MAJOR** (1.0.0 → 2.0.0): Breaking changes, API changes

## CHANGELOG Template

When updating, add your changes to CHANGELOG.md before running this command:

```markdown
## [Unreleased]

### Added
- New feature descriptions

### Changed
- Modified feature descriptions

### Fixed
- Bug fix descriptions

### Deprecated
- Features marked for removal

### Removed
- Removed features

### Security
- Security improvements
```

## Prerequisites Checklist

Before running this command:

- [ ] All changes are committed to git
- [ ] Working directory is clean
- [ ] On main/master branch
- [ ] CHANGELOG.md updated with new changes
- [ ] Package tested and working
- [ ] Breaking changes documented (for major versions)

## Git Commit Message Format

The command uses this standardized format:

```
Release v{version}

- Updated package.json to version {version}
- Updated CHANGELOG.md with release notes

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

## OpenUPM Build Monitoring

After update, monitor at:
- [OpenUPM Package Page](https://openupm.com/packages/com.mezookan.favorite-assets/)
- [OpenUPM Build Status](https://github.com/openupm/openupm/actions)

## Rollback Instructions

If something goes wrong:

```bash
# Remove the tag locally and remotely
git tag -d v{version}
git push origin :refs/tags/v{version}

# Reset to previous commit
git reset --hard HEAD~1
git push origin main --force-with-lease
```

## Troubleshooting

**Error: Uncommitted changes**
- Commit or stash your changes before updating

**Error: Not on main branch**
- Switch to main branch: `git checkout main`

**Error: Remote push failed**
- Check network connection and repository permissions
- Ensure you have push access to the repository

**Error: Tag already exists**
- Version already released, increment to next available version