using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    public enum FavoriteSortType
    {
        Name,
        Type, 
        DateAdded,
        DateUpdated
    }
    
    public enum SortOrder
    {
        Ascending,
        Descending
    }
    public static class FavoriteAssetsDataManager
    {
        private const string _kPrefsKey = "FavoriteAssets_Data";
        private const string _kDataFileName = "FavoriteAssetsData.json";
        
        private static List<FavoriteAssetData> _favoriteAssets;
        private static readonly object _lock = new object();
        
        static FavoriteAssetsDataManager()
        {
            LoadFavorites();
        }
        
        public static List<FavoriteAssetData> GetFavorites()
        {
            lock (_lock)
            {
                CleanupInvalidAssets();
                return _favoriteAssets?.ToList() ?? new List<FavoriteAssetData>();
            }
        }
        
        public static List<FavoriteAssetData> GetSortedFavorites(FavoriteSortType sortType, SortOrder sortOrder)
        {
            lock (_lock)
            {
                CleanupInvalidAssets();
                var favorites = _favoriteAssets?.ToList() ?? new List<FavoriteAssetData>();
                return SortFavorites(favorites, sortType, sortOrder);
            }
        }
        
        private static List<FavoriteAssetData> SortFavorites(List<FavoriteAssetData> favorites, FavoriteSortType sortType, SortOrder sortOrder)
        {
            switch (sortType)
            {
                case FavoriteSortType.Name:
                    return sortOrder == SortOrder.Ascending 
                        ? favorites.OrderBy(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList()
                        : favorites.OrderByDescending(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList();
                        
                case FavoriteSortType.Type:
                    return sortOrder == SortOrder.Ascending
                        ? favorites.OrderBy(f => f.AssetType).ThenBy(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList()
                        : favorites.OrderByDescending(f => f.AssetType).ThenBy(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList();
                        
                case FavoriteSortType.DateAdded:
                    return sortOrder == SortOrder.Ascending
                        ? favorites.OrderBy(f => f.DateAdded).ToList()
                        : favorites.OrderByDescending(f => f.DateAdded).ToList();
                        
                case FavoriteSortType.DateUpdated:
                    return sortOrder == SortOrder.Ascending
                        ? favorites.OrderBy(f => f.FileModificationDate).ToList()
                        : favorites.OrderByDescending(f => f.FileModificationDate).ToList();
                        
                default:
                    return favorites;
            }
        }
        
        public static bool AddFavorite(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid))
                return false;
                
            lock (_lock)
            {
                if (_favoriteAssets.Any(f => f.AssetGuid == assetGuid))
                    return false;
                    
                var assetName = Directory.Exists(assetPath) ? 
                    Path.GetFileName(assetPath.TrimEnd('/', '\\')) : 
                    Path.GetFileNameWithoutExtension(assetPath);
                    
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                var assetType = Directory.Exists(assetPath) ? "Folder" : 
                    (asset != null ? asset.GetType().Name : "Unknown");
                
                var favoriteData = new FavoriteAssetData(assetPath, assetName, assetType, assetGuid);
                _favoriteAssets.Add(favoriteData);
                
                SaveFavorites();
                return true;
            }
        }
        
        public static bool RemoveFavorite(string assetGuid)
        {
            lock (_lock)
            {
                var index = _favoriteAssets.FindIndex(f => f.AssetGuid == assetGuid);
                if (index >= 0)
                {
                    _favoriteAssets.RemoveAt(index);
                    SaveFavorites();
                    return true;
                }
                return false;
            }
        }
        
        public static bool IsFavorite(string assetPath)
        {
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return ! string.IsNullOrEmpty(assetGuid) && IsFavoriteByGuid(assetGuid);
        }
        
        public static bool IsFavoriteByGuid(string assetGuid)
        {
            lock (_lock)
            {
                return _favoriteAssets.Any(f => f.AssetGuid == assetGuid);
            }
        }
        
        public static void UpdateAssetAccessDate(string assetGuid)
        {
            lock (_lock)
            {
                var favorite = _favoriteAssets.FirstOrDefault(f => f.AssetGuid == assetGuid);
                if (favorite != null)
                {
                    favorite.UpdateAccessDate();
                    SaveFavorites();
                }
            }
        }
        
        private static void CleanupInvalidAssets()
        {
            if (_favoriteAssets == null) return;
            
            var initialCount = _favoriteAssets.Count;
            _favoriteAssets.RemoveAll(asset => !asset.IsValid());
            
            // Save only if we actually removed something
            if (_favoriteAssets.Count != initialCount)
            {
                SaveFavorites();
            }
        }
        
        public static int CleanupInvalidAssetsManually()
        {
            lock (_lock)
            {
                if (_favoriteAssets == null) return 0;
                
                var initialCount = _favoriteAssets.Count;
                _favoriteAssets.RemoveAll(asset => !asset.IsValid());
                
                var removedCount = initialCount - _favoriteAssets.Count;
                if (removedCount > 0)
                {
                    SaveFavorites();
                }
                
                return removedCount;
            }
        }
        
        public static void ClearAll()
        {
            lock (_lock)
            {
                _favoriteAssets.Clear();
                SaveFavorites();
            }
        }
        
        private static void LoadFavorites()
        {
            lock (_lock)
            {
                _favoriteAssets = new List<FavoriteAssetData>();
                
                var dataPath = GetDataPath();
                if (File.Exists(dataPath))
                {
                    try
                    {
                        var json = File.ReadAllText(dataPath);
                        var wrapper = JsonUtility.FromJson<FavoriteAssetsWrapper>(json);
                        if (wrapper != null && wrapper.favorites != null)
                        {
                            _favoriteAssets = wrapper.favorites.Where(f => f.IsValid()).ToList();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to load favorite assets data: {e.Message}");
                    }
                }
            }
        }
        
        private static void SaveFavorites()
        {
            lock (_lock)
            {
                try
                {
                    var dataPath = GetDataPath();
                    var directory = Path.GetDirectoryName(dataPath);
                    if (! Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    var wrapper = new FavoriteAssetsWrapper { favorites = _favoriteAssets };
                    var json = JsonUtility.ToJson(wrapper, true);
                    File.WriteAllText(dataPath, json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to save favorite assets data: {e.Message}");
                }
            }
        }
        
        private static string GetDataPath()
        {
            return Path.Combine(Application.persistentDataPath, "Editor", _kDataFileName);
        }
        
        [System.Serializable]
        private class FavoriteAssetsWrapper
        {
            public List<FavoriteAssetData> favorites = new List<FavoriteAssetData>();
        }
    }
}