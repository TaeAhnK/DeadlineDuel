using UnityEngine;

/// <summary>
/// 검 캐릭터 데이터 설정 및 초기화를 위한 스크립터블 오브젝트
/// </summary>
[CreateAssetMenu(fileName = "SwordCharacterData", menuName = "Game/Character Data/Sword Character")]
public class SwordCharacterData : ScriptableObject
{
    [Header("캐릭터 기본 정보")]
    public string characterName = "검사";
    public string characterDescription = "강력한 검술로 적을 압도하는 전사입니다.";
    public Sprite characterPortrait;
    public GameObject characterPrefab;
    public Color themeColor = new Color(0.8f, 0.2f, 0.2f); // 붉은색 테마
    
    [Header("Q 스킬 - 강한 베기")]
    public string slashName = "강한 베기";
    public string slashDescription = "전방 부채꼴 범위에 강력한 베기 공격을 가합니다.";
    public float slashDamage = 25f;
    public float slashRange = 3f;
    public float slashWidth = 120f; // 부채꼴 각도
    public float slashCooldown = 5f;
    public Sprite slashIcon;
    public GameObject slashEffectPrefab;
    
    [Header("W 스킬 - 반격기")]
    public string counterName = "칼날 반격";
    public string counterDescription = "짧은 시간 동안 방어 태세를 취하고, 공격을 받으면 더 강력한 공격으로 반격합니다.";
    public float counterDuration = 1.5f; // 반격 가능 시간
    public float counterDamage = 40f; // 반격 시 데미지
    public float counterCooldown = 12f;
    public Sprite counterIcon;
    public GameObject counterEffectPrefab;
    
    [Header("E 스킬 - 이동기")]
    public string dashName = "검풍 질주";
    public string dashDescription = "전방으로 빠르게 돌진하며 경로 상의 적들에게 피해를 입힙니다.";
    public float dashDistance = 5f; // 돌진 거리
    public float dashDamage = 15f; // 돌진 데미지
    public float dashSpeed = 20f; // 돌진 속도
    public float dashCooldown = 8f;
    public Sprite dashIcon;
    public GameObject dashEffectPrefab;
    
    [Header("R 스킬 - 궁극기")]
    public string ultimateName = "차원의 일격";
    public string ultimateDescription = "집중 후 강력한 차원의 검기를 방출해 넓은 범위의 적들에게 치명적인 피해를 입힙니다.";
    public float ultimateDamage = 80f;
    public float ultimateRange = 6f;
    public float ultimateRadius = 4f; // AOE 반경
    public float ultimateCastTime = 1f; // 시전 시간
    public float ultimateCooldown = 60f;
    public Sprite ultimateIcon;
    public GameObject ultimateEffectPrefab;
    
    /// <summary>
    /// 캐릭터 데이터 클래스로 변환
    /// </summary>
    public CharacterData ToCharacterData()
    {
        CharacterData data = new CharacterData
        {
            name = characterName,
            characterPrefab = characterPrefab,
            portraitImage = characterPortrait,
            themeColor = themeColor,
            skills = new CharacterData.SkillData[4]
        };
        
        // Q 스킬
        data.skills[0] = new CharacterData.SkillData
        {
            name = slashName,
            description = slashDescription,
            skillIcon = slashIcon,
            skillEffectPrefab = slashEffectPrefab,
            cooldown = slashCooldown,
            damage = slashDamage
        };
        
        // W 스킬
        data.skills[1] = new CharacterData.SkillData
        {
            name = counterName,
            description = counterDescription,
            skillIcon = counterIcon,
            skillEffectPrefab = counterEffectPrefab,
            cooldown = counterCooldown,
            damage = counterDamage
        };
        
        // E 스킬
        data.skills[2] = new CharacterData.SkillData
        {
            name = dashName,
            description = dashDescription,
            skillIcon = dashIcon,
            skillEffectPrefab = dashEffectPrefab,
            cooldown = dashCooldown,
            damage = dashDamage
        };
        
        // R 스킬
        data.skills[3] = new CharacterData.SkillData
        {
            name = ultimateName,
            description = ultimateDescription,
            skillIcon = ultimateIcon,
            skillEffectPrefab = ultimateEffectPrefab,
            cooldown = ultimateCooldown,
            damage = ultimateDamage
        };
        
        return data;
    }
    
    /// <summary>
    /// SwordCharacterSkills 컴포넌트 초기화
    /// </summary>
    public void SetupSwordCharacterSkills(SwordCharacterSkills skills)
    {
        // Q 스킬 설정
        skills.SetSlashSkill(slashDamage, slashRange, slashWidth, slashCooldown, slashEffectPrefab);
        
        // W 스킬 설정
        skills.SetCounterSkill(counterDuration, counterDamage, counterCooldown, counterEffectPrefab);
        
        // E 스킬 설정
        skills.SetDashSkill(dashDistance, dashDamage, dashSpeed, dashCooldown, dashEffectPrefab);
        
        // R 스킬 설정
        skills.SetUltimateSkill(ultimateDamage, ultimateRange, ultimateRadius, ultimateCastTime, ultimateCooldown, ultimateEffectPrefab);
    }
}