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
        [field: SerializeField] public BossCore BossCore { get; set; }

        
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
            BossCore.NetworkTransform.Interpolate = true;
            BossCore.NetworkTransform.PositionThreshold = 0.1f;
            BossCore.NetworkTransform.RotAngleThreshold = 3f;
            BossCore.NetworkTransform.ScaleThreshold = 0.1f;
            
            if (IsServer)
            {
                BossCore.NavMeshAgent.updatePosition = true;
                BossCore.NavMeshAgent.updateRotation = true;
                    
                BossCore.BossStats.Speed.OnValueChanged += OnSpeedChanged;
                BossCore.BossStats.OnDeath += OnDeathMessage;

                GamePlayManager.Instance.OnBossWake += OnWakeMessage;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (!IsServer) return;  // Server Code

            if (BossCore.BossStats)
            {
                BossCore.BossStats.Speed.OnValueChanged -= OnSpeedChanged;
                BossCore.BossStats.OnDeath -= OnDeathMessage;
            }
            
            GamePlayManager.Instance.OnBossWake -= OnWakeMessage;
        }

        private void OnSpeedChanged(float prev, float next)
        {
            if (!IsServer) return; // Server Code
            BossCore.NavMeshAgent.speed = next;
        }
        
        private void OnDeathMessage()
        {
            if (!IsServer) return;  // Server Code
            ChangeState((byte) BossState.Death);
        }

        public void OnWakeMessage() // TODO : to private and subscribe
        {
            Debug.Log("Wake" + gameObject.name);
            if (!IsServer) return;  // Server Code
            Debug.Log("Is Server Wake" + gameObject.name);
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