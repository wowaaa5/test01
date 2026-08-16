using UnityEngine;

public interface IBoardView
{
    GameObject ViewGameObject { get; }
    void InitBoardView(int[] sourceCardsToSpawn, int[] targetCardsToSpawn);
    void ShowCompletionMessage(string message);
}
