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
        private static List<FavoriteGroup> _favoriteGroups;
        private static readonly object _lock = new object();
        
        static FavoriteAssetsDataManager()
        {
            LoadFavorites();
        }
        
        public static List<FavoriteGroup> GetGroups()
        {
            lock (_lock)
            {
                return _favoriteGroups?.ToList() ?? new List<FavoriteGroup>();
            }
        }
        
        public static string CreateGroup(string name)
        {
            lock (_lock)
            {
                if (_favoriteGroups == null)
                    _favoriteGroups = new List<FavoriteGroup>();
                    
                var sortOrder = _favoriteGroups.Count;
                var group = new FavoriteGroup(name, sortOrder);
                _favoriteGroups.Add(group);
                SaveFavorites();
                return group.Id;
            }
        }
        
        public static bool DeleteGroup(string groupId)
        {
            lock (_lock)
            {
                var groupIndex = _favoriteGroups?.FindIndex(g => g.Id == groupId) ?? -1;
                if (groupIndex >= 0)
                {
                    _favoriteGroups.RemoveAt(groupIndex);
                    
                    // Move ungrouped assets back to no group
                    foreach (var asset in _favoriteAssets.Where(a => a.GroupId == groupId))
                    {
                        asset.SetGroupId(null);
                    }
                    
                    SaveFavorites();
                    return true;
                }
                return false;
            }
        }
        
        public static bool SetGroupCollapsed(string groupId, bool collapsed)
        {
            lock (_lock)
            {
                var group = _favoriteGroups?.FirstOrDefault(g => g.Id == groupId);
                if (group != null)
                {
                    group.SetCollapsed(collapsed);
                    SaveFavorites();
                    return true;
                }
                return false;
            }
        }
        
        public static bool RenameGroup(string groupId, string newName)
        {
            lock (_lock)
            {
                var group = _favoriteGroups?.FirstOrDefault(g => g.Id == groupId);
                if (group != null && !string.IsNullOrEmpty(newName))
                {
                    group.SetName(newName);
                    SaveFavorites();
                    return true;
                }
                return false;
            }
        }
        
        public static bool MoveAssetToGroup(string assetGuid, string groupId)
        {
            lock (_lock)
            {
                var asset = _favoriteAssets?.FirstOrDefault(a => a.AssetGuid == assetGuid);
                if (asset != null)
                {
                    asset.SetGroupId(groupId);
                    SaveFavorites();
                    return true;
                }
                return false;
            }
        }
        
        public static List<FavoriteAssetData> GetAssetsInGroup(string groupId)
        {
            lock (_lock)
            {
                CleanupInvalidAssets();
                return _favoriteAssets?.Where(a => a.GroupId == groupId).ToList() ?? new List<FavoriteAssetData>();
            }
        }
        
        public static List<FavoriteAssetData> GetUngroupedAssets()
        {
            lock (_lock)
            {
                CleanupInvalidAssets();
                return _favoriteAssets?.Where(a => string.IsNullOrEmpty(a.GroupId)).ToList() ?? new List<FavoriteAssetData>();
            }
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
                _favoriteGroups = new List<FavoriteGroup>();
                
                var dataPath = GetDataPath();
                if (File.Exists(dataPath))
                {
                    try
                    {
                        var json = File.ReadAllText(dataPath);
                        var wrapper = JsonUtility.FromJson<FavoriteAssetsWrapper>(json);
                        if (wrapper != null)
                        {
                            if (wrapper.favorites != null)
                            {
                                _favoriteAssets = wrapper.favorites.Where(f => f.IsValid()).ToList();
                            }
                            if (wrapper.groups != null)
                            {
                                _favoriteGroups = wrapper.groups.ToList();
                            }
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
                    
                    var wrapper = new FavoriteAssetsWrapper { favorites = _favoriteAssets, groups = _favoriteGroups ?? new List<FavoriteGroup>() };
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
        
        [Serializable]
        private class FavoriteAssetsWrapper
        {
            public List<FavoriteAssetData> favorites = new();
            public List<FavoriteGroup> groups = new();
        }
    }
}