using TMPro;
using UnityEngine;

public class TitleMenuSelector : MonoBehaviour
{
    [System.Serializable]
    public class MenuItem
    {
        public RectTransform target;
        public GameObject grayDot;
        public TMP_Text labelText;
        public GameObject clickedEffect;
    }

    [Header("Cursor")]
    [SerializeField] private RectTransform redCursor;
    [SerializeField] private float moveSpeed = 18f;

    [Header("Colors")]
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color normalTextColor = new Color32(0x88, 0x88, 0x88, 0xFF);

    [Header("Menu Items")]
    [SerializeField] private MenuItem[] menuItems;
    [SerializeField] private int defaultIndex = 0;

    private RectTransform currentTarget;
    private int currentIndex = -1;

    private void Start()
    {
        Select(defaultIndex, true);
    }

    private void Update()
    {
        if (redCursor == null || currentTarget == null) return;

        redCursor.anchoredPosition = Vector2.Lerp(
            redCursor.anchoredPosition,
            currentTarget.anchoredPosition,
            Time.unscaledDeltaTime * moveSpeed
        );
    }

    public void Select(int index)
    {
        Select(index, false);
    }

    private void Select(int index, bool instant)
    {
        if (index < 0 || index >= menuItems.Length) return;

        currentIndex = index;
        currentTarget = menuItems[index].target;

        for (int i = 0; i < menuItems.Length; i++)
        {
            bool selected = i == currentIndex;

            if (menuItems[i].grayDot != null)
                menuItems[i].grayDot.SetActive(!selected);

            if (menuItems[i].labelText != null)
                menuItems[i].labelText.color = selected ? selectedTextColor : normalTextColor;

            if (menuItems[i].clickedEffect != null)
                menuItems[i].clickedEffect.SetActive(selected);
        }

        if (instant && redCursor != null && currentTarget != null)
            redCursor.anchoredPosition = currentTarget.anchoredPosition;
    }
}