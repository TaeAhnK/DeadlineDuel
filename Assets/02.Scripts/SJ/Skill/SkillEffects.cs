using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 검 스킬 이펙트 관리 클래스 (프리팹 생성용)
/// </summary>
public class SkillEffects : MonoBehaviour
{
    [Header("Q 스킬 - 강한 베기 이펙트")]
    [SerializeField] private ParticleSystem _slashParticle;
    [SerializeField] private TrailRenderer _slashTrail;
    [SerializeField] private float _slashDuration = 0.5f;
    [SerializeField] private AudioClip _slashSound;
    
    [Header("W 스킬 - 반격기 이펙트")]
    [SerializeField] private ParticleSystem _counterParticle;
    [SerializeField] private GameObject _counterShield;
    [SerializeField] private AudioClip _counterReadySound;
    [SerializeField] private AudioClip _counterSuccessSound;
    
    [Header("E 스킬 - 이동기 이펙트")]
    [SerializeField] private ParticleSystem _dashParticle;
    [SerializeField] private TrailRenderer _dashTrail;
    [SerializeField] private GameObject _dashAfterimage;
    [SerializeField] private AudioClip _dashSound;
    
    [Header("R 스킬 - 궁극기 이펙트")]
    [SerializeField] private ParticleSystem _ultimateChargeParticle;
    [SerializeField] private ParticleSystem _ultimateReleaseParticle;
    [SerializeField] private Light _ultimateLight;
    [SerializeField] private AudioClip _ultimateChargeSound;
    [SerializeField] private AudioClip _ultimateReleaseSound;
    
    // 사운드 재생용 AudioSource
    private AudioSource _audioSource;
    
    // 이펙트 모드
    private string _effectMode = "";
    
    // 네트워크 개체 참조
    private NetworkObject _networkObject;
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        _networkObject = GetComponent<NetworkObject>();
        
        // 모든 이펙트 초기 상태 비활성화
        DisableAllEffects();
    }
    
    private void Start()
    {
        // 이름에 따라 적절한 이펙트 활성화
        string objectName = gameObject.name.ToLower();
        
        if (objectName.Contains("slash"))
        {
            _effectMode = "slash";
            PlaySlashEffect();
        }
        else if (objectName.Contains("counter"))
        {
            _effectMode = "counter";
            PlayCounterEffect();
        }
        else if (objectName.Contains("dash"))
        {
            _effectMode = "dash";
            PlayDashEffect();
        }
        else if (objectName.Contains("ultimate"))
        {
            // 충전과 방출 단계 구분
            if (objectName.Contains("charge"))
            {
                _effectMode = "ultimate_charge";
                PlayUltimateChargeEffect();
            }
            else if (objectName.Contains("release"))
            {
                _effectMode = "ultimate_release";
                PlayUltimateReleaseEffect();
            }
            else
            {
                // 기본값은 충전 단계로 설정
                _effectMode = "ultimate_charge";
                PlayUltimateChargeEffect();
            }
        }
    }
    
    /// <summary>
    /// 모든 이펙트 비활성화
    /// </summary>
    private void DisableAllEffects()
    {
        // Q 스킬
        if (_slashParticle != null) _slashParticle.Stop();
        if (_slashTrail != null) _slashTrail.enabled = false;
        
        // W 스킬
        if (_counterParticle != null) _counterParticle.Stop();
        if (_counterShield != null) _counterShield.SetActive(false);
        
        // E 스킬
        if (_dashParticle != null) _dashParticle.Stop();
        if (_dashTrail != null) _dashTrail.enabled = false;
        if (_dashAfterimage != null) _dashAfterimage.SetActive(false);
        
        // R 스킬
        if (_ultimateChargeParticle != null) _ultimateChargeParticle.Stop();
        if (_ultimateReleaseParticle != null) _ultimateReleaseParticle.Stop();
        if (_ultimateLight != null) _ultimateLight.enabled = false;
    }
    
    /// <summary>
    /// Q 스킬 - 강한 베기 이펙트 재생
    /// </summary>
    public void PlaySlashEffect()
    {
        if (_slashParticle != null)
        {
            _slashParticle.Play();
        }
        
        if (_slashTrail != null)
        {
            _slashTrail.enabled = true;
        }
        
        if (_audioSource != null && _slashSound != null)
        {
            _audioSource.PlayOneShot(_slashSound);
        }
        
        // 일정 시간 후 이펙트 제거
        if (IsServer() && _networkObject != null)
        {
            Invoke(nameof(DestroyEffect), _slashDuration);
        }
    }
    
    /// <summary>
    /// W 스킬 - 반격기 이펙트 재생
    /// </summary>
    public void PlayCounterEffect()
    {
        if (_counterParticle != null)
        {
            _counterParticle.Play();
        }
        
        if (_counterShield != null)
        {
            _counterShield.SetActive(true);
        }
        
        if (_audioSource != null && _counterReadySound != null)
        {
            _audioSource.PlayOneShot(_counterReadySound);
        }
    }
    
    /// <summary>
    /// W 스킬 - 반격 성공 이펙트 재생
    /// </summary>
    public void PlayCounterSuccessEffect()
    {
        if (_counterParticle != null)
        {
            var emission = _counterParticle.emission;
            emission.rateOverTime = emission.rateOverTime.constant * 3; // 파티클 증가
        }
        
        if (_audioSource != null && _counterSuccessSound != null)
        {
            _audioSource.PlayOneShot(_counterSuccessSound);
        }
        
        // 일정 시간 후 이펙트 제거
        if (IsServer() && _networkObject != null)
        {
            Invoke(nameof(DestroyEffect), 1.0f);
        }
    }
    
    /// <summary>
    /// E 스킬 - 이동기 이펙트 재생
    /// </summary>
    public void PlayDashEffect()
    {
        if (_dashParticle != null)
        {
            _dashParticle.Play();
        }
        
        if (_dashTrail != null)
        {
            _dashTrail.enabled = true;
        }
        
        if (_dashAfterimage != null)
        {
            _dashAfterimage.SetActive(true);
        }
        
        if (_audioSource != null && _dashSound != null)
        {
            _audioSource.PlayOneShot(_dashSound);
        }
    }
    
    /// <summary>
    /// R 스킬 - 궁극기 충전 이펙트 재생
    /// </summary>
    public void PlayUltimateChargeEffect()
    {
        if (_ultimateChargeParticle != null)
        {
            _ultimateChargeParticle.Play();
        }
        
        if (_ultimateLight != null)
        {
            _ultimateLight.enabled = true;
            StartCoroutine(PulseLight(_ultimateLight, 1.0f, 3.0f, 1.0f)); // 밝기 변화
        }
        
        if (_audioSource != null && _ultimateChargeSound != null)
        {
            _audioSource.PlayOneShot(_ultimateChargeSound);
        }
    }
    
    /// <summary>
    /// R 스킬 - 궁극기 방출 이펙트 재생
    /// </summary>
    public void PlayUltimateReleaseEffect()
    {
        if (_ultimateChargeParticle != null)
        {
            _ultimateChargeParticle.Stop();
        }
        
        if (_ultimateReleaseParticle != null)
        {
            _ultimateReleaseParticle.Play();
        }
        
        if (_ultimateLight != null)
        {
            StopAllCoroutines();
            _ultimateLight.intensity = 5.0f;
            StartCoroutine(FadeOutLight(_ultimateLight, 2.0f));
        }
        
        if (_audioSource != null && _ultimateReleaseSound != null)
        {
            _audioSource.PlayOneShot(_ultimateReleaseSound);
        }
        
        // 일정 시간 후 이펙트 제거
        if (IsServer() && _networkObject != null)
        {
            Invoke(nameof(DestroyEffect), 3.0f);
        }
    }
    
    /// <summary>
    /// 효과 제거
    /// </summary>
    private void DestroyEffect()
    {
        if (IsServer() && _networkObject != null && _networkObject.IsSpawned)
        {
            _networkObject.Despawn();
        }
    }
    
    /// <summary>
    /// 이 객체가 서버에서 실행 중인지 확인
    /// </summary>
    private bool IsServer()
    {
        // NetworkBehaviour 컴포넌트 참조 가져오기
        NetworkBehaviour networkBehaviour = GetComponent<NetworkBehaviour>();
    
        return _networkObject != null && _networkObject.IsSpawned &&
               networkBehaviour != null && (networkBehaviour.IsServer || networkBehaviour.IsHost);
    }
    
    /// <summary>
    /// 라이트 밝기 변화 코루틴
    /// </summary>
    private System.Collections.IEnumerator PulseLight(Light light, float minIntensity, float maxIntensity, float duration)
    {
        float timer = 0;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // 싸인 함수로 부드러운 밝기 변화
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, (Mathf.Sin(t * Mathf.PI * 2) + 1) * 0.5f);
            light.intensity = intensity;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 라이트 페이드 아웃 코루틴
    /// </summary>
    private System.Collections.IEnumerator FadeOutLight(Light light, float duration)
    {
        float startIntensity = light.intensity;
        float timer = 0;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            light.intensity = Mathf.Lerp(startIntensity, 0, t);
            
            yield return null;
        }
        
        light.enabled = false;
    }
    
    /// <summary>
    /// 외부에서 이펙트 모드 설정
    /// </summary>
    public void SetEffectMode(string mode)
    {
        _effectMode = mode.ToLower();
        
        // 기존 이펙트 중지
        DisableAllEffects();
        
        // 새 이펙트 재생
        switch (_effectMode)
        {
            case "slash":
                PlaySlashEffect();
                break;
            case "counter":
                PlayCounterEffect();
                break;
            case "dash":
                PlayDashEffect();
                break;
            case "ultimate_charge":
                PlayUltimateChargeEffect();
                break;
            case "ultimate_release":
                PlayUltimateReleaseEffect();
                break;
        }
    }
}