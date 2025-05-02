using UnityEngine;
using Unity.Netcode;

namespace Boss.Skills
{
    public class BossSkill_Slash : BossSkill
    {
        [field: Header("Dependencies")]
        [field: SerializeField] private ParticleSystem hitParticle;
        [field: SerializeField] private ParticleSystem skillEffectParticle;

        [field: Header("Skill Data")]
        [field: SerializeField] private float radius;

        [field: SerializeField] private float damageCoeff;

        [ClientRpc]
        public override void ActivateIndicatorClientRpc()
        {
            SkillIndicator.ActivateIndicator(BossPos.Value, 180f, 0.5f);
        }

        [ClientRpc]
        public override void ActivateSkillEffectClientRpc()
        {
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
                    Vector3 dir = (Colliders[i].transform.position - BossPos.Value).normalized;
                    float angle = Vector3.Angle(forward, dir);
                    if (angle <= 90f) // half circle
                    {
                        Debug.Log("[Boss] Hit Object : " + Colliders[i].gameObject.name);
                        if (Colliders[i].TryGetComponent<IDamageable>(out var damageable))
                        {
                            damageable.TakeDamageServerRpc(bossAtk * damageCoeff);
                        }
                    }
                }   
            }
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(BossPos.Value, radius);
        
            // Draw Half Circle
            int segments = 32;
            float angleStep = 180f / segments;
            Vector3 forward = transform.forward;
            for (int i = 0; i < segments; i++)
            {
                float angleA = -90f + angleStep * i;
                float angleB = -90f + angleStep * (i + 1);
                Vector3 dirA = Quaternion.Euler(0, angleA, 0) * forward;
                Vector3 dirB = Quaternion.Euler(0, angleB, 0) * forward;
                Gizmos.DrawLine(BossPos.Value + dirA * radius, BossPos.Value + dirB * radius);
            }
        }
        
        // [ClientRpc]
        // private void PlayHitEffectClientRpc(Vector3 pos)
        // {
        //     Instantiate(hitParticle, pos, Quaternion.identity);
        // }
        //
    }
}