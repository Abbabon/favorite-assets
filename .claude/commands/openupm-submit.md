# OpenUPM Submit Command

Submit the Favorite Assets package to OpenUPM registry.

## Usage

```
/openupm-submit
```

## Description

This command guides you through submitting the Favorite Assets Unity package to the OpenUPM registry, handling all prerequisites and submission steps.

## What this command does:

1. **Verifies Prerequisites**:
   - Checks that the GitHub repository is public and contains necessary files
   - Ensures proper Git tagging for releases (semantic versioning)
   - Validates package structure and Unity compatibility

2. **Prepares Package Information**:
   - **Package Name**: `com.mezookan.favorite-assets`
   - **Repository URL**: Current GitHub repository URL
   - **Package Path**: `Packages/FavoriteAssets`
   - **License**: MIT
   - **Keywords**: unity, editor, assets, favorites, productivity, tools
   - **Category**: Editor Extensions

3. **Submission Process**:
   - Directs to [OpenUPM Package Add Form](https://openupm.com/packages/add/)
   - Provides pre-filled package information for the form
   - Generates proper YAML configuration
   - Guides through pull request submission to OpenUPM curated repository

4. **Post-Submission Tasks**:
   - Updates README with correct OpenUPM package URL
   - Adds OpenUPM badge to repository
   - Sets up monitoring for package downloads and feedback

## Generated YAML Example

```yaml
name: com.mezookan.favorite-assets
displayName: Favorite Assets
description: A Unity Editor tool that allows you to mark assets and folders as favorites for quick access through a dedicated window with context menu integration.
repoUrl: 'https://github.com/yourusername/favorite-assets'
packagePath: Packages/FavoriteAssets
gitTagPrefix: v
gitTagIgnore: ''
minVersion: ''
image: ''
readme: 'master:README.md'
readme_zhCN: ''
parentRepoUrl: null
licenseSpdxId: MIT
licenseName: MIT License
topics:
  - unity
  - editor
  - assets
  - favorites
  - productivity
  - tools
hunter: yourusername
gitTagIncludes: []
```

## Prerequisites Checklist

- [ ] Repository is public on GitHub
- [ ] Git tag exists for current release (e.g., `v1.0.0`)
- [ ] Package works correctly in Unity 2022.3+
- [ ] All necessary files are committed and pushed
- [ ] Package.json is properly configured
- [ ] LICENSE file exists in repository

## Important Notes

- OpenUPM requires Git tags to trigger its build pipeline
- Each new release should have a corresponding Git tag following semantic versioning
- The package will be automatically built from Git tags once approved
- Monitor the pull request status for approval and feedback

## Badge for README

After successful publication, add this badge:

```markdown
[![openupm](https://img.shields.io/npm/v/com.mezookan.favorite-assets?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.mezookan.favorite-assets/)
```