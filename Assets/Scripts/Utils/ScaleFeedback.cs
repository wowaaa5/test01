using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScaleFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] Transform feedbackTransform = null!;
    [SerializeField] float scaleFactorX2 = 1.04f;
    [SerializeField] float scaleFactorY = 0.95f;
    [SerializeField] bool useRelativeRectScaling = false;
    [SerializeField] bool animate = true;
    [SerializeField] float animationDownAmplitudeIn = 2f;
    [SerializeField] float animationUpAmplitudeIn = 2f;

    Selectable selectable;
    Vector3 baseScale;
    Vector2 initialRectSize;

    Tween activeFeedbackTween;

    private const float ElasticPeriod = 0.4f;

    public void ResetToBaseScale()
    {
        activeFeedbackTween?.Kill();
        feedbackTransform.localScale = baseScale;
    }

    public void SetFeedbackTransform(Transform newTransform) => feedbackTransform = newTransform;

    void Awake()
    {
        Assert.IsNotNull(feedbackTransform);

        if (useRelativeRectScaling)
        {
            Assert.IsTrue(feedbackTransform is RectTransform, "Relative scaling requires a RectTransform.");
            initialRectSize = ((RectTransform)feedbackTransform).rect.size;
        }

        selectable = GetComponent<Selectable>();
        baseScale = feedbackTransform.localScale;
    }

    void OnEnable() => ResetToBaseScale();

    void OnDisable() => activeFeedbackTween?.Kill();

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable)
            return;

        Vector3 targetScale = CalculateTargetScale();

        activeFeedbackTween?.Kill();

        if (animate)
        {
            activeFeedbackTween = feedbackTransform.DOScale(targetScale, 0.6f)
                .SetEase(Ease.OutElastic, animationDownAmplitudeIn, ElasticPeriod)
                .SetLink(gameObject);
        }
        else
        {
            feedbackTransform.localScale = targetScale;
        }
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerPress != gameObject)
            return;

        activeFeedbackTween?.Kill();

        if (animate)
        {
            activeFeedbackTween = feedbackTransform.DOScale(baseScale, 0.7f)
                .SetEase(Ease.OutElastic, animationUpAmplitudeIn, ElasticPeriod)
                .SetLink(gameObject);
        }
        else
        {
            feedbackTransform.localScale = baseScale;
        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerPress == gameObject)
            ((IPointerUpHandler)this).OnPointerUp(eventData);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerPress == gameObject)
            ((IPointerDownHandler)this).OnPointerDown(eventData);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable)
            return;
    }

    private Vector3 CalculateTargetScale()
    {
        if (useRelativeRectScaling)
        {
            if (initialRectSize.x == 0 || initialRectSize.y == 0)
                initialRectSize = ((RectTransform)feedbackTransform).rect.size;

            Assert.IsTrue(initialRectSize.x != 0 && initialRectSize.y != 0, gameObject.name + ": Initial size must be != 0.");

            float deltaPixelsX = (1 - scaleFactorX2) * initialRectSize.x;
            float deltaPixelsY = (1 - scaleFactorY) * initialRectSize.y;
            float newScaleX = (initialRectSize.x - deltaPixelsX) / initialRectSize.x;
            float newScaleY = (initialRectSize.y - deltaPixelsY) / initialRectSize.y;
            return new Vector3(newScaleX, newScaleY, baseScale.z);
        }

        return new Vector3(baseScale.x * scaleFactorX2, baseScale.y * scaleFactorY, baseScale.z);
    }
}