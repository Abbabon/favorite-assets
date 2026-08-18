using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    /// <summary>
    /// One recorded visit to a prefab opened in Prefab Mode.
    /// The GUID is the source of truth; the stored path is only a display fallback for prefabs
    /// whose GUID no longer resolves (deleted assets).
    /// </summary>
    [Serializable]
    public class PrefabHistoryEntry
    {
        [SerializeField] private string _prefabGuid;
        [SerializeField] private string _prefabPath;
        [SerializeField] private string _prefabName;
        [SerializeField] private long _lastOpenedTicks;

        public string PrefabGuid => _prefabGuid;
        public string PrefabName => _prefabName;
        public DateTime LastOpened => _lastOpenedTicks == 0 ? DateTime.Now : new DateTime(_lastOpenedTicks);

        /// <summary>
        /// The prefab's live path, resolved from the GUID so moved and renamed prefabs keep working.
        /// Falls back to the path recorded at visit time when the GUID no longer resolves.
        /// </summary>
        public string CurrentPath
        {
            get
            {
                try
                {
                    var livePath = AssetDatabase.GUIDToAssetPath(_prefabGuid);
                    if (!string.IsNullOrEmpty(livePath))
                        return livePath;
                }
                catch
                {
                    // Fall through to the recorded path.
                }
                return _prefabPath;
            }
        }

        public PrefabHistoryEntry(string prefabGuid, string prefabPath, string prefabName)
        {
            _prefabGuid = prefabGuid;
            _prefabPath = prefabPath;
            _prefabName = prefabName;
            _lastOpenedTicks = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Refreshes the display snapshot and stamps this entry as visited right now.
        /// </summary>
        public void Touch(string prefabPath)
        {
            if (!string.IsNullOrEmpty(prefabPath))
            {
                _prefabPath = prefabPath;
                _prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            }
            _lastOpenedTicks = DateTime.Now.Ticks;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_prefabGuid))
                return false;

            try
            {
                var livePath = AssetDatabase.GUIDToAssetPath(_prefabGuid);
                return !string.IsNullOrEmpty(livePath) && File.Exists(livePath);
            }
            catch
            {
                return false;
            }
        }
    }
}
