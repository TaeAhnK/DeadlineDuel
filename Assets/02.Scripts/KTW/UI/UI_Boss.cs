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
    [SerializeField] private float minSliderValue = 0.05f; // 슬라이더는 이 아래의 값이 되면 이상하게 표시됨

    /// <summary>
    /// 플레이어1 2의 이름을 입력하여 UI 초기화
    /// </summary>
    public void InitializedUI(string player1, string player2) {
        UpdatePlayerBossHP(1.0f);
        UpdateEnemyBossHP(1.0f);
        SetPlayerName(player1, player2);
    }

    /// <summary>
    /// 플레이어 보스 HP 업데이트
    /// </summary>
    /// <param name="hpPercentage">0~1사이의 값으로 입력</param>
    public void UpdatePlayerBossHP(float hpPercentage) {
        float safeValue = Mathf.Max(hpPercentage, minSliderValue);
        playerBossHPSlider.DOValue(safeValue, 0.3f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 적 보스 HP 업데이트
    /// </summary>
    /// <param name="hpPercentage">0~1사이의 값으로 입력</param>
    public void UpdateEnemyBossHP(float hpPercentage) {
        float safeValue = Mathf.Max(hpPercentage, minSliderValue);
        enemyBossHPSlider.DOValue(safeValue, 0.3f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 좌우의 플레이어 보스 이름을 초기화
    /// </summary>
    private void SetPlayerName(string player1, string player2) {
        playerNameText.text = $"{player1}'s Boss";
        enemyNameText.text = $"{player2}'s Boss";
    }
}
