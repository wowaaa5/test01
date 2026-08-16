using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class ColoredFireSource : MonoBehaviour
{
    static readonly int NextColorHash = Animator.StringToHash("NextColor");

    [SerializeField] FireSystemData[] fireSystems;
    [SerializeField] Color[] originalPalette;
    [SerializeField] AnimatedFirePalette animatedPalette;

    [SerializeField] ButtonUI nextColorButton;
    [SerializeField] Image colorIndicator;
    [SerializeField] Animator animator;

    void Awake()
    {
        for (int i = 0; i < fireSystems.Length; i++)
        {
            if (fireSystems[i].system == null)
                continue;
            fireSystems[i].Initialize(originalPalette);
        }
    }

    void OnEnable()
    {
        if (nextColorButton != null)
        {
            nextColorButton.SetOnCLickListener(HandleNextColorClick, lockInteractable: false);
        }
    }

    void HandleNextColorClick() => animator.SetTrigger(NextColorHash);

    void OnValidate()
    {
        var systems = GetComponentsInChildren<ParticleSystem>();
        if (systems == null || systems.Length == 0)
            return;

        fireSystems = systems.Where(s => s.emission.enabled).Select(s =>
            new FireSystemData
            {
                system = s,
                lifetimeGradient = s.colorOverLifetime.color.gradient
            }
        ).ToArray();

        originalPalette = fireSystems.SelectMany(fs => fs.lifetimeGradient.colorKeys).Select(k => ColorUtility.ToHtmlStringRGB(k.color)).Distinct().Select(hex => ColorUtility.TryParseHtmlString($"#{hex}", out var c) ? c : Color.white).OrderBy(c => c.grayscale).ToArray();
        if (animatedPalette == null)
        {
            animatedPalette = GetComponent<AnimatedFirePalette>();
            if (animatedPalette != null)
                animatedPalette.Assign(originalPalette);
        }
    }

    void LateUpdate()
    {
        for (int i = 0; i < fireSystems.Length; i++)
        {
            fireSystems[i].Apply(animatedPalette);
            colorIndicator.color = animatedPalette.glow;
        }
    }
}
