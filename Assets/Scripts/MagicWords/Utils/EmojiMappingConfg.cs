using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmojiMappingConfig", menuName = "MagicWords/Emoji Mapping Config")]
public class EmojiMappingConfig : ScriptableObject
{
    [Serializable]
    public struct EmojiEntry
    {
        public string keyword;
        public string emojiText;
        public Sprite emojiSprite;
    }

    public List<EmojiEntry> entries = new();
    public EmojiEntry fallbackEntry;

    Dictionary<string, string> lookupCache;
    string fallbackUnicode;


    public void Initialize()
    {
        lookupCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.keyword)) continue;

            if (entry.emojiSprite != null || !string.IsNullOrEmpty(entry.emojiText))
            {
                lookupCache[entry.keyword] = !string.IsNullOrEmpty(entry.emojiText)
                    ? entry.emojiText
                    : char.ConvertFromUtf32((int)(0x1F600 + entries.IndexOf(entry))); // Dynamic placeholder code point
            }
        }

        fallbackUnicode = !string.IsNullOrEmpty(fallbackEntry.emojiText)
            ? fallbackEntry.emojiText
            : char.ConvertFromUtf32(0x1F4AC);
    }

    public string TryGetEmoji(string keyword, out string unicodeCharacter)
    {
        if (lookupCache == null) Initialize();

        return lookupCache.TryGetValue(keyword, out unicodeCharacter) ?
            unicodeCharacter : fallbackUnicode;
    }

#if UNITY_EDITOR
    [NaughtyAttributes.Button]
    void BakeEmojiAsset() => EmojiAssetGenerator.BakeAsset();
#endif
}