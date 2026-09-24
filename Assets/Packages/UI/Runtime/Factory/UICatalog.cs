using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.UI
{
    [Serializable]
    public sealed class UICatalogEntry
    {
        [Tooltip("By convention the feature id (\"shop\", \"daily_reward\").")]
        public string key;

        public UIView prefab;

        [Tooltip("Keep the instance after closing and reuse it next time (frequent screens). Off = destroy on close.")]
        public bool keepInstance = true;
    }

    /// <summary>key → prefab for every screen and popup, plus the root canvas settings.</summary>
    [CreateAssetMenu(menuName = "NinetyNine/UI/UI Catalog", fileName = "UICatalog")]
    public sealed class UICatalog : ScriptableObject
    {
        public List<UICatalogEntry> entries = new();

        [Header("Root canvas")]
        public Vector2 referenceResolution = new(1080, 1920);

        [Tooltip("Expand keeps the whole reference layout on screen at any aspect ratio; extra space goes to the longer axis.")]
        public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        [Tooltip("Only used when the match mode is MatchWidthOrHeight.")]
        [Range(0, 1)] public float matchWidthOrHeight = 0.5f;
        public int sortingOrder = 100;

        public UICatalogEntry Find(string key) => entries.Find(e => e.key == key);

        private void OnValidate()
        {
            var keys = new HashSet<string>();
            foreach (var entry in entries)
            {
                if (!keys.Add(entry.key)) Debug.LogWarning($"[UICatalog] Duplicate key '{entry.key}'.", this);
                if (!entry.prefab) Debug.LogWarning($"[UICatalog] '{entry.key}' has no prefab.", this);
            }
        }
    }
}
