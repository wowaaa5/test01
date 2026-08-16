using DG.Tweening;

public sealed class BoardPresenter
{
    readonly IBoardView boardView;
    readonly IDeckView sourceDeckView;
    readonly IDeckView targetDeckView;
    readonly BoardModel model;

    Sequence moveSequence;
    float moveFrequencyInSeconds;
    bool isCompleted;

    public BoardPresenter(IBoardView boardView, IDeckView sourceDeckView, IDeckView targetDeckView)
    {
        this.boardView = boardView;
        this.sourceDeckView = sourceDeckView;
        this.targetDeckView = targetDeckView;
        model = new BoardModel();
    }

    public void Initialize(int cardsToSpawn, int moveFrequencyInMilliseconds)
    {
        model.Initialize(cardsToSpawn);
        boardView.InitBoardView(model.SourceDeckIds, model.TargetDeckIds);
        moveFrequencyInSeconds = moveFrequencyInMilliseconds / 1000f;
    }

    public void StartSequence()
    {
        moveSequence?.Kill();
        moveSequence = DOTween.Sequence()
            .AppendCallback(TryMoveNextCard)
            .AppendInterval(moveFrequencyInSeconds)
            .SetLoops(-1).SetDelay(.5f, false)
                .SetLink(boardView.ViewGameObject);
    }

    public void StopSequence()
    {
        moveSequence?.Kill();
    }

    void TryMoveNextCard()
    {
        if (isCompleted) return;

        if (!model.TryMoveCard(out int movedCardId))
        {
            CompleteGame("All cards moved!");
            return;
        }

        PlayCardMove(movedCardId, moveFrequencyInSeconds * 0.5f, model.SourceDeckCount == 0);
    }

    void PlayCardMove(int movedCardId, float moveDuration, bool isComplete)
    {
        var targetPosition = targetDeckView.GetNextCardPosition(sourceDeckView.UsedOffset);

        sourceDeckView.AnimateMoveTopCardTo(movedCardId, targetPosition, moveDuration, card =>
        {
            targetDeckView.ReceiveMovedCard(card);
            if (isComplete)
            {
                CompleteGame("All cards moved!");
            }
        });
    }

    void CompleteGame(string message)
    {
        if (isCompleted) return;

        isCompleted = true;
        moveSequence?.Kill();
        boardView.ShowCompletionMessage(message);
    }
}
