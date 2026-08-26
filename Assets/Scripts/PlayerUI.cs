using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI promptTextTMP;

    [SerializeField]
    private Text promptTextLegacy;

    void Awake()
    {
        if (promptTextTMP == null)
        {
            promptTextTMP = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (promptTextTMP == null && promptTextLegacy == null)
        {
            promptTextLegacy = GetComponentInChildren<Text>();
        }
    }

    public void UpdateText(string promptMessage)
    {
        if (promptTextTMP != null)
        {
            promptTextTMP.text = promptMessage;
        }
        else if (promptTextLegacy != null)
        {
            promptTextLegacy.text = promptMessage;
        }
    }
}
