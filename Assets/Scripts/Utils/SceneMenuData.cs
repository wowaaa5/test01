using System;
using UnityEngine;

[Serializable]
public class EditorSceneData
{
#if UNITY_EDITOR
    public UnityEditor.SceneAsset sceneAsset;
#endif
    public Sprite buttonAsset;
}

[Serializable]
public class SceneMenuData : EditorSceneData
{
    [HideInInspector] public string sceneName;
    public string displayTitle;

#if UNITY_EDITOR
    internal static SceneMenuData Create(EditorSceneData data)
    {
        return new()
        {
            sceneAsset = data.sceneAsset,
            sceneName = data.sceneAsset.name,
            displayTitle = UnityEditor.ObjectNames.NicifyVariableName(data.sceneAsset.name),
            buttonAsset = data.buttonAsset
        };
    }

    internal SceneMenuData Fill(UnityEditor.SceneAsset sceneAsset)
    {
        this.sceneAsset = sceneAsset;
        sceneName = sceneAsset.name;
        displayTitle = UnityEditor.ObjectNames.NicifyVariableName(sceneAsset.name);

        return this;
    }
#endif
}