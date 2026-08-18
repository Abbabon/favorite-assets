using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    /// <summary>
    /// Records every prefab opened in Prefab Mode. The subscription is re-established on each domain
    /// reload, because [InitializeOnLoad] re-runs this static constructor and the event itself is
    /// wiped by the reload.
    /// </summary>
    [InitializeOnLoad]
    public static class PrefabHistoryTracker
    {
        static PrefabHistoryTracker()
        {
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        }

        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            // Headless runs should not write a history file.
            if (Application.isBatchMode)
                return;

            if (stage == null || string.IsNullOrEmpty(stage.assetPath))
                return;

            if (!FavoriteAssetsSettings.RecordPrefabHistory)
                return;

            // No "navigating" flag is needed here. That guard exists to keep history-driven opens from
            // truncating a forward stack, and this is a plain chronological list. Reopening a prefab
            // from the history tab re-fires this event for an entry that is already at the top, which
            // Record() reports as "no change".
            if (PrefabHistoryManager.Record(stage.assetPath))
            {
                FavoriteAssetsWindow.RefreshOpenWindows();
            }
        }
    }
}
