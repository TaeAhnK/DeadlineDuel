using System;
using System.Collections;
using System.Collections.Generic;
using Boss.Skills;
using Unity.Netcode;
using UnityEngine;


namespace Boss.Skills
{
    public class BossSkill_TargetShoot : BossSkill
    {
        [field: Header("Dependencies")]
        [field: SerializeField] private ParticleSystem skillEffectParticle;
        
        
        [ClientRpc]
        public override void ActivateIndicatorClientRpc()
        {
            if (!bossCharacter.IsClientBoss) return;
            SkillIndicator.ActivateIndicator(BossPos.Value, 360f, 1.6f, 0f);
        }

        [ClientRpc]
        public override void ActivateSkillEffectClientRpc()
        {
            if (!bossCharacter.IsClientBoss) return;
            skillEffectParticle.Play();
        }

        public override void ActivateDamageCollider(float bossAtk)
        {

        }
    
    }
}
