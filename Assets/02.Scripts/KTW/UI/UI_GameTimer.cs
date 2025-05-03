using System.Collections;
using TMPro;
using UnityEngine;

public class UI_GameTimer : MonoBehaviour
{
    [Header("UI Assign")]
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private float timerValueTest = 0.0f;

    private int remainingTime = 0;
    private bool isRunning = false;

    /// <summary>
    /// 외부에서 타이머 호출하여 시작
    /// </summary>
    /// <param name="startSeconds">초 단위로 입력. ex) 5분 * 60 -> 300 </param>
    public void StartTimer(int startSeconds) {
        remainingTime = startSeconds;
        isRunning = true;
        UpdateTimerText(remainingTime);
        StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine() {
        UpdateTimerText(remainingTime); // 최초 초기화
        while (isRunning && remainingTime > 0f) {
            yield return new WaitForSeconds(1.0f);
            remainingTime -= 1;
            UpdateTimerText(remainingTime);
            if (remainingTime < 0f) remainingTime = 0;
        }

        if (remainingTime <= 0) {
            TimeGameOver();
        }
    }

    private void UpdateTimerText(float time) {
        int minute = Mathf.FloorToInt(time / 60f);
        int second = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minute:00}:{second:00}";
    }

    private void TimeGameOver() {
        Debug.Log("UI_GameTimer | TimeGameOver 시간 오버");
        StopTimer();
        // TODO
        // 외부로 메시지 발사
    }

    /// <summary>
    /// 외부에서 호출하여 타이머 중지
    /// </summary>
    public void StopTimer() {
        Debug.Log("UI_GameTimer | StopTimer 타이머 중지");
        isRunning = false;
    }

    /// <summary>
    /// 남은 타이머 시간 반환
    /// </summary>
    public int GetRemainingTime() => remainingTime;
}
