using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterSelect : MonoBehaviour {
    [Header("UI Assign")]
    [SerializeField] private List<Button> characterButtons;
    [SerializeField] private Image selectedCharacterBorderImage;
    [SerializeField] private Button matchmakingButton;
    [SerializeField] private Button cancleMatchmakingButton;
    [SerializeField] private Button backToMainButton;
    [SerializeField] private TextMeshProUGUI matchingText;
    // [SerializeField] private GameObject matchingSpinnerObject;

    [Header("Matchmaking")]
    [SerializeField] private int selectedCharacterIndex = -1;
    [SerializeField] private bool isMatchmaking = false;
    private float matchmakingElapsed = 0f;
    private Coroutine matchmakingTimerCoroutine;

    [Header("Sound")]
    [SerializeField] private AudioClip characterSelectSound;
    [SerializeField] private AudioClip matchmakingSound;    // TODO 사운드 추가
    private AudioSource audioSource;
    
    public List<Button> GetCharacterButtons { get;  set; }
    public TextMeshProUGUI MatchingText => matchingText;

    private void Start() {
        InitalizedButtonsAndImages();
        InitState();
        UpdateMatchmakingUI(false);
        // audioSource = GetComponent<AudioSource>();
    }

    private void InitalizedButtonsAndImages() {
        matchmakingButton.onClick.AddListener(() => OnClickMatchmakingtButton());
        cancleMatchmakingButton.onClick.AddListener(() => OnClickCanclematchMakingtButton());
        backToMainButton.onClick.AddListener(() => OnClickBackToMainButton());

        for (int i = 0; i < characterButtons.Count; i++) {
            int index = i; // Value Capture
            characterButtons[index].onClick.AddListener(() => OnCharacterSelected(index));
            characterButtons[index].GetComponent<Image>().sprite = GameMainManager.Instance.GetCharacterPortraitImage(0);
            // TODO 캐릭터 포트레잇 전부 0으로 되어있음
        }
    }

    public void InitState() {
        selectedCharacterIndex = -1;
        selectedCharacterBorderImage.gameObject.SetActive(false);
        matchmakingButton.interactable = false;
        matchingText.text = "Select a character to start matching";
    }

    private void UpdateMatchmakingUI(bool isMatching) {
        matchmakingButton.gameObject.SetActive(!isMatching);
        cancleMatchmakingButton.gameObject.SetActive(isMatching);
        matchingText.gameObject.SetActive(isMatching);
        // matchingSpinnerObject.gameObject.SetActive(isMatching);
    }

    private void OnClickMatchmakingtButton() {
        Debug.Log("Ui_CharacterSelect | OnClickMatchmakingtButton");
        if (selectedCharacterIndex == -1) return;

        UpdateMatchmakingUI(true);
        isMatchmaking = true;

        // PlayMatchmakingSound(matchmakingSound);
        GameMainManager.Instance.OnStartMatchmakingClicked();

    }

    private void OnClickCanclematchMakingtButton() {
        Debug.Log("매칭 취소 버튼 클릭");
        UpdateMatchmakingUI(false);
        isMatchmaking = false;
        // TODO 매칭 취소
    }

    private void OnClickBackToMainButton() {
        Debug.Log("Ui_CharacterSelect | OnClickBackToMainButton");
        GameMainManager.Instance.OnBackToMainClicked(); // 다른 UI 접근은 매니저에서 처리
        matchmakingButton.interactable = false;
    }

    private void OnCharacterSelected(int index) {
        if (isMatchmaking) return;

        Debug.Log($"캐릭터 {index + 1} 선택");

        selectedCharacterIndex = index;

        // 빨간 테두리 옮기기 (선택 효과)
        if (selectedCharacterBorderImage != null) {
            selectedCharacterBorderImage.gameObject.SetActive(true);
            Vector3 buttonPosition = characterButtons[index].GetComponent<RectTransform>().position;
            selectedCharacterBorderImage.rectTransform.position = buttonPosition;
        }

        matchmakingButton.interactable = true;
        GameMainManager.Instance.SetSelectCharacterIndex(index);
    }

    // 외부에서 값 가져갈 때?
    public int GetSelectedCharacter() {
        return selectedCharacterIndex;
    }

    private void PlayMatchmakingSound(AudioClip clip) {
        if (audioSource == null) return;
        if (clip == null) return;

        audioSource.PlayOneShot(clip);
    }
}
