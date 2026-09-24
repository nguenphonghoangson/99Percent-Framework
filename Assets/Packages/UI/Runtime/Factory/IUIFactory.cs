using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NinetyNine.UI
{
    /// <summary>
    ///     Creates and recycles views. The default builds from <see cref="UICatalog" /> prefabs; an Addressables
    ///     factory plugs in behind the same contract when UI moves to remote content.
    ///     <para>
    ///         Keys resolve against the catalog of the active context (Home UI, Gameplay UI) first, then the global
    ///         catalog given to <see cref="UIModule" />. A key belongs to exactly one catalog.
    ///     </para>
    /// </summary>
    public interface IUIFactory : IDisposable
    {
        bool Has(string key);

        /// <summary>
        ///     Switches the context catalog; set by each context scene's entry. Idle instances of the previous
        ///     context are destroyed, and its views still open are destroyed instead of cached when released — so a
        ///     scene's UI never outlives the scene. Global views are unaffected. Null = global catalog only.
        /// </summary>
        void SetContext(UICatalog catalog);

        /// <summary>An inactive instance. Throws for an unknown key — a missing screen is a build error.</summary>
        UIView Create(string key);

        void Release(UIView view);
    }

    internal sealed class PrefabUIFactory : IUIFactory
    {
        private readonly UIInstanceCache _cache = new();
        private readonly UICatalog _global;
        private readonly Transform _staging;
        private UICatalog _context;

        /// <param name="staging">
        ///     Inactive parent new instances are created under, so Awake/OnEnable only run once the navigator
        ///     actually opens the view — not during instantiation, before it has its arguments.
        /// </param>
        public PrefabUIFactory(UICatalog catalog, Transform staging)
        {
            _global = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
            _staging = staging;
        }

        public bool Has(string key) => Find(key)?.prefab;

        public void SetContext(UICatalog catalog)
        {
            if (catalog == _context) return;

            _context = catalog;
            foreach (var view in _cache.Drain(key => _global.Find(key) == null)) Destroy(view.gameObject);
        }

        public UIView Create(string key)
        {
            var entry = Find(key);
            if (entry == null || !entry.prefab) throw new ArgumentException($"No UI registered for '{key}'.", nameof(key));

            if (_cache.TryTake(key, out var cached)) return cached;

            var view = Object.Instantiate(entry.prefab, _staging, false);
            view.name = entry.prefab.name;
            view.gameObject.SetActive(false);
            return view;
        }

        public void Release(UIView view)
        {
            if (!view) return;

            // A view of a context that is no longer active resolves to nothing here and is destroyed.
            var entry = Find(view.Key);
            if (entry is { keepInstance: true } && _cache.Put(view.Key, view)) return;

            Destroy(view.gameObject);
        }

        public void Dispose()
        {
            foreach (var view in _cache.Drain()) Destroy(view.gameObject);
        }

        private UICatalogEntry Find(string key) => (_context ? _context.Find(key) : null) ?? _global.Find(key);

        private static void Destroy(GameObject target)
        {
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }
    }
}
