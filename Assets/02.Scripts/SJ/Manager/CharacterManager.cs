using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 프리팹과 관련 데이터를 관리하는 매니저 클래스
/// </summary>
public class CharacterManager : MonoBehaviour
{
    private static CharacterManager _instance;
    public static CharacterManager Instance => _instance;

    [Header("캐릭터 데이터")]
    [SerializeField] private CharacterData[] _characterDataList;
    
    [Header("캐릭터 스탯 데이터")]
    [SerializeField] private CharacterStatsData[] _characterStatsList;

    // 싱글톤 구현
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ValidateCharacterData();
    }

    /// <summary>
    /// 캐릭터 데이터 유효성 검사
    /// </summary>
    private void ValidateCharacterData()
    {
        if (_characterDataList == null || _characterDataList.Length == 0)
        {
            Debug.LogError("CharacterManager: 캐릭터 데이터가 설정되지 않았습니다!");
            return;
        }

        for (int i = 0; i < _characterDataList.Length; i++)
        {
            if (_characterDataList[i] == null)
            {
                Debug.LogError($"CharacterManager: 캐릭터 {i}의 데이터가 null입니다!");
                continue;
            }

            if (_characterDataList[i].characterPrefab == null)
            {
                Debug.LogError($"CharacterManager: 캐릭터 '{_characterDataList[i].name}'의 프리팹이 없습니다!");
            }

            // 스킬 검증
            for (int j = 0; j < _characterDataList[i].skills.Length; j++)
            {
                if (_characterDataList[i].skills[j] == null)
                {
                    Debug.LogWarning($"CharacterManager: 캐릭터 '{_characterDataList[i].name}'의 스킬 {j}가 null입니다!");
                }
                else if (_characterDataList[i].skills[j].skillEffectPrefab == null)
                {
                    Debug.LogWarning($"CharacterManager: 캐릭터 '{_characterDataList[i].name}'의 스킬 '{_characterDataList[i].skills[j].name}'의 이펙트 프리팹이 없습니다!");
                }
            }
        }
    }

    /// <summary>
    /// 인덱스로 캐릭터 데이터 가져오기
    /// </summary>
    public CharacterData GetCharacterData(int index)
    {
        if (index < 0 || index >= _characterDataList.Length)
        {
            Debug.LogError($"CharacterManager: 유효하지 않은 캐릭터 인덱스: {index}");
            return null;
        }

        return _characterDataList[index];
    }
    
    /// <summary>
    /// 캐릭터 스탯 데이터 가져오기
    /// </summary>
    public CharacterStatsData GetCharacterStats(int index)
    {
        if (_characterStatsList == null || index < 0 || index >= _characterStatsList.Length)
        {
            Debug.LogWarning($"CharacterManager: 유효하지 않은 캐릭터 인덱스: {index} 또는 스탯 데이터가 없습니다.");
            return null;
        }

        return _characterStatsList[index];
    }
    
    /// <summary>
    /// PlayerPrefs에서 선택된 캐릭터의 스탯 데이터 가져오기
    /// </summary>
    public CharacterStatsData GetSelectedCharacterStats()
    {
        int selectedIndex = GetSelectedCharacterIndex();
        return GetCharacterStats(selectedIndex);
    }

    /// <summary>
    /// 캐릭터 프리팹 가져오기
    /// </summary>
    public GameObject GetCharacterPrefab(int index)
    {
        CharacterData data = GetCharacterData(index);
        return data?.characterPrefab;
    }

    /// <summary>
    /// 스킬 이펙트 프리팹 가져오기
    /// </summary>
    public GameObject GetSkillEffectPrefab(int characterIndex, int skillIndex)
    {
        CharacterData data = GetCharacterData(characterIndex);
        if (data == null) return null;

        if (skillIndex < 0 || skillIndex >= data.skills.Length)
        {
            Debug.LogError($"CharacterManager: 유효하지 않은 스킬 인덱스: {skillIndex} (캐릭터: {data.name})");
            return null;
        }

        return data.skills[skillIndex]?.skillEffectPrefab;
    }

    /// <summary>
    /// PlayerPrefs에서 선택된 캐릭터 인덱스 가져오기
    /// </summary>
    public int GetSelectedCharacterIndex()
    {
        return PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
    }

    /// <summary>
    /// PlayerPrefs에서 선택된 캐릭터의 스킬 정보 가져오기
    /// </summary>
    public Dictionary<int, float> GetSelectedCharacterSkillCooldowns()
    {
        Dictionary<int, float> skillCooldowns = new Dictionary<int, float>();
        
        for (int i = 0; i < 4; i++)
        {
            float cooldown = PlayerPrefs.GetFloat($"SkillCooldown_{i}", 10f); // 기본값 10초
            skillCooldowns[i] = cooldown;
        }
        
        return skillCooldowns;
    }

    /// <summary>
    /// PlayerPrefs에서 선택된 캐릭터의 스킬 데미지 정보 가져오기
    /// </summary>
    public Dictionary<int, float> GetSelectedCharacterSkillDamages()
    {
        Dictionary<int, float> skillDamages = new Dictionary<int, float>();
        
        for (int i = 0; i < 4; i++)
        {
            float damage = PlayerPrefs.GetFloat($"SkillDamage_{i}", 10f); // 기본값 10
            skillDamages[i] = damage;
        }
        
        return skillDamages;
    }
}