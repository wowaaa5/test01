using System;
using UnityEngine;

public interface IDeckView
{
    float UsedOffset { get; }
    void InitializeVisual(int[] cardsToSpawn);
    void AnimateMoveTopCardTo(int cardId, Vector3 targetPosition, float moveDuration, Action<ICardView> onComplete = null);
    Vector3 GetNextCardPosition(float? offset = null);
    void ReceiveMovedCard(ICardView card);
}
