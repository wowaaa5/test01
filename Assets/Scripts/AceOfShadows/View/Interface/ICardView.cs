using System;
using UnityEngine;

public interface ICardView
{
    int Id { get; }
    void AssignToDeck(Transform deckTransform, int count);
    void Destroy();
    void MoveTo(Vector3 targetPosition, float moveDuration, Action onComplete);
}