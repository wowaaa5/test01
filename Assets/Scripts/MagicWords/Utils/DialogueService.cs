using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading;
using Cysharp.Threading.Tasks;

public class DialogueService
{
    readonly int requestTimeoutInSeconds;

    public DialogueService(int requestTimeoutInSeconds = 10)
    {
        this.requestTimeoutInSeconds = requestTimeoutInSeconds;
    }

    public async UniTask<Dialogue> FetchDialogueDataAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("[DialogueService] Target configuration URL is null or empty.");
            return null;
        }

        try
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = requestTimeoutInSeconds;

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    Debug.LogWarning("[DialogueService] Network request cancelled by caller pipeline.");
                    return null;
                }
                await UniTask.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DialogueService] Network fetch failed. Code: {request.responseCode}. Error: {request.error}");
                return null;
            }

            string rawJson = request.downloadHandler.text;
            return JsonUtility.FromJson<Dialogue>(rawJson);
        }
        catch (Exception ex)
        {
            Debug.LogException(new Exception($"[DialogueService] Fatal exception encountered during network sync: {ex.Message}"));
            return null;
        }
    }
}