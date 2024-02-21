using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace NATFrameWork.NatAsset.Runtime
{
    internal static class CommonFunc
    {
        internal static void LoadScene(string sceneName, Priority priority, RunModel runModel,
            LoadSceneMode loadSceneMode, out Scene scene, out AsyncOperation operation)
        {
            if (runModel == RunModel.Async)
            {
                operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if (operation != null)
                {
                    operation.priority = (int) priority;
                    operation.allowSceneActivation = true;
                    scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                }
                else
                {
                    scene = default;
                }
            }
            else
            {
                scene = SceneManager.LoadScene(sceneName,
                    new LoadSceneParameters() {loadSceneMode = loadSceneMode});
                operation = null;
            }
        }

#if UNITY_EDITOR
        internal static void EditorLoadScene(string sceneName, Priority priority, RunModel runModel,
            LoadSceneMode loadSceneMode, out Scene scene, out AsyncOperation operation)
        {
            if (runModel == RunModel.Async)
            {
                operation = EditorSceneManager.LoadSceneAsyncInPlayMode(sceneName,
                    new LoadSceneParameters() {loadSceneMode = loadSceneMode});
                if (operation != null)
                {
                    operation.priority = (int) priority;
                    operation.allowSceneActivation = true;
                    scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                }
                else
                {
                    scene = default;
                }
            }
            else
            {
                scene = EditorSceneManager.LoadSceneInPlayMode(sceneName,
                    new LoadSceneParameters() {loadSceneMode = loadSceneMode});
                operation = null;
            }
        }
#endif
    }
}