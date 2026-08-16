using System.Collections.Generic;
using System.Linq;

public sealed class DeckModel
{
    readonly Stack<CardModel> cards = new Stack<CardModel>();

    public int Count => cards.Count;

    public int[] CardIds => cards.Select(card => card.Id).ToArray();

    public void Initialize(IEnumerable<CardModel> initialCards)
    {
        cards.Clear();
        foreach (var card in initialCards)
        {
            cards.Push(card);
        }
    }

    public bool TryPop(out CardModel card)
    {
        if (cards.Count == 0)
        {
            card = null;
            return false;
        }

        card = cards.Pop();
        return true;
    }

    public void Push(CardModel card)
    {
        cards.Push(card);
    }
}
