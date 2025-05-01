using UnityEngine;

[System.Serializable]
public class CharacterData
{
    public string name;               // 캐릭터 이름
    public GameObject characterPrefab; // 캐릭터 프리팹
    public Sprite portraitImage;      // 캐릭터 초상화/아이콘
    
    [Header("스킬 정보")]
    public SkillData[] skills = new SkillData[4]; // Q, W, E, R 스킬
    
    [System.Serializable]
    public class SkillData
    {
        public string name;           // 스킬 이름
        public string description;    // 스킬 설명
        public Sprite skillIcon;      // 스킬 아이콘
        public GameObject skillEffectPrefab; // 스킬 이펙트 프리팹
        public float cooldown;        // 스킬 쿨다운 시간 
        public float damage;          // 스킬 기본 데미지
    }
}