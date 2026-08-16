using System;
using UnityEngine;

public partial class ColoredFireSource
{
    [Serializable]
    public struct FireSystemData
    {
        public ParticleSystem system;
        public Gradient lifetimeGradient;

        GradientColorKey[] initialKeys;
        GradientColorKey[] workingKeys;
        int[] keyToPaletteIndexMap;
        Gradient gradient;

        public void Initialize(Color[] originalPalette)
        {
            if (system == null || lifetimeGradient == null)
                return;

            initialKeys = lifetimeGradient.colorKeys;
            keyToPaletteIndexMap = new int[initialKeys.Length];
            workingKeys = new GradientColorKey[initialKeys.Length];

            for (int i = 0; i < initialKeys.Length; i++)
            {
                var key = initialKeys[i];
                workingKeys[i] = new GradientColorKey(key.color, key.time);
                keyToPaletteIndexMap[i] = Array.FindIndex(originalPalette, c => c.Compare(key.color));
            }

            var module = system.colorOverLifetime;
            gradient = module.color.gradient;
        }

        public void Apply(AnimatedFirePalette animatedPalette)
        {
            if (system == null || initialKeys == null)
                return;

            var changed = false;

            for (int i = 0; i < initialKeys.Length; i++)
            {
                var key = initialKeys[i];
                var index = keyToPaletteIndexMap[i];
                if (index >= 0)
                {
                    var newAnimatedColor = animatedPalette.GetColor(index);
                    changed |= !key.color.Compare(newAnimatedColor);
                    workingKeys[i].color = newAnimatedColor;
                }
            }

            if (changed)
            {
                gradient.SetColorKeys(workingKeys);
            }
        }
    }
}
