using System;
using System.Collections.Generic;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;



public class AvatarsService
{
    readonly Sprite fallbackAvatar;
    readonly int requestTimeoutInSeconds;

    readonly Dictionary<string, RuntimeCharacterProfile> avatarsRegistry = new(StringComparer.OrdinalIgnoreCase);

    public AvatarsService(Sprite fallbackAvatar, int requestTimeoutInSeconds = 5)
    {
        this.fallbackAvatar = fallbackAvatar;
        this.requestTimeoutInSeconds = requestTimeoutInSeconds;
    }

    public RuntimeCharacterProfile GetProfile(string characterName)
    {
        if (avatarsRegistry.TryGetValue(characterName, out var profile))
        {
            return profile;
        }

        return new RuntimeCharacterProfile(fallbackAvatar, null!);
    }

    public async UniTask PreloadProfilesAsync(List<Avatar> avatarDataList, CancellationToken ct)
    {
        if (avatarDataList == null || avatarDataList.Count == 0) return;

        List<UniTask> downloadTasks = new List<UniTask>();

        foreach (var data in avatarDataList)
        {
            if (string.IsNullOrEmpty(data.name)) continue;

            downloadTasks.Add(ProcessSingleProfileAsync(data, ct));
        }

        await UniTask.WhenAll(downloadTasks);
    }

    public async UniTask CheckAvailableProfilesAsync(HashSet<string> names, CancellationToken ct)
    {
        if (names == null || names.Count == 0) return;

        List<UniTask> waitForNameTasks = new List<UniTask>();

        foreach (var name in names)
        {
            if (avatarsRegistry.ContainsKey(name)) continue;

            waitForNameTasks.Add(UniTask.WaitUntil(() => avatarsRegistry.ContainsKey(name), cancellationToken: ct));
        }

        await UniTask.WhenAll(waitForNameTasks);
    }

    async UniTask ProcessSingleProfileAsync(Avatar data, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(data.url))
        {
            try
            {
                using UnityWebRequest request = UnityWebRequestTexture.GetTexture(data.url);
                request.timeout = requestTimeoutInSeconds;

                await request.SendWebRequest().WithCancellation(ct);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    var resolvedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    avatarsRegistry[data.name] = new RuntimeCharacterProfile(resolvedSprite, data.position);
                }
            }
            catch { }
        }
    }
}