using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

public class MenuSceneBehaviour : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] EditorSceneData[] sceneAssetsData;
#endif
    [SerializeField, ReadOnly] SceneMenuData[] sceneNames;

    [SerializeField] Transform scenesListVisualRoot;
    [SerializeField] ButtonUI buttonPrefab;

    void Start()
    {
        for (int i = 0; i < sceneNames.Length; i++)
        {
            var scene = sceneNames[i];
            var button = Instantiate(buttonPrefab, scenesListVisualRoot);
            button.SetText(scene.displayTitle);
            if (scene.buttonAsset != null)
            {
                button.SetButtonImage(scene.buttonAsset);
            }
            button.SetOnCLickListener(() => SceneTransitionFader.Instance.TransitionTo(scene.sceneName));
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        var scenesInBuildSettings = UnityEditor.EditorBuildSettings.scenes
            .Where(s => s.enabled).Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToHashSet();
        if (sceneAssetsData != null && sceneAssetsData.Length > 0)
        {
            sceneNames = sceneAssetsData.Where(s => s != null).Select(s => SceneMenuData.Create(s)).ToArray();

            var missingScenes = sceneNames.Where(s => !scenesInBuildSettings.Contains(s.sceneName)).ToArray();
            if (missingScenes.Length > 0)
            {
                foreach (var missingScene in missingScenes)
                {
                    Debug.LogWarning($"<b><color=orange>[Scene Validation]</color></b> Scene '<b>{missingScene.sceneName}</b>' is referenced in MenuSceneBehaviour but is missing or disabled in your Build Settings!", this);
                }
            }
        }
    }
#endif
}