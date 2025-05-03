using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_PlayerUI : MonoBehaviour
{
    [Header("UI Assign")]
    [SerializeField] private Slider playerHPSlider;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI aspText;
    [SerializeField] private TextMeshProUGUI coolText;
    [SerializeField] private Button[] skillButton;
    [SerializeField] private TextMeshProUGUI[] skillCoolTexts;
    [SerializeField] private Image[] skillButtonImages;
    [SerializeField] private Button healItemButton;
    [SerializeField] private TextMeshProUGUI itemAmountText;


    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TextMeshProUGUI descriptionNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Coroutine[] cooldownCoroutines = new Coroutine[4];
    private int coolTimeReduction = 0;

    private const float UIHPBarAnimationDuration = 0.3f;

    [Header("Skill Data")]
    [SerializeField] public string characterName = "Cosmo"; // ���� ĳ���ʹ� Cosmo�ۿ� ����

    [SerializeField] private CharacterSkillData skillData;

    private void Start() {
        InitializedButton();
        InitializedMouseOverOutEvent();

        skillData = SkillDataLoader.Instance.LoadSkillData(characterName);

        // TODO TEST
        UpdateStatTexts(100, 100, 1, -2);
        StartSkillCooldown(0, 5);
        StartSkillCooldown(1, 10);
        StartSkillCooldown(2, 15);
        StartSkillCooldown(3, 20);
    }

    private void InitializedButton() {
        healItemButton.onClick.AddListener(() => OnClickHealItemButton());
        skillButton[0].onClick.AddListener(() => OnClickQButton());
        skillButton[1].onClick.AddListener(() => OnClickWButton());
        skillButton[2].onClick.AddListener(() => OnClickEButton());
        skillButton[3].onClick.AddListener(() => OnClickRButton());
    }

    private void InitializedMouseOverOutEvent() {
        // QWER ��ư�� ���콺 ���� / �ƿ� �̺�Ʈ �߰�
        for (int i = 0; i < skillButton.Length; i++) {
            int idx = i; // Ŭ���� ĸó
            EventTrigger trigger = skillButton[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = skillButton[i].gameObject.AddComponent<EventTrigger>();

            // PointerEnter
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { ShowSkillDescription(idx); });
            trigger.triggers.Add(entryEnter);

            // PointerExit
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { HideSkillDescription(); });
            trigger.triggers.Add(entryExit);
        }

        descriptionPanel.SetActive(false);
    }

    private void Update() {
        // TODO TEST
        if (Input.GetKeyDown(KeyCode.A)) {
            UpdatePlayerHP(0.0f);
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            UpdatePlayerHP(0.3f);
        }

        if (Input.GetKeyDown(KeyCode.D)) {
            UpdatePlayerHP(0.8f);
        }

        if (Input.GetKeyDown(KeyCode.F)) {
            UpdatePlayerHP(1.0f);
        }
    }

    /// <summary>
    /// UI �÷��̾� ü�� ����
    /// </summary>
    /// <param name="hpValue">0~1������ ��</param>
    public void UpdatePlayerHP(float hpValue) {
        playerHPSlider.DOValue(hpValue, UIHPBarAnimationDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// ȸ�� ������ ��� ���� Ƚ��
    /// </summary>
    /// <param name="count"></param>
    public void UpdateItemAmount(int count) {
        itemAmountText.text = count.ToString();
    }


    // TODO Player�� ��ư�� ����
    public void OnClickQButton() {
        Debug.Log("Pressd Q");
    }

    public void OnClickWButton() {
        Debug.Log("Pressd W");
    }

    public void OnClickEButton() {
        Debug.Log("Pressd E");
    }

    public void OnClickRButton() {
        Debug.Log("Pressd R");
    }

    public void OnSkillButtonPressed(int skillIndex) {
        // TODO �÷��̾� �� ���� ���� ���� �Ʒ��ŷ� ����.
    }


    public void OnClickHealItemButton() {
        Debug.Log("UI_PlayerUI | OnClickHealItemButton");
    }

    /// <summary>
    /// ���ݷ� ���� ���ݼӵ� ��Ÿ�ӹ���� float ���·� �Է¹ް� ������Ʈ
    /// </summary>
    public void UpdateStatTexts(float atk, float def, float asp, int cool) {
        string sign = cool >= 0 ? "+": (cool < 0 ? "-" : "");   // + (����) - �Ǻ�

        atkText.text = $"ATK : {atk}";
        defText.text = $"DEF : {def}";
        aspText.text = $"ASP : {asp:0.00}";
        coolText.text = $"Cool {sign}{Mathf.Abs(cool)}"; // +- 1�� 2�� �̷���
        coolTimeReduction = cool;
    }

    /// <summary>
    /// �ܺο��� ��ų ��Ÿ���� ������ �� ȣ��
    /// skillIndex: 0=Q, 1=W, 2=E, 3=R
    /// cooldownSeconds: ��Ÿ��(��)
    /// </summary>
    public void StartSkillCooldown(int skillIndex, int cooldownSeconds) {
        if (!IsValidSkillIndex(skillIndex)) return;

        // ���� ��Ÿ�� �ڷ�ƾ üũ
        if (cooldownCoroutines[skillIndex] != null) {
            StopCoroutine(cooldownCoroutines[skillIndex]);
        }
        cooldownCoroutines[skillIndex] = StartCoroutine(SkillCooldownCoroutine(skillIndex, cooldownSeconds));
    }

    private bool IsValidSkillIndex(int index) {
        return index >= 0 && index < skillButton.Length;

    }

    private IEnumerator SkillCooldownCoroutine(int skillIndex, int cooldownSeconds) {
        TextMeshProUGUI targetText = skillCoolTexts[skillIndex];
        Image targetImage = skillButtonImages[skillIndex];
        Color originalColor= targetImage.color;
        Color fadedColor = targetImage.color; fadedColor.a = 0.5f;

        targetImage.color = fadedColor;


        float remainingTime = Mathf.Max(cooldownSeconds - coolTimeReduction, 0);    // ��Ÿ�� ������ ���
        int displayedTime = Mathf.CeilToInt(remainingTime);

        targetText.text = displayedTime.ToString();

        while (remainingTime > 0f) {
            remainingTime -= Time.deltaTime;
            displayedTime = Mathf.CeilToInt(remainingTime);

            targetText.text = displayedTime.ToString();

            yield return null;
        }


        targetText.text = "";
        targetImage.color = originalColor;

        cooldownCoroutines[skillIndex] = null;
    }

    private void ShowSkillDescription(int idx) {
        if (descriptionPanel == null || descriptionNameText == null || descriptionText == null) return;

        descriptionPanel.SetActive(true);
        descriptionNameText.text = skillData.skillNames[idx];
        descriptionText.text = skillData.skillDescriptions[idx];
    }

    private void HideSkillDescription() {
        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);
    }
}
