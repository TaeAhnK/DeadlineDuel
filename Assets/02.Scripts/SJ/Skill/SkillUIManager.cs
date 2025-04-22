using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUIManager : MonoBehaviour
{
    [System.Serializable]
    public class SkillUI
    {
        public Image skillIcon;
        public Image cooldownOverlay;
        public TextMeshProUGUI cooldownText;
        public KeyCode keyCode;
    }
    
    [Header("스킬 UI 참조")]
    [SerializeField] private SkillUI[] _skillUIs = new SkillUI[4]; // Q, W, E, R에 해당하는 UI
    
    // 플레이어 컨트롤러 참조
    private PlayerController _playerController;
    
    // 스킬 쿨다운 정보
    private float[] _skillCooldowns = new float[4];
    private float[] _skillMaxCooldowns = new float[4];
    
    private void Start()
    {
        // 플레이어 찾기
        FindLocalPlayer();
        
        // 스킬 정보 초기화
        InitializeSkillInfo();
    }
    
    private void FindLocalPlayer()
    {
        // 로컬 플레이어 찾기
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                _playerController = player;
                break;
            }
        }
    }
    
    private void InitializeSkillInfo()
    {
        if (_playerController == null) return;
        
        // 플레이어 컨트롤러에서 스킬 정보 가져오기
        // 이 부분은 PlayerController에 GetSkillInfo 같은 메서드를 추가해야 동작합니다
        // 여기서는 기본값 사용
        
        for (int i = 0; i < _skillMaxCooldowns.Length; i++)
        {
            _skillMaxCooldowns[i] = 10f; // 기본값, 실제로는 플레이어 컨트롤러에서 가져와야 함
        }
        
        // UI 초기화
        for (int i = 0; i < _skillUIs.Length; i++)
        {
            if (_skillUIs[i] != null && _skillUIs[i].cooldownOverlay != null)
            {
                _skillUIs[i].cooldownOverlay.fillAmount = 0;
                
                if (_skillUIs[i].cooldownText != null)
                {
                    _skillUIs[i].cooldownText.text = "";
                }
            }
        }
    }
    
    private void Update()
    {
        // 쿨다운 정보 업데이트
        UpdateCooldownInfo();
        
        // UI 업데이트
        UpdateSkillUI();
        
        // 키 입력 하이라이트
        UpdateKeyHighlight();
    }
    
    private void UpdateCooldownInfo()
    {
        if (_playerController == null) return;
        
        // 플레이어 컨트롤러에서 쿨다운 정보 가져오기
        // 이 부분은 PlayerController에 GetSkillCooldowns 같은 메서드를 추가해야 동작합니다
        // 여기서는 테스트용 더미 데이터 사용
        
        for (int i = 0; i < _skillCooldowns.Length; i++)
        {
            // 테스트용: 키를 누르면 쿨다운 시작
            if (Input.GetKeyDown(_skillUIs[i].keyCode))
            {
                _skillCooldowns[i] = _skillMaxCooldowns[i];
            }
            
            // 쿨다운 감소
            if (_skillCooldowns[i] > 0)
            {
                _skillCooldowns[i] -= Time.deltaTime;
                if (_skillCooldowns[i] < 0) _skillCooldowns[i] = 0;
            }
        }
    }
    
    private void UpdateSkillUI()
    {
        for (int i = 0; i < _skillUIs.Length; i++)
        {
            if (_skillUIs[i] == null) continue;
            
            // 쿨다운 오버레이 업데이트
            if (_skillUIs[i].cooldownOverlay != null)
            {
                if (_skillCooldowns[i] > 0)
                {
                    float fillAmount = _skillCooldowns[i] / _skillMaxCooldowns[i];
                    _skillUIs[i].cooldownOverlay.fillAmount = fillAmount;
                }
                else
                {
                    _skillUIs[i].cooldownOverlay.fillAmount = 0;
                }
            }
            
            // 쿨다운 텍스트 업데이트
            if (_skillUIs[i].cooldownText != null)
            {
                if (_skillCooldowns[i] > 0)
                {
                    _skillUIs[i].cooldownText.text = Mathf.Ceil(_skillCooldowns[i]).ToString();
                }
                else
                {
                    _skillUIs[i].cooldownText.text = "";
                }
            }
        }
    }
    
    private void UpdateKeyHighlight()
    {
        for (int i = 0; i < _skillUIs.Length; i++)
        {
            if (_skillUIs[i] == null || _skillUIs[i].skillIcon == null) continue;
            
            // 키를 누르고 있을 때 하이라이트 효과
            if (Input.GetKey(_skillUIs[i].keyCode) && _skillCooldowns[i] <= 0)
            {
                _skillUIs[i].skillIcon.color = Color.yellow;
            }
            else
            {
                // 쿨다운 중이면 회색, 아니면 흰색
                _skillUIs[i].skillIcon.color = _skillCooldowns[i] > 0 ? Color.gray : Color.white;
            }
        }
    }
    
    // 특정 스킬의 쿨다운 업데이트 (PlayerController에서 호출)
    public void UpdateSkillCooldown(int index, float currentCooldown, float maxCooldown)
    {
        if (index < 0 || index >= _skillCooldowns.Length) return;
        
        _skillCooldowns[index] = currentCooldown;
        _skillMaxCooldowns[index] = maxCooldown;
    }
}