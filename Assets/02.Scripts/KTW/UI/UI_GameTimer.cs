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
    /// �ܺο��� Ÿ�̸� ȣ���Ͽ� ����
    /// </summary>
    /// <param name="startSeconds">�� ������ �Է�. ex) 5�� * 60 -> 300 </param>
    public void StartTimer(int startSeconds) {
        remainingTime = startSeconds;
        isRunning = true;
        UpdateTimerText(remainingTime);
        StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine() {
        UpdateTimerText(remainingTime); // ���� �ʱ�ȭ
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
        Debug.Log("UI_GameTimer | TimeGameOver �ð� ����");
        StopTimer();
        // TODO
        // �ܺη� �޽��� �߻�
    }

    /// <summary>
    /// �ܺο��� ȣ���Ͽ� Ÿ�̸� ����
    /// </summary>
    public void StopTimer() {
        Debug.Log("UI_GameTimer | StopTimer Ÿ�̸� ����");
        isRunning = false;
    }

    /// <summary>
    /// ���� Ÿ�̸� �ð� ��ȯ
    /// </summary>
    public int GetRemainingTime() => remainingTime;
}
