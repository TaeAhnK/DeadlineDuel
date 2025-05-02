using Boss.Skills;
using Stats.Boss;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace Boss
{
    public class BossStateMachine : StateMachine.StateMachine
    {
        [field: Header("Dependencies")]
        [field: SerializeField] public BossCharacter BossCharacter { get; set; }
        [field: SerializeField] public NetworkAnimator NetworkAnimator { get; private set; }
        [field: SerializeField] public NetworkTransform NetworkTransform { get; private set; } 
        [field: SerializeField] public NavMeshAgent NavMeshAgent { get; private set; }
        [field: SerializeField] public BossSkillController BossSkillController { get; private set; }
        [field: SerializeField] public BossStats BossStats { get; private set; }
        
        [field: Header("Boss Settings")] // TODO : Move to Boss Stat
        [field: SerializeField] public float PlayerDetectRange { get; private set; } = 10f;
        [field: SerializeField] public float MovementSpeed { get; private set; } = 1f;

        private BossIdleState IdleState { get; set; }
        private BossWakeState WakeState  { get; set; }
        private BossChaseState ChaseState { get; set; }
        private BossAttackState AttackState { get; set; }
        private BossDeathState DeathState { get; set; }
        private BossSleepState SleepState { get; set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Init();
            
            ChangeState((byte) BossState.Sleep);    // Initial State : Sleep
        }
        
        private void Init()
        {
            // Set States
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
            
            // Set Network Transform
            NetworkTransform.Interpolate = true;
            NetworkTransform.PositionThreshold = 0.1f;
            NetworkTransform.RotAngleThreshold = 3f;
            NetworkTransform.ScaleThreshold = 0.1f;
            
            if (IsServer)
            {
                NavMeshAgent.updatePosition = true;
                NavMeshAgent.updateRotation = true;
                    
                BossStats.Speed.OnValueChanged += OnSpeedChanged;
                BossStats.OnDeath += OnDeathMessage;
                // TODO : Subscribe OnWakeMessage to Wake
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (!IsServer) return;  // Server Code

            if (BossStats)
            {
                BossStats.Speed.OnValueChanged -= OnSpeedChanged;
                BossStats.OnDeath -= OnDeathMessage;
            }
            // TODO : Unsubscribe OnWakeMessage to Wake
        }

        private void OnSpeedChanged(float prev, float next)
        {
            if (!IsServer) return; // Server Code
            NavMeshAgent.speed = next;
        }
        
        private void OnDeathMessage()
        {
            if (!IsServer) return;  // Server Code
            ChangeState((byte) BossState.Death);
        }

        public void OnWakeMessage() // TODO : to private and subscribe
        {
            if (!IsServer) return;  // Server Code
            
            ChangeState((byte) BossState.Wake);
        }
        
        // Draw Attack Range
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, PlayerDetectRange);
        }
    }
}