using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsPanelScript : MonoBehaviour
{
    public GameObject settingsPanel;
    public Button gearButton;
    public Button resetButton;
    public Button muteButton;
    public TextMeshProUGUI muteButtonLabel;

    private bool isMuted = false;

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (gearButton != null) gearButton.onClick.AddListener(TogglePanel);
        if (resetButton != null) resetButton.onClick.AddListener(ResetQuest);
        if (muteButton != null) muteButton.onClick.AddListener(ToggleMute);
    }

    void TogglePanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    void ResetQuest()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.pause = isMuted;
        if (muteButtonLabel != null)
            muteButtonLabel.text = isMuted ? "Unmute" : "Mute";
    }
}