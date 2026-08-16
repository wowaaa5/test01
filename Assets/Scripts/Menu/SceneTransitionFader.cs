using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionFader : MonoBehaviour
{
    [SerializeField] Image fadeImage;
    [SerializeField] ButtonUI closeButton;
#if UNITY_EDITOR
    [SerializeField] UnityEditor.SceneAsset menuSceneAsset;
#endif
    [SerializeField, ReadOnly] SceneMenuData backScene;

    public static SceneTransitionFader Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var prefab = Resources.Load<SceneTransitionFader>(nameof(SceneTransitionFader));
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            Instance = instance;
            DontDestroyOnLoad(instance.gameObject);
        }
    }

    public async void TransitionTo(string sceneName, bool hideCloseButton = false)
    {
        fadeImage.DOKill();
        closeButton.transform.DOKill();
        fadeImage.raycastTarget = true;

        if (hideCloseButton)
        {
            closeButton.Hide();
        }

        await fadeImage.DOFade(1, 0.5f).ToUniTask();

        await LoadSceneAsync(sceneName);

        _ = fadeImage.DOFade(0, 0.5f).OnComplete(() =>
        {
            fadeImage.raycastTarget = false;
            if (!hideCloseButton)
            {
                closeButton.Show();
            }
        });
    }

    void Awake()
    {
        closeButton.gameObject.SetActive(false);
        closeButton.SetOnCLickListener(TransitionBackToMenu);
    }

    void Start()
    {
        fadeImage.DOFade(0, 0.5f).SetEase(Ease.InOutSine);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (menuSceneAsset != null)
        {
            backScene.Fill(menuSceneAsset);
        }
    }
#endif

    void TransitionBackToMenu() => TransitionTo(backScene.sceneName, hideCloseButton: true);

    async UniTask LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            await UniTask.Yield();
        }
    }
}