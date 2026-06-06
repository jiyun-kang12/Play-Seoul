using UnityEngine;
using UnityEngine.EventSystems;

public class TitleMenuHoverItem : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private TitleMenuSelector selector;
    [SerializeField] private int menuIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        selector.Select(menuIndex);
    }
}