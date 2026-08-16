using System.Collections.Generic;

public sealed class BoardModel
{
    DeckModel SourceDeck { get; } = new DeckModel();
    DeckModel TargetDeck { get; } = new DeckModel();

    public int SourceDeckCount => SourceDeck.Count;
    public int TargetDeckCount => TargetDeck.Count;

    public int[] SourceDeckIds => SourceDeck.CardIds;
    public int[] TargetDeckIds => TargetDeck.CardIds;

    public void Initialize(int cardsToSpawn)
    {
        var sourceCards = new List<CardModel>(cardsToSpawn);
        for (int i = 0; i < cardsToSpawn; i++)
        {
            sourceCards.Add(new CardModel(i));
        }

        SourceDeck.Initialize(sourceCards);
        TargetDeck.Initialize(new List<CardModel>());
    }

    public bool TryMoveCard(out int id)
    {
        id = -1;
        if (!SourceDeck.TryPop(out CardModel movedCard))
        {
            return false;
        }

        id = movedCard.Id;
        TargetDeck.Push(movedCard);

        return true;
    }
}
