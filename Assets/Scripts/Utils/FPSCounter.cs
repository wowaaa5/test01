using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    static FPSCounter instance;

    [Header("Settings")]
    [SerializeField] float updateInterval = 1 / 3f;

    TextMeshProUGUI fpsText;
    int frameCount = 0;
    float timeAccumulator = 0.0f;
    int lastRenderedFps = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeOnPlay()
    {
        if (instance != null) return;

        // 1. Create Root Object
        GameObject go = new GameObject("Runtime_FPS_Counter");
        instance = go.AddComponent<FPSCounter>();
        DontDestroyOnLoad(go);

        // 2. Programmatically generate a lightweight mobile Canvas
        GameObject canvasGo = new GameObject("FPS_Canvas");
        canvasGo.transform.SetParent(go.transform);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Keep on top of everything

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>(); // Required but cheap

        // 3. Create the Text Mesh Pro component
        //add background panel for better visibility
        GameObject panelGo = new GameObject("FPS_Panel");
        panelGo.transform.SetParent(canvasGo.transform);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // semi-transparent black
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(20, -20);
        panelRect.sizeDelta = new Vector2(200, 80);

        GameObject textGo = new GameObject("FPS_Text");
        textGo.transform.SetParent(panelGo.transform);

        instance.fpsText = textGo.AddComponent<TextMeshProUGUI>();
        instance.fpsText.color = Color.green;
        instance.fpsText.fontSize = 36;
        instance.fpsText.alignment = TextAlignmentOptions.TopLeft;

        // Position in the top left corner with safe padding
        RectTransform rect = instance.fpsText.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(400, 100);
    }

    void Update()
    {
        frameCount++;
        timeAccumulator += Time.unscaledDeltaTime;

        // Only process mathematics when the interval window closes
        if (timeAccumulator >= updateInterval)
        {
            int currentFps = Mathf.RoundToInt(frameCount / timeAccumulator);

            // OPTIMIZATION: Only touch the UI component if the value actually changed
            if (currentFps != lastRenderedFps)
            {
                lastRenderedFps = currentFps;

                // TMP Golden Rule: Passing an int directly to SetText 
                // performs an inline integer-to-char conversion with 0 GC allocations.
                fpsText.SetText("FPS: {0}", currentFps);
            }

            // Reset sampling window fields
            frameCount = 0;
            timeAccumulator = 0.0f;
        }
    }
}