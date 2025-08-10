# Favorite Assets

A Unity Editor tool that allows you to mark Unity assets and folders as favorites for quick access through a dedicated window with context menu integration.

## Features

- Mark any Unity asset or folder as a favorite from the Project window context menu
- Dedicated "Favorite Assets" window for viewing and managing favorites
- Quick access to favorites with single-click selection and double-click opening
- Persistent storage that survives Unity restarts
- Visual asset type indicators and full path display
- GUID-based tracking ensures favorites persist even when assets are moved

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
    "com.mezookan.favorite-assets": "1.0.0"
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

### Viewing Favorites
1. Open the Favorite Assets window via **Window → Favorite Assets**
2. View all your favorite assets with their icons, names, and paths
3. Single-click to select an asset in the Project window
4. Double-click to open the asset

### Managing Favorites
- Use the **Clear All** button to remove all favorites
- Use the **Refresh** button to update the list
- The asset count is displayed in the toolbar

## Technical Details

- **Data Storage**: Favorites are stored in JSON format at `Application.persistentDataPath/Editor/FavoriteAssetsData.json`
- **Thread Safety**: Uses thread-safe data access patterns
- **Asset Validation**: Automatically filters out missing or invalid assets on load
- **UI Framework**: Built using Unity's UIElements system for modern Editor integration

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