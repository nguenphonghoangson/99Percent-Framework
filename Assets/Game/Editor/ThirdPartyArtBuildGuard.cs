using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Art under <c>_ThirdPartyReference/</c> is not ours to ship. A release build that reaches any of it — from
    ///     a build scene or a Resources folder — fails with the list of offending assets; a development build only
    ///     warns, so the team can still test on device with the reference look.
    /// </summary>
    internal sealed class ThirdPartyArtBuildGuard : IPreprocessBuildWithReport
    {
        public const string Marker = "/_ThirdPartyReference/";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var hits = FindReferences(BuildRoots());
            if (hits.Length == 0) return;

            var message = $"[ThirdPartyArtBuildGuard] {hits.Length} asset(s) from {Marker} are in this build, e.g.\n  " +
                          string.Join("\n  ", hits.Take(8)) +
                          "\nReplace them with Percas art (see Assets/Game/Art/_ThirdPartyReference/PixelFlow/README.md).";
            if ((report.summary.options & BuildOptions.Development) != 0) Debug.LogWarning(message);
            else throw new BuildFailedException(message);
        }

        public static string[] BuildRoots() =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
                .Concat(AssetDatabase.GetAllAssetPaths().Where(p => p.Contains("/Resources/")))
                .ToArray();

        public static string[] FindReferences(IEnumerable<string> roots) =>
            AssetDatabase.GetDependencies(roots.ToArray(), true).Where(p => p.Contains(Marker)).Distinct().OrderBy(p => p).ToArray();
    }
}
