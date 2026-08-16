using UnityEngine;

[CreateAssetMenu(fileName = "ChatConfig", menuName = "MagicWords/ChatConfig")]
public class ChatConfig : ScriptableObject
{
    [SerializeField] string endpointUrl;
    [SerializeField] Sprite fallbackAvatar;
    [SerializeField] int requestTimeoutSeconds;
    [SerializeField] int avatarTimeoutSeconds;

    public string EndpointUrl => endpointUrl;
    public Sprite FallbackAvatar => fallbackAvatar;
    public int RequestTimeoutSeconds => requestTimeoutSeconds;
    public int AvatarTimeoutSeconds => avatarTimeoutSeconds;
}