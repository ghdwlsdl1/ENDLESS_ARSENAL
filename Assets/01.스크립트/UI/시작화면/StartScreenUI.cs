using UnityEngine;

public class StartScreenUI : MonoBehaviour
{
    [InspectorLabel("UI 관리자")]
    [SerializeField] private UIManager uiManager;

    public void StartButton()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(
                SfxType.Button,
                0);
        }

        if (uiManager != null)
        {
            uiManager.ShowPopup(
                UIPanelType.CharacterScreen);
        }
    }

    public void QuitButton()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(
                SfxType.Button,
                0);
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying =
            false;
#endif
    }
}