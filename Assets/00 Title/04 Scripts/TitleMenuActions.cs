using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class TitleMenuActions : MonoBehaviour
{
    [Header("Start Game")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject cinematicPanel;
    [SerializeField] private VideoPlayer cinematicPlayer;
    [SerializeField] private string nextSceneName = "GhostStation";

    private bool isStarting;

    private void Awake()
    {
        if (cinematicPanel != null)
            cinematicPanel.SetActive(false);

        if (cinematicPlayer != null)
            cinematicPlayer.loopPointReached += OnCinematicFinished;
    }

    private void OnDestroy()
    {
        if (cinematicPlayer != null)
            cinematicPlayer.loopPointReached -= OnCinematicFinished;
    }

    public void StartNewGame()
    {
        if (isStarting) return;
        isStarting = true;

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (cinematicPanel != null)
            cinematicPanel.SetActive(true);

        if (cinematicPlayer != null)
        {
            cinematicPlayer.Stop();
            cinematicPlayer.Play();
        }
        else
        {
            LoadNextScene();
        }
    }

    private void OnCinematicFinished(VideoPlayer source)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}