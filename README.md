# Favorite Assets

A Unity Editor tool that allows you to mark Unity assets and folders as favorites for quick access through a dedicated window with context menu integration.

[![openupm](https://img.shields.io/npm/v/com.mezookan.favorite-assets?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.mezookan.favorite-assets/)

## Table of Contents

- [Features](#features)
- [Installation](#installation)
  - [Method 1: OpenUPM (Recommended)](#method-1-openupm-recommended)
  - [Method 2: Package Manager (Git URL)](#method-2-package-manager-git-url)
  - [Method 3: Local Package](#method-3-local-package)
  - [Method 4: Manual Installation](#method-4-manual-installation)
- [Requirements](#requirements)
- [Usage](#usage)
  - [Adding Favorites](#adding-favorites)
  - [Viewing Favorites](#viewing-favorites)
  - [Managing Favorites](#managing-favorites)
- [Technical Details](#technical-details)
- [Uninstallation](#uninstallation)
- [License](#license)
- [Support](#support)

## Features

### 🗂️ **Asset Organization**
- **Collapsible Groups**: Organize your favorites into named, collapsible groups for better management
- **Group Management**: Create, rename (double-click), and delete groups with confirmation dialogs
- **Drag-Free Organization**: Right-click context menu to easily move assets between groups
- **Smart Layout**: Ungrouped assets appear first, followed by organized grouped content

### 🎯 **Quick Access & Navigation**
- **Context Menu Integration**: Mark any Unity asset or folder as a favorite from the Project window
- **Dedicated Window**: Clean, modern UI for viewing and managing all your favorites
- **One-Click Access**: Single-click to select, double-click to open assets
- **Visual Hierarchy**: Groups have distinctive blue headers with expand/collapse arrows

### 🔧 **Advanced Controls**
- **Smart Sorting**: Multiple sorting options with easy-to-use cycling buttons:
  - **Name** (A-Z or Z-A)
  - **Type** (Asset type, then name)  
  - **Date Added** (when you first favorited it)
  - **File Modified** (actual file modification date from disk)
- **Colorful Toolbar**: Color-coded buttons for intuitive navigation:
  - 🔵 **Blue**: Sort type cycling
  - 🟢 **Green**: Sort order toggle  
  - 🟣 **Purple**: Create new group
  - 🔷 **Teal**: Refresh list
  - 🔴 **Red**: Clear all favorites
- **Status Bar**: Bottom status line showing total count of favorite assets

### 🛡️ **Reliability & Performance**
- **Automatic Cleanup**: Deleted assets are automatically removed from favorites
- **Persistent Storage**: Data survives Unity restarts and project switches
- **GUID-based Tracking**: Favorites persist even when assets are moved or renamed
- **Thread-Safe Operations**: Robust data management with proper locking mechanisms

## Installation

### Method 1: OpenUPM (Recommended)

Install via [OpenUPM](https://openupm.com/packages/com.mezookan.favorite-assets/):

**Using OpenUPM CLI:**
```bash
openupm add com.mezookan.favorite-assets
```

**Using Unity Package Manager:**
1. Open **Edit → Project Settings → Package Manager**
2. Add a new Scoped Registry:
   - **Name**: `OpenUPM`
   - **URL**: `https://package.openupm.com`
   - **Scope**: `com.mezookan`
3. Open **Window → Package Manager**
4. Select **My Registries** from the dropdown
5. Find and install **Favorite Assets**

### Method 2: Package Manager (Git URL)

1. Open Unity and navigate to **Window → Package Manager**
2. Click the **+** button in the top-left corner
3. Select **Add package from git URL...**
4. Enter the following URL:
   ```
   https://github.com/yourusername/favorite-assets.git?path=Packages/FavoriteAssets
   ```
5. Click **Add**

### Method 3: Local Package

1. Download or clone this repository to your local machine
2. In Unity, open **Window → Package Manager**
3. Click the **+** button in the top-left corner
4. Select **Add package from disk...**
5. Navigate to the downloaded folder and select `package.json` inside the `Packages/FavoriteAssets/` directory
6. Click **Open**

### Method 4: Manual Installation

Add this to your `manifest.json` file located in the `Packages` folder of your project:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.mezookan"]
    }
  ],
  "dependencies": {
    "com.mezookan.favorite-assets": "1.2.0"
  }
}
```

## Requirements

- **Unity Version**: 2022.3 or later
- **Editor Only**: This package only works in the Unity Editor (not in builds)

## Usage

### Adding Favorites
1. Right-click any asset or folder in the Project window
2. Select **Add to Favorites** from the context menu

### Opening the Window
1. Open the Favorite Assets window via **Window → Favorite Assets**
2. The window displays all your favorite assets with their icons, names, and paths
3. Single-click to select an asset in the Project window
4. Double-click to open the asset

### Working with Groups
- **Creating Groups**:
  - Click the 🟣 **"+ Group"** button in the toolbar
  - Groups are created with timestamp names (e.g., "Group 14:32:15")
- **Renaming Groups**:
  - Double-click any group name to edit it inline
  - Press **Enter** to save or **Escape** to cancel
- **Organizing Assets**:
  - Right-click any asset to open the context menu
  - Select **"Move to Group"** → choose target group
  - Use **"Remove from Group"** to move assets back to ungrouped
- **Managing Groups**:
  - Click **▶/▼** arrows to expand/collapse groups
  - Click **×** button to delete groups (assets move to ungrouped)
  - Groups remember their collapsed state

### Sorting & Filtering
- **Sorting Controls**:
  - 🔵 **Blue Button**: Click to cycle through sorting options (Name → Type → Added → Modified)
  - 🟢 **Green Button**: Click to toggle sort order (↑ ascending / ↓ descending)
  - Sorting applies to both grouped and ungrouped assets
- **Action Buttons**:
  - 🔷 **Refresh**: Update the list and clean up any deleted assets
  - 🔴 **Clear All**: Remove all favorites (with confirmation dialog)
- **Status Information**:
  - Bottom status bar shows total count of all favorite assets
  - Group headers show count of assets in each group
- **Automatic Cleanup**: Deleted assets are automatically removed when the window refreshes or gains focus

## Technical Details

### Data Architecture
- **Storage Format**: Favorites and groups stored in JSON format at `Application.persistentDataPath/Editor/FavoriteAssetsData.json`
- **Group System**: New `FavoriteGroup` class with metadata (ID, name, collapsed state, creation date, sort order)
- **Asset Linking**: Assets linked to groups via `groupId` field in `FavoriteAssetData`
- **Serialization**: DateTime fields converted to serializable long ticks for Unity compatibility

### Performance & Reliability
- **Thread Safety**: Uses thread-safe data access patterns with proper locking mechanisms
- **Smart Asset Validation**: 
  - Checks both Unity GUID system and file system existence
  - Automatically removes deleted assets from favorites and groups
  - Validates assets on load, focus, and refresh
- **File Modification Tracking**: Real-time monitoring of actual file modification dates from disk
- **Memory Efficient**: Optimized data structures and minimal garbage collection

### User Interface
- **UI Framework**: Built using Unity's UIElements system with modern USS styling
- **Responsive Design**: Compact, scalable interface that adapts to different window sizes
- **Color Coding**: Distinctive colors for each button type to improve usability
- **Context Menus**: Native Unity GenericMenu integration for intuitive asset management
- **Inline Editing**: TextField-based group renaming with keyboard shortcuts

## Uninstallation

To remove the package:

1. Open **Window → Package Manager**
2. Select **In Project** from the dropdown
3. Find **Favorite Assets** in the list
4. Click **Remove**

Note: Your favorite assets data will persist even after uninstalling the package. To completely clean up, manually delete the `FavoriteAssetsData.json` file from your persistent data path if desired.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, feature requests, or questions:
- Create an issue on the GitHub repository
- Contact: contact@mezookan.com