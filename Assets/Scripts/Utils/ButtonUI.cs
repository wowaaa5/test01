using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonUI : MonoBehaviour
{
    [SerializeField] TMP_Text buttonText;
    [SerializeField] Button button;

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }

    public void SetText(string text)
    {
        if (buttonText != null)
            buttonText.text = text;
    }

    public void SetOnCLickListener(UnityEngine.Events.UnityAction action, bool lockInteractable = true)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            action?.Invoke();
            if (lockInteractable)
                button.interactable = false;
        });
    }

    public void Hide()
    {
        button.interactable = false;
        button.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => button.gameObject.SetActive(false));
    }

    public void Show()
    {
        button.gameObject.SetActive(true);
        button.transform.DOScale(Vector3.one, 0.2f).From(Vector3.zero)
            .SetEase(Ease.OutBack)
            .OnComplete(() => button.interactable = true);
    }

    internal void SetButtonImage(Sprite sprite)
    {
        button.image.sprite = sprite;
    }
}