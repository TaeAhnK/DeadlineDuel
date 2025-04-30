using System;
using Boss.Skills;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Boss
{
    public class BossStateMachine : StateMachine.StateMachine
    {
        //[field: SerializeField] public Animator Animator { get; private set; }
        [field: SerializeField] public NetworkAnimator NetworkAnimator { get; private set; }
        [field: SerializeField] public NetworkTransform NetworkTransform { get; private set; } 
        [field: SerializeField] public NavMeshAgent NavMeshAgent { get; private set; }
        [field: SerializeField] public BossSkillController  BossSkillController { get; private set; }
        [field: SerializeField] public float PlayerDetectRange { get; private set; } = 10f;
        [field: SerializeField] public float MovementSpeed { get; private set; } = 1f;
        [field: SerializeField] public GameObject Player { get;  private set; } // TODO : Need Change in Multiplayer
        
        public BossIdleState IdleState { get; private set; }
        public BossWakeState WakeState  { get; private set; }
        public BossChaseState ChaseState { get; private set; }
        public BossAttackState AttackState { get; private set; }
        public BossDeathState DeathState { get; private set; }

        protected void Awake()
        {
            IdleState = new BossIdleState(this);
            WakeState = new BossWakeState(this);
            ChaseState = new BossChaseState(this);
            AttackState = new BossAttackState(this);
            DeathState = new BossDeathState(this);
            
            StateDict.Add((byte) BossState.Idle, IdleState);
            StateDict.Add((byte) BossState.Wake, WakeState);
            StateDict.Add((byte) BossState.Chase, ChaseState);
            StateDict.Add((byte) BossState.Attack, AttackState);
            StateDict.Add((byte) BossState.Death, DeathState);
            
            NetworkTransform.Interpolate = true;
            NetworkTransform.PositionThreshold = 0.1f;
            NetworkTransform.RotAngleThreshold = 3f;
            NetworkTransform.ScaleThreshold = 0.1f;
            NavMeshAgent.updatePosition = true;
            NavMeshAgent.updateRotation = true;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsHost)
            {
                RequestStateChangeServerRpc((byte) BossState.Wake);
            }
        }

        // private void OnEnable()
        // {
        //     // TODO : Subscribe OnDeathMessage to Death
        // }
        //
        // private void OnDisable()
        // {
        //     // TODO : Unsubscribe OnDeathMessage to Death
        // }
        //
        // private void OnDeathMessage()
        // {
        //     SwitchState(DeathState);
        // }

        public void OnAnimationEnd()
        {
            CurrentState?.OnAnimationEnd();
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, PlayerDetectRange);
        }
    }
}