using UnityEngine;

/// <summary>
/// SwordCharacterSkills에 설정 메서드를 추가하는 확장 클래스
/// </summary>
public static class SwordCharacterSkillsExtension
{
    /// <summary>
    /// Q 스킬 - 강한 베기 설정
    /// </summary>
    public static void SetSlashSkill(this SwordCharacterSkills skills, float damage, float range, float width, float cooldown, GameObject effectPrefab)
    {
        // 리플렉션을 사용하여 private 필드에 접근
        var slashDamageField = skills.GetType().GetField("_slashDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slashRangeField = skills.GetType().GetField("_slashRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slashWidthField = skills.GetType().GetField("_slashWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slashCooldownField = skills.GetType().GetField("_slashCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slashEffectField = skills.GetType().GetField("_slashEffectPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (slashDamageField != null) slashDamageField.SetValue(skills, damage);
        if (slashRangeField != null) slashRangeField.SetValue(skills, range);
        if (slashWidthField != null) slashWidthField.SetValue(skills, width);
        if (slashCooldownField != null) slashCooldownField.SetValue(skills, cooldown);
        if (slashEffectField != null) slashEffectField.SetValue(skills, effectPrefab);
    }
    
    /// <summary>
    /// W 스킬 - 반격기 설정
    /// </summary>
    public static void SetCounterSkill(this SwordCharacterSkills skills, float duration, float damage, float cooldown, GameObject effectPrefab)
    {
        var counterDurationField = skills.GetType().GetField("_counterDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var counterDamageField = skills.GetType().GetField("_counterDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var counterCooldownField = skills.GetType().GetField("_counterCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var counterEffectField = skills.GetType().GetField("_counterEffectPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (counterDurationField != null) counterDurationField.SetValue(skills, duration);
        if (counterDamageField != null) counterDamageField.SetValue(skills, damage);
        if (counterCooldownField != null) counterCooldownField.SetValue(skills, cooldown);
        if (counterEffectField != null) counterEffectField.SetValue(skills, effectPrefab);
    }
    
    /// <summary>
    /// E 스킬 - 이동기 설정
    /// </summary>
    public static void SetDashSkill(this SwordCharacterSkills skills, float distance, float damage, float speed, float cooldown, GameObject effectPrefab)
    {
        var dashDistanceField = skills.GetType().GetField("_dashDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dashDamageField = skills.GetType().GetField("_dashDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dashSpeedField = skills.GetType().GetField("_dashSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dashCooldownField = skills.GetType().GetField("_dashCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dashEffectField = skills.GetType().GetField("_dashEffectPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (dashDistanceField != null) dashDistanceField.SetValue(skills, distance);
        if (dashDamageField != null) dashDamageField.SetValue(skills, damage);
        if (dashSpeedField != null) dashSpeedField.SetValue(skills, speed);
        if (dashCooldownField != null) dashCooldownField.SetValue(skills, cooldown);
        if (dashEffectField != null) dashEffectField.SetValue(skills, effectPrefab);
    }
    
    /// <summary>
    /// R 스킬 - 궁극기 설정
    /// </summary>
    public static void SetUltimateSkill(this SwordCharacterSkills skills, float damage, float range, float radius, float castTime, float cooldown, GameObject effectPrefab)
    {
        var ultimateDamageField = skills.GetType().GetField("_ultimateDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ultimateRangeField = skills.GetType().GetField("_ultimateRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ultimateRadiusField = skills.GetType().GetField("_ultimateRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ultimateCastTimeField = skills.GetType().GetField("_ultimateCastTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ultimateCooldownField = skills.GetType().GetField("_ultimateCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ultimateEffectField = skills.GetType().GetField("_ultimateEffectPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (ultimateDamageField != null) ultimateDamageField.SetValue(skills, damage);
        if (ultimateRangeField != null) ultimateRangeField.SetValue(skills, range);
        if (ultimateRadiusField != null) ultimateRadiusField.SetValue(skills, radius);
        if (ultimateCastTimeField != null) ultimateCastTimeField.SetValue(skills, castTime);
        if (ultimateCooldownField != null) ultimateCooldownField.SetValue(skills, cooldown);
        if (ultimateEffectField != null) ultimateEffectField.SetValue(skills, effectPrefab);
    }
}