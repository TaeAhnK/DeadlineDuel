using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Boss : MonoBehaviour
{
    [Header("UI Assign")]
    [SerializeField] private Slider playerBossHPSlider;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Slider enemyBossHPSlider;
    [SerializeField] private TextMeshProUGUI enemyNameText;

    [Header("Setting")]
    [SerializeField] private float minSliderValue = 0.05f; // �����̴��� �� �Ʒ��� ���� �Ǹ� �̻��ϰ� ǥ�õ�

    /// <summary>
    /// �÷��̾�1 2�� �̸��� �Է��Ͽ� UI �ʱ�ȭ
    /// </summary>
    public void InitializedUI(string player1, string player2) {
        UpdatePlayerBossHP(1.0f);
        UpdateEnemyBossHP(1.0f);
        SetPlayerName(player1, player2);
    }

    /// <summary>
    /// �÷��̾� ���� HP ������Ʈ
    /// </summary>
    /// <param name="hpPercentage">0~1������ ������ �Է�</param>
    public void UpdatePlayerBossHP(float hpPercentage) {
        float safeValue = Mathf.Max(hpPercentage, minSliderValue);
        playerBossHPSlider.DOValue(safeValue, 0.3f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// �� ���� HP ������Ʈ
    /// </summary>
    /// <param name="hpPercentage">0~1������ ������ �Է�</param>
    public void UpdateEnemyBossHP(float hpPercentage) {
        float safeValue = Mathf.Max(hpPercentage, minSliderValue);
        enemyBossHPSlider.DOValue(safeValue, 0.3f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// �¿��� �÷��̾� ���� �̸��� �ʱ�ȭ
    /// </summary>
    private void SetPlayerName(string player1, string player2) {
        playerNameText.text = $"{player1}'s Boss";
        enemyNameText.text = $"{player2}'s Boss";
    }
}
