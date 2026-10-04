using UnityEngine;

public class ColorChangeCube : Interactable
{
    [SerializeField] private Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow };
    private Renderer targetRenderer;
    private int colorIndex;

    private void Awake() => targetRenderer = GetComponent<Renderer>();

    protected override void Interact()
    {
        if (targetRenderer == null || colors == null || colors.Length == 0) return;
        targetRenderer.material.color = colors[colorIndex++ % colors.Length];
    }
}
