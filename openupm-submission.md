# OpenUPM Submission Guide

This document outlines the steps to submit the Favorite Assets package to OpenUPM.

## Prerequisites

1. **Repository Setup**: Ensure the GitHub repository is public and contains all necessary files
2. **Versioning**: Create a Git tag for the release (e.g., `v1.0.0`)
3. **Package Testing**: Verify the package works correctly in Unity

## Submission Process

### Step 1: Create a Git Tag

```bash
git tag v1.0.0
git push origin v1.0.0
```

### Step 2: Prepare Package Information

Use the following details for OpenUPM submission:

- **Package Name**: `com.mezookan.favorite-assets`
- **Repository URL**: `https://github.com/yourusername/favorite-assets`
- **Package Path**: `Packages/FavoriteAssets`
- **License**: MIT
- **Keywords**: unity, editor, assets, favorites, productivity, tools
- **Category**: Editor Extensions

### Step 3: Submit to OpenUPM

1. Visit [OpenUPM Package Add Form](https://openupm.com/packages/add/)
2. Fill in the package information:
   - Repository URL: Your GitHub repository URL
   - Package path: `Packages/FavoriteAssets`
   - License: MIT
3. Click "Generate YAML"
4. Review the generated YAML file
5. Submit a pull request to the [OpenUPM curated repository](https://github.com/openupm/openupm)

### Step 4: Monitor Status

- Check the pull request status for approval
- Once merged, the package will be available on OpenUPM
- Build pipeline will automatically create package versions from Git tags

## YAML Example

The generated YAML should look similar to this:

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

## Post-Submission

After successful submission:

1. Update the README.md with the correct OpenUPM package URL
2. Add the OpenUPM badge to the repository
3. Monitor package downloads and user feedback
4. Keep the package updated with new releases and tags

## Badge for README

Add this badge to your README.md after the package is published:

```markdown
[![openupm](https://img.shields.io/npm/v/com.mezookan.favorite-assets?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.mezookan.favorite-assets/)
```