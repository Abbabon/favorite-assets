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
        
        /// <summary>
        /// The asset's live path, resolved from the GUID so that moved and renamed assets keep working.
        /// Falls back to the path recorded when the favorite was added if the GUID no longer resolves.
        /// </summary>
        public string CurrentPath
        {
            get
            {
                try
                {
                    var pathFromGuid = UnityEditor.AssetDatabase.GUIDToAssetPath(_assetGuid);
                    if (!string.IsNullOrEmpty(pathFromGuid))
                        return pathFromGuid;
                }
                catch
                {
                    // Fall through to the recorded path.
                }
                return _assetPath;
            }
        }
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
                    var path = CurrentPath;
                    if (File.Exists(path))
                    {
                        return File.GetLastWriteTime(path);
                    }
                    if (Directory.Exists(path))
                    {
                        return Directory.GetLastWriteTime(path);
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
            if (string.IsNullOrEmpty(_assetGuid))
                return false;
                
            try
            {
                // Resolve through the GUID rather than the stored path, so that an asset moved or
                // renamed in the Project window stays a favorite.
                var pathFromGuid = UnityEditor.AssetDatabase.GUIDToAssetPath(_assetGuid);
                if (string.IsNullOrEmpty(pathFromGuid))
                    return false;
                
                return File.Exists(pathFromGuid) || Directory.Exists(pathFromGuid);
            }
            catch
            {
                return false;
            }
        }
    }
}