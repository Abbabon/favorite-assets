using UnityEditor;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    public static class FavoriteAssetsContextMenu
    {
        [MenuItem("Assets/Add to Favorites", false, 19)]
        public static void AddToFavorites()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;
                
            var addedCount = 0;
            foreach (var obj in selectedObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (! string.IsNullOrEmpty(assetPath) && FavoriteAssetsDataManager.AddFavorite(assetPath))
                {
                    addedCount++;
                }
            }
            
            if (addedCount > 0)
            {
                Debug.Log($"Added {addedCount} asset(s) to favorites.");
                
                var window = EditorWindow.GetWindow<FavoriteAssetsWindow>();
                if (window != null)
                {
                    window.RefreshWindow();
                }
            }
            else
            {
                Debug.Log("Selected assets are already in favorites or cannot be added.");
            }
        }
        
        [MenuItem("Assets/Add to Favorites", true)]
        public static bool AddToFavoritesValidate()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;
                
            foreach (var obj in selectedObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (! string.IsNullOrEmpty(assetPath) && ! FavoriteAssetsDataManager.IsFavorite(assetPath))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        [MenuItem("Assets/Remove from Favorites", false, 20)]
        public static void RemoveFromFavorites()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;
                
            var removedCount = 0;
            foreach (var obj in selectedObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (! string.IsNullOrEmpty(assetPath))
                {
                    var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (FavoriteAssetsDataManager.RemoveFavorite(assetGuid))
                    {
                        removedCount++;
                    }
                }
            }
            
            if (removedCount > 0)
            {
                Debug.Log($"Removed {removedCount} asset(s) from favorites.");
                
                var window = EditorWindow.GetWindow<FavoriteAssetsWindow>();
                if (window != null)
                {
                    window.RefreshWindow();
                }
            }
        }
        
        [MenuItem("Assets/Remove from Favorites", true)]
        public static bool RemoveFromFavoritesValidate()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;
                
            foreach (var obj in selectedObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (! string.IsNullOrEmpty(assetPath) && FavoriteAssetsDataManager.IsFavorite(assetPath))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}