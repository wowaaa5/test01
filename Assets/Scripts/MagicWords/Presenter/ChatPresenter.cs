using System.Threading;
using Cysharp.Threading.Tasks;
using System.Linq;
using System;
using UnityEngine;

public class ChatPresenter
{
    readonly ChatView chatView;
    readonly DialogueService dialogueService;
    readonly AvatarsService avatarService;
    readonly string endpointUrl;

    readonly float messageDisplayDuration;

    public ChatPresenter(ChatView view, ChatConfig config, float messageDisplayDuration)
    {
        chatView = view;
        dialogueService = new DialogueService(config.RequestTimeoutSeconds);
        avatarService = new AvatarsService(config.FallbackAvatar, config.AvatarTimeoutSeconds);
        endpointUrl = config.EndpointUrl;
        this.messageDisplayDuration = messageDisplayDuration;
    }

    public async UniTask InitializeAsync(CancellationToken ct)
    {
        chatView.DisplayLoadingIndicator(true);

        var result = await dialogueService.FetchDialogueDataAsync(endpointUrl, ct);

        if (result == null || result.dialogue.Count == 0)
        {
            chatView.DisplayNetworkErrorBanner();
            return;
        }

        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            var avatarsLoadingTask = avatarService.PreloadProfilesAsync(result.avatars, cts.Token)
                .SuppressCancellationThrow();

            var availableAvatarNames = result.avatars.Select(a => a.name).ToHashSet();
            var requiredProfiles = result.dialogue.Select(d => d.name)
                .Where(availableAvatarNames.Contains).ToHashSet();

            var checkRequiredProfilesTask = avatarService.CheckAvailableProfilesAsync(requiredProfiles, cts.Token)
                .SuppressCancellationThrow();

            await UniTask.WhenAny(avatarsLoadingTask, checkRequiredProfilesTask);
            cts.Cancel();
        }

        chatView.DisplayLoadingIndicator(false);


        foreach (var line in result.dialogue)
        {
            if (ct.IsCancellationRequested) return;

            var profile = avatarService.GetProfile(line.name);
            chatView.SpawnMessageBubble(new RuntimeLine(line), profile);

            await UniTask.Delay(TimeSpan.FromSeconds(messageDisplayDuration), cancellationToken: ct);
        }
    }
}