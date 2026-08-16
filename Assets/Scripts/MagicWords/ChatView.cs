using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ChatView : MonoBehaviour
{
    [SerializeField] ChatConfig configuration;
    [SerializeField] Transform messageListContainer;
    [SerializeField] MessageView messagePrefab;
    [SerializeField] float messageSpawnDelayDuration;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] GameObject errorBanner;
    [SerializeField] GameObject loadingIndicator;

    List<MessageView> messageViews = new List<MessageView>();

    readonly Vector3[] viewCorners = new Vector3[4];


    ChatPresenter chatPresenter;
    void Awake()
    {
        chatPresenter = new ChatPresenter(this, configuration, messageSpawnDelayDuration);

        scrollRect.onValueChanged.RemoveAllListeners();
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

        errorBanner.SetActive(false);
    }

    void OnScrollValueChanged(Vector2 scrollPosition)
    {
        scrollRect.viewport.GetWorldCorners(viewCorners);
        float minY = viewCorners[0].y;
        float maxY = viewCorners[1].y;
        foreach (var messageView in messageViews)
        {
            messageView.CheckVisibility(minY, maxY);
        }
    }

    void Start()
    {
        _ = chatPresenter.InitializeAsync(destroyCancellationToken);
    }

    public void SpawnMessageBubble(RuntimeLine line, RuntimeCharacterProfile profile)
    {
        var messageInstance = Instantiate(messagePrefab, messageListContainer);
        messageInstance.SetMessage(line, profile);
        messageViews.Add(messageInstance);
    }

    public void DisplayLoadingIndicator(bool isVisible)
    {
        loadingIndicator.transform.DOKill();
        loadingIndicator.SetActive(isVisible);

        if (!isVisible)
            return;

        loadingIndicator.transform.DOLocalRotate(new Vector3(0, 0, -360f), 1.2f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetLink(gameObject);
    }

    public void DisplayNetworkErrorBanner()
    {
        errorBanner.SetActive(true);
    }
}
