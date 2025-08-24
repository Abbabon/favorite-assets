using System;
using System.IO;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    [Serializable]
    public class FavoriteAssetData
    {
        [SerializeField] private string _assetPath;
        [SerializeField] private string _assetName;
        [SerializeField] private string _assetType;
        [SerializeField] private string _assetGuid;
        [SerializeField] private string _groupId;
        [SerializeField] private long _dateAddedTicks;
        [SerializeField] private long _dateUpdatedTicks;
        
        public string AssetPath => _assetPath;
        public string AssetName => _assetName;
        public string AssetType => _assetType;
        public string AssetGuid => _assetGuid;
        public DateTime DateAdded => _dateAddedTicks == 0 ? DateTime.Now : new DateTime(_dateAddedTicks);
        public DateTime DateUpdated => _dateUpdatedTicks == 0 ? DateAdded : new DateTime(_dateUpdatedTicks);
        public string GroupId => _groupId;
        
        public DateTime FileModificationDate
        {
            get
            {
                try
                {
                    if (File.Exists(_assetPath))
                    {
                        return File.GetLastWriteTime(_assetPath);
                    }
                    if (Directory.Exists(_assetPath))
                    {
                        return Directory.GetLastWriteTime(_assetPath);
                    }
                }
                catch
                {
                    // If we can't get file modification date, fall back to our tracked date
                }
                return DateUpdated;
            }
        }
        
        public FavoriteAssetData(string assetPath, string assetName, string assetType, string assetGuid, string groupId = null)
        {
            _assetPath = assetPath;
            _assetName = assetName;
            _assetType = assetType;
            _assetGuid = assetGuid;
            _groupId = groupId;
            var now = DateTime.Now;
            _dateAddedTicks = now.Ticks;
            _dateUpdatedTicks = now.Ticks;
        }
        
        public void UpdateAccessDate()
        {
            _dateUpdatedTicks = DateTime.Now.Ticks;
        }
        
        public void SetGroupId(string groupId)
        {
            _groupId = groupId;
        }
        
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_assetPath) || string.IsNullOrEmpty(_assetGuid))
                return false;
                
            // Check if the asset still exists in the project
            try
            {
                // Check if the GUID still points to a valid asset
                var pathFromGuid = UnityEditor.AssetDatabase.GUIDToAssetPath(_assetGuid);
                if (string.IsNullOrEmpty(pathFromGuid))
                    return false;
                
                // Check if the file/folder actually exists on disk
                return File.Exists(_assetPath) || Directory.Exists(_assetPath);
            }
            catch
            {
                return false;
            }
        }
    }
}