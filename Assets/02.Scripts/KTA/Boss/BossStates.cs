using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boss
{
    public enum BossState : byte
    {
        Wake = 0,
        Idle = 1,
        Chase = 2,
        Attack = 3,
        Death = 4,
    }
    
    public class BossIdleState : BossBaseState
    {
        private readonly int locomotionHash = Animator.StringToHash("Locomotion");
        private readonly int speedHash = Animator.StringToHash("Speed");
        private const float CrossFadeDuration = 0.1f;
        private const float AnimatorDampTime = 0.1f;
        private const float IdleDuration = 1f;
        private float stateEnterTime;
        public BossIdleState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsHost) return;
            
            Debug.Log("Idle State");
            stateEnterTime = Time.time;
        }

        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsHost) return; // Only on host
            
            SetAnimatorFloat(speedHash, 0f, AnimatorDampTime, deltaTime);

            TurnToPlayer();
            if (!IsPlayerInRange()) // and Player Not Dead
            {
                StateMachine.RequestStateChangeServerRpc((byte) BossState.Chase);
            }
            
            if (Time.time - stateEnterTime < IdleDuration)
            {
                return;
            }
            
            if (IsPlayerInRange()) // and Player Not Dead
            {
                StateMachine.RequestStateChangeServerRpc((byte) BossState.Attack);
            }
        }

        public override void Exit() { }
    }

    public class BossWakeState : BossBaseState
    {
        private int wakeTriggerHash = Animator.StringToHash("Wake");
        public BossWakeState(BossStateMachine stateMachine) : base(stateMachine) { }

        private bool flag = false;
        public override void Enter() { }

        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsHost) return;
            if (!flag)  // NetworkAnimator 초기화 문제로 Tick에서 처리
            {
                StateMachine.NetworkAnimator.SetTrigger(wakeTriggerHash);
                flag = true;
            }
        }

        public override void OnAnimationEnd()
        {
            if (!StateMachine.IsHost) return;
            StateMachine.RequestStateChangeServerRpc((byte) BossState.Idle);
        }
        
        public override void Exit() { }
    }

    public class BossChaseState : BossBaseState
    {
        private readonly int locomotionHash = Animator.StringToHash("Locomotion");
        private readonly int speedHash = Animator.StringToHash("Speed");
        private const float CrossFadeDuration = 0.1f;
        private const float AnimatorDampTime = 0.1f;
        public BossChaseState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsHost) return;
            
            Debug.Log("Chase State");
            StateMachine.NavMeshAgent.speed = StateMachine.MovementSpeed;
        }
        
        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsHost) return;

            if (IsPlayerInRange())
            {
                StateMachine.RequestStateChangeServerRpc((byte) BossState.Idle);
                return;
            }

            TurnToPlayer();
            MoveToPlayer(deltaTime);
            
            float speedPercent = StateMachine.NavMeshAgent.velocity.magnitude / StateMachine.MovementSpeed;
            SetAnimatorFloat(speedHash, speedPercent, AnimatorDampTime, deltaTime);
        }

        public override void Exit()
        {
            if (!StateMachine.IsHost) return;

            StateMachine.NavMeshAgent.ResetPath();
            StateMachine.NavMeshAgent.velocity = Vector3.zero;
            
            StateMachine.NetworkTransform.Teleport(StateMachine.transform.position, StateMachine.transform.rotation, StateMachine.transform.localScale);
        }

        private void MoveToPlayer(float deltaTime)
        {
            if (!StateMachine.IsHost) return;
            
            if (StateMachine.Player)
            {
                StateMachine.NavMeshAgent.destination = StateMachine.Player.transform.position;
            }
        }
    }

    public class BossAttackState : BossBaseState
    {
        public BossAttackState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsHost) return;
            Debug.Log("Attack State");
            StateMachine.BossSkillController.ActivateSkillServerRpc();
        }
        
        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsHost) return;
            
            if (!StateMachine.BossSkillController.IsSkillActive.Value)
            {
                StateMachine.RequestStateChangeServerRpc((byte) BossState.Idle);
            }
        }

        public override void Exit() { }
    }

    public class BossDeathState : BossBaseState
    {
        public BossDeathState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsHost) return;
            throw new System.NotImplementedException();
        }

        public override void Tick(float deltaTime) { }

        public override void Exit() { }
    }

}
