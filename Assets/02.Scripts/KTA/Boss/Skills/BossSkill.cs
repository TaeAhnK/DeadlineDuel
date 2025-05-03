using System;
using Unity.Netcode;
using UnityEngine;

namespace Boss.Skills
{
    public abstract class BossSkill : NetworkBehaviour
    {
        [field: Header("Dependencies")]
        [field: SerializeField] public SkillIndicator SkillIndicator { get; protected set; }
        
        [field: Header("Skill Data")]
        [field: SerializeField] public String BossSkillName { get; protected set; }
        [field: SerializeField] public float IndicatorTime { get; protected set; }
        [field: SerializeField] public float EffectTime { get; protected set; }
        [field: SerializeField] public float SkillAnimationTime { get; protected set; }
        [field: SerializeField] protected float damageCoeff;

        public int BossSkillHash { get; private set; }
        
        protected Collider[] Colliders = new Collider[2];
        protected NetworkVariable<Vector3> BossPos = new NetworkVariable<Vector3>(default(Vector3));
        protected NetworkVariable<Vector3> TargetPos = new NetworkVariable<Vector3>(default(Vector3));
        
        protected virtual void Awake()
        {
           BossSkillHash =  Animator.StringToHash(BossSkillName);
        }
        
        public virtual void ActivateSkill(Vector3 bossPos, Vector3 targetPos)
        {
            if (!IsServer) return;  // Only on Server
            this.BossPos.Value = bossPos;
            this.TargetPos.Value = targetPos;
        }
        [ClientRpc] public virtual void ActivateIndicatorClientRpc() { }
        [ClientRpc] public virtual void ActivateSkillEffectClientRpc() { }
        public virtual void ActivateDamageCollider(float bossAtk) { }
    }
}