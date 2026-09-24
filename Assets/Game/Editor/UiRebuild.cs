using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Throws away the generated UI prefabs and builds them again from <see cref="UiSetup" />. Use after changing
    ///     the builders; hand edits to those prefabs are lost (the old files go to the OS trash). The catalog keeps
    ///     its tuned entries and only has the broken prefab references repaired.
    /// </summary>
    public static class UiRebuild
    {
        [MenuItem("NinetyNine/Setup/Rebuild UI Prefabs")]
        public static void RebuildPrefabs()
        {
            var paths = PrefabPaths().Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).ToArray();
            if (paths.Length > 0 && !EditorUtility.DisplayDialog("Rebuild UI Prefabs",
                    "Move these prefabs to the trash and rebuild them?\n\n" + string.Join("\n", paths), "Rebuild", "Cancel"))
                return;

            foreach (var path in paths) AssetDatabase.MoveAssetToTrash(path);

            FrameworkSetup.CreateAll();
            Debug.Log($"[UiRebuild] Rebuilt {paths.Length} UI prefab(s).");
        }

        // Every *Path constant on UiSetup that names a prefab, so a newly added screen is rebuilt without touching this file.
        private static string[] PrefabPaths() =>
            typeof(UiSetup).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Where(p => p.EndsWith(".prefab"))
                .ToArray();
    }
}
