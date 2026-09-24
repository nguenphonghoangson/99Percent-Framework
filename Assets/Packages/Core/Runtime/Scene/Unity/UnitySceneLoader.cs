using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace NinetyNine.Core
{
    /// <summary>Single-mode <see cref="SceneManager" /> loads by build-settings scene name.</summary>
    internal sealed class UnitySceneLoader : ISceneLoader
    {
        private Task _running = Task.CompletedTask;

        public string ActiveScene => SceneManager.GetActiveScene().name;

        public bool IsLoading => !_running.IsCompleted;

        public object Args { get; private set; }

        public Task LoadAsync(string sceneName, object args = null)
        {
            if (string.IsNullOrEmpty(sceneName)) throw new ArgumentException("Scene name is empty.", nameof(sceneName));
            if (IsLoading) return _running;

            Args = args;
            return _running = Load(sceneName);
        }

        private static Task Load(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
                return Task.FromException(new ArgumentException($"Scene '{sceneName}' is not in Build Settings.", nameof(sceneName)));

            var completion = new TaskCompletionSource<bool>();
            operation.completed += _ => completion.TrySetResult(true);
            return completion.Task;
        }
    }

    /// <summary>Provides <see cref="ISceneLoader" />. No dependencies.</summary>
    public sealed class SceneModule : IModule
    {
        public string Name => "Scene";

        public void Install(IServiceRegistry registry) => registry.Register<ISceneLoader>(new UnitySceneLoader());
    }
}
