using System;
using Boss.Skills;
using Unity.Netcode;
using UnityEngine;

namespace Boss.Skills
{
     public class BossSkill_GroundPowerHit : BossSkill
    {
        [field: Header("Dependencies")]
        [field: SerializeField] private ParticleSystem skillEffectParticle;
        
        [field: Header("Skill Data")]
        [field: SerializeField] private float radius;
        [field: SerializeField] private float innerRadius;
        
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
            int layerMask = LayerMask.GetMask("Player");
            var size = Physics.OverlapSphereNonAlloc(BossPos.Value, radius, Colliders, layerMask);
                
            Vector3 forward = transform.forward;
                
            if (size > 0)
            {
                for  (int i = 0; i < size; i++) // Do not use foreach on NonAlloc
                {
                    Vector3 bossPosXZ = new Vector3(BossPos.Value.x, 0, BossPos.Value.z);
                    Vector3 targetPosXZ = new Vector3(Colliders[i].transform.position.x, 0, Colliders[i].transform.position.z);
                    
                    float sqrDistance = (bossPosXZ - targetPosXZ).sqrMagnitude;
                    
                    if (sqrDistance <= radius * radius && sqrDistance >= innerRadius * innerRadius)
                    {
                        Debug.Log("[Boss] Hit Object : " + Colliders[i].gameObject.name + Math.Sqrt(sqrDistance));
                        if (Colliders[i].TryGetComponent<IDamageable>(out var damageable))
                        {
                            damageable.TakeDamage(bossAtk * damageCoeff);
                        }
                    }
                }   
            }
        }
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(BossPos.Value, radius);
            Gizmos.DrawWireSphere(BossPos.Value, innerRadius);
        }
    }
   
}