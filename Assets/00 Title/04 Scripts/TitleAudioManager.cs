using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleAudioManager : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip titleBgm;

    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.18f;

    [Header("UI SFX Source")]
    [SerializeField] private AudioSource uiSfxSource;

    [Header("Hover Sound")]
    [SerializeField] private AudioClip hoverClip;

    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.45f;

    [Header("Click Sound")]
    [SerializeField] private AudioClip clickClip;

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1.0f;

    [Header("Buttons")]
    [SerializeField] private Button[] buttons;

    private void Awake()
    {
        SetupBgm();
        SetupButtons();
    }

    private void SetupBgm()
    {
        if (bgmSource == null || titleBgm == null)
            return;

        bgmSource.clip = titleBgm;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    private void SetupButtons()
    {
        if (buttons == null)
            return;

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            EventTrigger trigger = button.GetComponent<EventTrigger>();

            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            AddEvent(trigger, EventTriggerType.PointerEnter, PlayHover);
            AddEvent(trigger, EventTriggerType.PointerClick, PlayClick);
        }
    }

    private void AddEvent(
        EventTrigger trigger,
        EventTriggerType eventType,
        UnityEngine.Events.UnityAction action
    )
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(_ => action.Invoke());
        trigger.triggers.Add(entry);
    }

    public void PlayHover()
    {
        if (uiSfxSource == null || hoverClip == null)
            return;

        uiSfxSource.spatialBlend = 0f;
        uiSfxSource.PlayOneShot(hoverClip, hoverVolume);
    }

    public void PlayClick()
    {
        if (uiSfxSource == null || clickClip == null)
            return;

        uiSfxSource.spatialBlend = 0f;
        uiSfxSource.PlayOneShot(clickClip, clickVolume);
    }
}