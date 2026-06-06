using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    private const string MasterVolumeKey = "MasterVolume";
    private const string BGMVolumeKey = "BGMVolume";
    private const string MouseSensitivityKey = "MouseSensitivity";
    private const string FullscreenKey = "Fullscreen";

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Defaults")]
    [SerializeField] private float defaultMasterVolume = 0.8f;
    [SerializeField] private float defaultBGMVolume = 0.7f;
    [SerializeField] private float defaultMouseSensitivity = 1.0f;

    private void Awake()
    {
        LoadSettings();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat(BGMVolumeKey, value);

        // 실제 BGM AudioSource가 생기면 여기에 연결
        // bgmAudioSource.volume = value;
    }

    public void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(MouseSensitivityKey, value);

        // 플레이어 카메라 스크립트에서 이 값을 읽어서 사용하면 됨
        // PlayerPrefs.GetFloat("MouseSensitivity", 1.0f)
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        float bgm = PlayerPrefs.GetFloat(BGMVolumeKey, defaultBGMVolume);
        float sensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        AudioListener.volume = master;
        Screen.fullScreen = fullscreen;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(master);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(bgm);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.SetValueWithoutNotify(sensitivity);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
    }
}