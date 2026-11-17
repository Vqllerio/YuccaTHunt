using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    public float charsPerSecond = 45f;

    TMP_Text label;
    Coroutine routine;

    void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    void OnDisable()
    {
        if (label != null) label.maxVisibleCharacters = int.MaxValue;
    }

    IEnumerator Run()
    {
        label.ForceMeshUpdate();
        int total = label.textInfo.characterCount;
        label.maxVisibleCharacters = 0;

        float shown = 0f;
        while (shown < total)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            label.maxVisibleCharacters = Mathf.FloorToInt(shown);
            yield return null;
        }
        label.maxVisibleCharacters = int.MaxValue;
    }
}
