using UnityEngine;
using DG.Tweening;
using TMPro;

public class BoardView : MonoBehaviour, IBoardView
{
    [Header("Decks")]
    [SerializeField] DeckView sourceDeck;
    [SerializeField] DeckView targetDeck;

    [Header("Config")]
    [SerializeField] int cardsToSpawn;
    [SerializeField] int moveFrequencyInMilliseconds;

    [Header("UI")]
    [SerializeField] TMP_Text finalMessageText;

    BoardPresenter presenter;

    public GameObject ViewGameObject => gameObject;

    void Start()
    {
        presenter = new BoardPresenter(this, sourceDeck, targetDeck);
        presenter.Initialize(cardsToSpawn, moveFrequencyInMilliseconds);
        presenter.StartSequence();
    }

    void OnDestroy()
    {
        presenter?.StopSequence();
    }

    public void InitBoardView(int[] sourceCardsToSpawn, int[] targetCardsToSpawn)
    {
        sourceDeck.InitializeVisual(sourceCardsToSpawn);
        targetDeck.InitializeVisual(targetCardsToSpawn);
        finalMessageText.gameObject.SetActive(false);
    }

    public void ShowCompletionMessage(string message)
    {
        finalMessageText.text = message;
        finalMessageText.gameObject.SetActive(true);

        finalMessageText.transform
            .DOScale(Vector3.one, 0.5f).From(Vector3.zero)
            .SetEase(Ease.OutBack);
    }
}
