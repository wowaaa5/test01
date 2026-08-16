using System;
using UnityEngine;

public class RuntimeCharacterProfile
{
    public enum AvatarPosition
    {
        Left,
        Right
    }

    public Sprite AvatarSprite { get; }
    public AvatarPosition Position { get; }
    public string Name { get; }

    public RuntimeCharacterProfile(Sprite sprite, string position)
    {
        AvatarSprite = sprite;
        Position = (!string.IsNullOrEmpty(position) && Enum.TryParse(position, true, out AvatarPosition parsedPosition)) ?
            parsedPosition : AvatarPosition.Right;
    }
}