using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using System;


public class DeckView : MonoBehaviour, IDeckView
{
    [SerializeField] TMP_Text counterText;
    [SerializeField] CardView cardPrefab;
    [SerializeField] Vector2 defaultSpawnOffset;
    [SerializeField] Transform startPositionX;
    [SerializeField] Transform endPositionX;

    Stack<ICardView> cards = new Stack<ICardView>();


    public float UsedOffset { get; private set; }

    public void InitializeVisual(int[] cardsToSpawn)
    {
        foreach (var card in cards)
        {
            card.Destroy();
        }
        cards.Clear();

        UsedOffset = CalculateOffset(cardsToSpawn.Length);

        for (int i = 0; i < cardsToSpawn.Length; i++)
        {
            var spawnPosition = GetNextCardPosition(UsedOffset);
            var card = Instantiate(cardPrefab, spawnPosition, Quaternion.identity, transform);
            card.Initialize(cardsToSpawn[cardsToSpawn.Length - 1 - i]);
            cards.Push(card);
        }

        RefreshCounter();
    }

    public void AnimateMoveTopCardTo(int cardId, Vector3 targetPosition, float moveDuration, Action<ICardView> onComplete = null)
    {
        if (cards.Count == 0) return;

        if (cards.Peek().Id != cardId)
        {
            Debug.LogError($"Card ID mismatch. Expected: {cardId}, but got: {cards.Peek().Id}");
            return;
        }

        var cardToMove = cards.Pop();
        cardToMove.MoveTo(targetPosition, moveDuration, () =>
        {
            onComplete?.Invoke(cardToMove);
        });

        RefreshCounter(false);
    }

    public Vector3 GetNextCardPosition(float? offset = null)
    {
        var offsetToUse = offset ?? CalculateOffset(cards.Count + 1);
        return startPositionX.position + new Vector3(cards.Count * offsetToUse, 0, 0);
    }

    public void ReceiveMovedCard(ICardView card)
    {
        cards.Push(card);
        card.AssignToDeck(transform, cards.Count);

        RefreshCounter(true);
    }

    float CalculateOffset(int amountOfCards)
    {
        var offset = defaultSpawnOffset.x;
        if (amountOfCards > 1)
        {
            var placementDistance = (endPositionX.position.x - startPositionX.position.x) / (amountOfCards - 1);
            if (Mathf.Abs(placementDistance) < Mathf.Abs(offset))
            {
                offset = placementDistance;
            }
        }
        return offset;
    }

    void RefreshCounter(bool? scaleUp = null)
    {
        counterText.text = $"{cards.Count}";

        if (scaleUp.HasValue)
        {
            var scaleTarget = scaleUp.Value ? Vector3.one * 1.2f : Vector3.one * 0.8f;
            DOTween.Sequence()
                .Append(counterText.transform.DOScale(scaleTarget, 5 / 60f).From().SetEase(Ease.OutSine))
                .Append(counterText.transform.DOScale(Vector3.one, 3 / 60f).SetEase(Ease.OutSine))
                .SetLink(gameObject);
        }
    }
}
