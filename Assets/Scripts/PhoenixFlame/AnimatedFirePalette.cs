using System;
using UnityEngine;

[Serializable]
public class AnimatedFirePalette : MonoBehaviour
{
    public Color coreHot;
    public Color coreMain;
    public Color glow;
    public Color flame;
    public Color flameEdge;

    public void Assign(Color[] colors)
    {
        if (colors.Length > 0) flameEdge = colors[0];
        if (colors.Length > 1) flame = colors[1];
        if (colors.Length > 2) glow = colors[2];
        if (colors.Length > 3) coreMain = colors[3];
        if (colors.Length > 4) coreHot = colors[4];
    }

    public Color GetColor(int index) => index switch
    {
        0 => flameEdge,
        1 => flame,
        2 => glow,
        3 => coreMain,
        4 => coreHot,
        _ => Color.black
    };
}