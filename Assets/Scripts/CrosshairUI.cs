using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Object.FindFirstObjectByType<CrosshairUI>() != null) return;
        GameObject root = new GameObject("Crosshair", typeof(Canvas), typeof(CrosshairUI));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
        dot.transform.SetParent(root.transform, false);
        RectTransform rect = dot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(10f, 10f);
        Image image = dot.GetComponent<Image>();
        image.sprite = CreateDotSprite();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static Sprite CreateDotSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= 7f ? Color.white : Color.clear);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
