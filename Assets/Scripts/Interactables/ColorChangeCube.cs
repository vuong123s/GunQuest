using UnityEngine;

public class ColorChangeCube : Interactable
{
    private MeshRenderer meshRenderer;
    [SerializeField]
    private Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };
    private int colorIndex = 0;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    protected override void Interact()
    {
        if (meshRenderer != null && colors.Length > 0)
        {
            colorIndex = (colorIndex + 1) % colors.Length;
            meshRenderer.material.color = colors[colorIndex];
        }
    }
}
