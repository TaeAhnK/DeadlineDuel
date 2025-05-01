using Boss.Skills;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace Boss
{
    public class BossStateMachine : StateMachine.StateMachine
    {
        [field: SerializeField] public BossCharacter BossCharacter { get; set; }
        [field: SerializeField] public NetworkAnimator NetworkAnimator { get; private set; }
        [field: SerializeField] public NetworkTransform NetworkTransform { get; private set; } 
        [field: SerializeField] public NavMeshAgent NavMeshAgent { get; private set; }
        [field: SerializeField] public BossSkillController BossSkillController { get; private set; }
        [field: SerializeField] public float PlayerDetectRange { get; private set; } = 10f;
        [field: SerializeField] public float MovementSpeed { get; private set; } = 1f;
        // [field: SerializeField] public GameObject Player { get;  private set; } // TODO : Need Change in Multiplayer
        
        public BossIdleState IdleState { get; private set; }
        public BossWakeState WakeState  { get; private set; }
        public BossChaseState ChaseState { get; private set; }
        public BossAttackState AttackState { get; private set; }
        public BossDeathState DeathState { get; private set; }
        public BossSleepState SleepState { get; private set; }

        protected void Awake()
        {
            IdleState = new BossIdleState(this);
            WakeState = new BossWakeState(this);
            ChaseState = new BossChaseState(this);
            AttackState = new BossAttackState(this);
            DeathState = new BossDeathState(this);
            SleepState = new BossSleepState(this);
            
            StateDict.Add((byte) BossState.Idle, IdleState);
            StateDict.Add((byte) BossState.Wake, WakeState);
            StateDict.Add((byte) BossState.Chase, ChaseState);
            StateDict.Add((byte) BossState.Attack, AttackState);
            StateDict.Add((byte) BossState.Death, DeathState);
            StateDict.Add((byte) BossState.Sleep, SleepState);
            
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
                RequestStateChangeServerRpc((byte) BossState.Sleep);
            }
        }

        private void OnEnable()
        {
            if (!IsHost) return;
            // TODO : Subscribe OnWakeMessage to Wake
            // TODO : Subscribe OnDeathMessage to Death
        }
        
        private void OnDisable()
        {
            if (!IsHost) return;
            
             // TODO : Subscribe OnWakeMessage to Wake
             // TODO : Unsubscribe OnDeathMessage to Death
        }
        
        private void OnDeathMessage()
        {
            if (IsHost)
            {
                RequestStateChangeServerRpc((byte) BossState.Death);
            }
        }

        public void OnWakeMessage() //-> to private
        {
            if (IsHost)
            {
                RequestStateChangeServerRpc((byte) BossState.Wake);
            }
        }
        
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