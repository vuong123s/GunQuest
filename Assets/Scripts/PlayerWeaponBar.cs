using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWeaponBar : MonoBehaviour
{
    [SerializeField] private PlayerWeapons weapons;
    [SerializeField] private Image[] backgrounds;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private Color normalColor = new Color(0.08f, 0.1f, 0.12f, 0.75f);
    [SerializeField] private Color selectedColor = new Color(0.15f, 0.5f, 0.43f, 0.95f);

    private void OnEnable()
    {
        if (weapons == null) return;
        weapons.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (weapons != null) weapons.Changed -= Refresh;
    }

    private void Refresh()
    {
        for (int i = 0; i < backgrounds.Length; i++)
            if (backgrounds[i] != null) backgrounds[i].color = i == weapons.SelectedIndex ? selectedColor : normalColor;
        for (int i = 0; i < labels.Length; i++)
            if (labels[i] != null) labels[i].text = (i + 1) + "  " + weapons.GetLabel(i);
    }
}
