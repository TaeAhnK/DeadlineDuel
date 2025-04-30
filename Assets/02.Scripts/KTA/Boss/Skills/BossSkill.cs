using System;
using Unity.Netcode;
using UnityEngine;

namespace Boss.Skills
{
    public abstract class BossSkill : NetworkBehaviour
    {
        [field: SerializeField] public String BossSkillName { get; protected set; }
        [field: SerializeField] public SkillIndicator SkillIndicator { get; protected set; }
        protected Collider[] Colliders = new Collider[2];
        public int BossSkillHash { get; private set; }
        protected Vector3 BossPos;
        protected Vector3 TargetPos;
        protected virtual void Awake()
        {
           BossSkillHash =  Animator.StringToHash(BossSkillName);
        }

        [ServerRpc] public virtual void PlayIndicatorServerRpc(Vector3 BossPos, Vector3 TargetPos) { }

        [ClientRpc] public virtual void PlayIndicatorClientRpc(Vector3 BossPos, Vector3 TargetPos) {}
        [ServerRpc] public virtual void PlayColliderServerRpc() {}
        [ClientRpc] public virtual void PlayEffectClientRpc() {}
    }
}