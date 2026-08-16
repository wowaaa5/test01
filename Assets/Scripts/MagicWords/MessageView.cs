using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageView : MonoBehaviour
{
    [SerializeField] TMP_Text messageText;
    [SerializeField] TMP_Text characterNameText;

    [SerializeField] Image avatarImage;
    [SerializeField] Transform leftAvatarAnchor;
    [SerializeField] Transform rightAvatarAnchor;

    [SerializeField] Transform messageRoot;
    [SerializeField] Transform leftMessageAnchor;
    [SerializeField] Transform rightMessageAnchor;

    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform rectTransform;


    public Tween SetMessage(RuntimeLine line, RuntimeCharacterProfile avatar)
    {
        messageText.text = line.Text;
        characterNameText.text = line.Name;
        avatarImage.sprite = avatar.AvatarSprite;

        var (avatarParent, messageParent) = avatar.Position switch
        {
            RuntimeCharacterProfile.AvatarPosition.Left => (leftAvatarAnchor, leftMessageAnchor),
            RuntimeCharacterProfile.AvatarPosition.Right => (rightAvatarAnchor, rightMessageAnchor),
            _ => (rightAvatarAnchor, rightMessageAnchor)
        };

        avatarImage.transform.SetParent(avatarParent, false);
        messageRoot.transform.SetParent(messageParent, false);

        var tweenSequence = DOTween.Sequence()
            .Append(messageRoot.DOScale(Vector3.one, 0.5f).From(Vector3.zero).SetEase(Ease.OutBack))
            .Join(transform.DOScale(Vector3.one, 0.5f).From(Vector3.zero).SetEase(Ease.OutBack, .5f))
            .Join(messageRoot.DOLocalRotate(Vector3.zero, 0.5f).From(Vector3.forward * 10).SetEase(Ease.OutBack, 2))
            .Join(avatarImage.DOFade(1f, 0.5f).From(0f))
            .SetLink(gameObject);

        return tweenSequence;
    }

    public void CheckVisibility(float viewportMinY, float viewportMaxY)
    {
        
        Vector3[] msgCorners = new Vector3[4];
        rectTransform.GetWorldCorners(msgCorners);
        float msgMinY = msgCorners[0].y;
        float msgMaxY = msgCorners[2].y;
        bool isVisible = (msgMaxY >= viewportMinY) && (msgMinY <= viewportMaxY);

        canvasGroup.alpha = isVisible ? 1f : 0f;
    }

}
