using Unity.Netcode;
using UnityEngine;

namespace Boss.Skills
{
    public class SampleBossSkill : BossSkill
    {
    //     [field: SerializeField] private float radius;
    //     [field: SerializeField] private ParticleSystem hitParticle;
    //     [field: SerializeField] private ParticleSystem skillEffectParticle;
    //     
    //     [ServerRpc]
    //     public override void PlayIndicatorServerRpc(Vector3 BossPos, Vector3 TargetPos)
    //     {
    //         this.BossPos = BossPos;
    //         this.TargetPos = TargetPos;
    //         PlayIndicatorClient(BossPos, TargetPos);
    //     }
    //     
    //     public override void PlayIndicatorClient(Vector3 BossPos, Vector3 TargetPos)
    //     {
    //         this.BossPos = BossPos;
    //         this.TargetPos = TargetPos;
    //         gameObject.transform.position = BossPos;
    //         SkillIndicator.ActivateIndicator(BossPos, 180f, 0.5f);
    //     }
    //
    //     [ServerRpc]
    //     public override void PlayColliderServerRpc()
    //     {
    //         var size = Physics.OverlapSphereNonAlloc(BossPos, radius, Colliders);
    //         
    //         Vector3 forward = transform.forward;
    //         
    //         if (size > 0)
    //         {
    //             foreach (var col in Colliders)
    //             {
    //                 Vector3 dir = (col.transform.position - BossPos).normalized;
    //                 float angle = Vector3.Angle(forward, dir);
    //                 if (angle <= 90f) // 180도(반원) 이내
    //                 {
    //                     Debug.Log(col.gameObject.name);
    //                     // Give Damage
    //                     PlayHitEffectClientRpc(col.bounds.center);
    //                 }
    //             }   
    //         }
    //     }
    //     
    //     public override void PlayEffectClient()
    //     {
    //         skillEffectParticle.Play();
    //     }
    //
    //     [ClientRpc]
    //     private void PlayHitEffectClientRpc(Vector3 pos)
    //     {
    //         Instantiate(hitParticle, pos, Quaternion.identity);
    //     }
    //     
    //     void OnDrawGizmos()
    //     {
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawWireSphere(BossPos, radius);
    //     
    //         // 반원(180도) 외곽선 그리기 예시
    //         int segments = 32;
    //         float angleStep = 180f / segments;
    //         Vector3 forward = transform.forward;
    //         for (int i = 0; i < segments; i++)
    //         {
    //             float angleA = -90f + angleStep * i;
    //             float angleB = -90f + angleStep * (i + 1);
    //             Vector3 dirA = Quaternion.Euler(0, angleA, 0) * forward;
    //             Vector3 dirB = Quaternion.Euler(0, angleB, 0) * forward;
    //             Gizmos.DrawLine(BossPos + dirA * radius, BossPos + dirB * radius);
    //         }
    //     }
    //
    }
}