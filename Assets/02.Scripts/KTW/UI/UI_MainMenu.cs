using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_MainMenu : MonoBehaviour 
{
    [Header("UI Assign")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI loadingText;

    private void Start() {
        startButton.onClick.AddListener(() => OnClickGameStartButton());
        settingButton.onClick.AddListener(() => OnClickSettingButton());
        quitButton.onClick.AddListener(() => OnClickQuitButton());
        SetLoadingProgress(0);
    }


    private void OnClickGameStartButton() {
        Debug.Log("UI_MainMenu | OnClickGameStartButton");
        GameMainManager.Instance.OnStartGameClicked();

    }

    private void OnClickSettingButton() {
        Debug.Log("설정 버튼 클릭");
    }

    private void OnClickQuitButton()
    {
        // 이전 세션 데이터 정리
        GameMainManager.ClearPreviousSessionData();

        // 로그아웃 처리
        GameInitManager initManager = GameInitManager.GetInstance();
        if (initManager != null)
        {
            initManager.LogoutPlayer();
        }
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    public void SetLoadingProgress(float value) {
        if (loadingSlider == null || loadingText == null) {
            Debug.LogError("UI_MainMenu | SetLoadingProgress | UI is not assigned");
        }
        value = Mathf.Clamp01(value);
        loadingSlider.value = value;
        loadingText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}
