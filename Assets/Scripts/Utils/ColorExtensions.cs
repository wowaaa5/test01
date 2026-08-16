using UnityEngine;

public static class ColorExtensions
{
    public static bool Compare(this Color c, Color other, float tolerance = 0.01f)
    {
        return Mathf.Abs(c.r - other.r) < tolerance &&
               Mathf.Abs(c.g - other.g) < tolerance &&
               Mathf.Abs(c.b - other.b) < tolerance &&
               Mathf.Abs(c.a - other.a) < tolerance;
    }
}
