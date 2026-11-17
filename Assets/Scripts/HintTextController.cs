using UnityEngine;
using TMPro;

public class HintTextController : MonoBehaviour
{
    public TextMeshProUGUI hintText;
    public EntranceTrigger entranceTrigger;

    void Start()
    {
        if (hintText == null) return;
        hintText.text = "Point at the entrance poster to meet Yucca.";

        if (entranceTrigger == null)
            entranceTrigger = FindObjectOfType<EntranceTrigger>();

        if (entranceTrigger != null)
            entranceTrigger.OnIntroClosed += OnIntroClosed;
    }

    void OnDestroy()
    {
        if (entranceTrigger != null)
            entranceTrigger.OnIntroClosed -= OnIntroClosed;
    }

    void OnIntroClosed()
    {
        if (hintText != null)
            hintText.text = "Find and scan the memories across the lounge.";
    }
}