using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = 0.93f;
    public float speed = 18f;
    public bool playClick = true;

    Vector3 target = Vector3.one;

    public void OnPointerDown(PointerEventData eventData)
    {
        target = Vector3.one * pressedScale;
        if (playClick)
        {
            var sel = GetComponent<UnityEngine.UI.Selectable>();
            if (sel == null || sel.interactable) SfxPlayer.PlayClick();
        }
    }
    public void OnPointerUp(PointerEventData eventData) { target = Vector3.one; }
    public void OnPointerExit(PointerEventData eventData) { target = Vector3.one; }

    void OnDisable()
    {
        target = Vector3.one;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        if ((transform.localScale - target).sqrMagnitude < 0.00001f) return;
        float k = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, target, k);
    }
}
