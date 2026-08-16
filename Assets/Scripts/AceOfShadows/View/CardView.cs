using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering;
using System;

public class CardView : MonoBehaviour, ICardView
{
    [SerializeField] SortingGroup sortingGroup;
    [SerializeField] SpriteRenderer shadowRenderer;

    const int FlyingCardSortOffset = 1000;

    public int Id { get; private set; }

    public void Initialize(int id)
    {
        Id = id;
    }

    public void SetSortingOrder(int order)
    {
        sortingGroup.sortingOrder = order;
    }

    public void IncreaseSortingOrderBy(int amount)
    {
        sortingGroup.sortingOrder += amount;
    }

    public void MoveTo(Vector3 targetPosition, float moveDuration, Action onComplete = null)
    {
        transform.SetParent(null);
        IncreaseSortingOrderBy(FlyingCardSortOffset);

        var startDuration = 3 / 60f;
        var endDuration = 2 * startDuration;
        var restDuration = moveDuration - startDuration - endDuration;
        float movementHalfTime = startDuration + (moveDuration / 2);

        DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one * .9f, startDuration).SetEase(Ease.OutSine))
            .Append(transform.DOMoveY(targetPosition.y, moveDuration).SetEase(Ease.OutBack))
            .Join(transform.DOMoveX(targetPosition.x, moveDuration).SetEase(Ease.OutSine))
            .Join(transform.DOScale(Vector3.one * 1.2f, restDuration / 2).SetEase(Ease.OutSine))
            .Insert(movementHalfTime, transform.DOScale(Vector3.one, restDuration / 2).SetEase(Ease.OutSine))
            .Append(transform.DOScale(Vector3.one * 1.1f, endDuration / 2).SetEase(Ease.OutSine))
            .Append(transform.DOScale(Vector3.one, endDuration / 2).SetEase(Ease.OutSine))
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke()); ;
    }

    public void AssignToDeck(Transform deckTransform, int count)
    {
        transform.SetParent(deckTransform);
        SetSortingOrder(count);
    }

    public void Destroy() => Destroy(gameObject);
}
