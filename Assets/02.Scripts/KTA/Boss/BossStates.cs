using UnityEngine;

namespace Boss
{
    public enum BossState : byte
    {
        Wake = 0,
        Idle = 1,
        Chase = 2,
        Attack = 3,
        Death = 4,
        Sleep = 5
    }
    
    public class BossIdleState : BossBaseState
    {
        private readonly int speedHash = Animator.StringToHash("Speed");
        private readonly int turnSpeedHash = Animator.StringToHash("TurnSpeed");
        private const float AnimatorDampTime = 0.1f;
        private const float IdleDuration = 1f;
        private float stateEnterTime;
        private Quaternion lastRotation;
        private float currentTurnSpeed;
        public BossIdleState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsServer) return; // Only on Server
            
            Debug.Log("[Boss] Idle State");

            stateEnterTime = Time.time;
            lastRotation = StateMachine.transform.rotation;
        }

        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsServer) return; // Only on Server
            
            SetAnimatorFloat(speedHash, 0f, AnimatorDampTime, deltaTime);
            
            TurnToPlayer();
            SetAnimatorFloat(turnSpeedHash, CalcTurnSpeed(), AnimatorDampTime, deltaTime);
            
            // Not In Range
            if (!IsPlayerInRange()) // TODO : and Player Not Dead + TargetPlayer not null
            {
                SetAnimatorFloat(turnSpeedHash, 0f, AnimatorDampTime, deltaTime);
                StateMachine.ChangeState((byte) BossState.Chase);
            }
            
            // Idle Wait Time
            if (Time.time - stateEnterTime < IdleDuration) return;
            
            // Can Attack
            if (IsPlayerInRange()) // and Player Not Dead
            {
                SetAnimatorFloat(turnSpeedHash, 0f, AnimatorDampTime, deltaTime);
                StateMachine.ChangeState((byte) BossState.Attack);
            }
        }

        public override void Exit() { }

        private float CalcTurnSpeed()
        {
            Quaternion deltaRotation = StateMachine.transform.rotation * Quaternion.Inverse(lastRotation);
            
            deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 _);
            
            if (angleInDegrees > 180f)
                angleInDegrees = 360f - angleInDegrees;

            currentTurnSpeed = angleInDegrees / Time.deltaTime;

            lastRotation = StateMachine.transform.rotation;

            return Mathf.Clamp01(currentTurnSpeed / 120f);
        }
        
    }

    public class BossWakeState : BossBaseState
    {
        private readonly int wakeTriggerHash = Animator.StringToHash("Wake");
        private float wakeAnimationLength = 1.29f;
        private float stateEnterTime;
        
        public BossWakeState(BossStateMachine stateMachine) : base(stateMachine)
        {
            // Get Wake Animation Length
            if (!StateMachine.IsServer) return;
            
            AnimationClip[] clips = Core.NetworkAnimator.Animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name == "Wake")
                {
                    wakeAnimationLength = clip.length;
                    break;
                }
            }
        }

        public override void Enter()
        {
            if (!StateMachine.IsServer) return; // Only on Server
            SetAnimatorTrigger(wakeTriggerHash);
            stateEnterTime = Time.time;
        }

        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsServer) return; // Only on Server

            if (Time.time - stateEnterTime > wakeAnimationLength)   // Finished Animation
            {
                StateMachine.ChangeState((byte) BossState.Idle);
            }
        }
        
        public override void Exit() { }
    }

    public class BossChaseState : BossBaseState
    {
        private readonly int speedHash = Animator.StringToHash("Speed");
        private const float AnimatorDampTime = 0.1f;
        public BossChaseState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsServer) return; // Only on Server
            
            Debug.Log("[Boss] Chase State");
            Core.NavMeshAgent.speed = StateMachine.MovementSpeed;
        }
        
        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsServer) return; // Only on Server

            if (IsPlayerInRange())
            {
                StateMachine.ChangeState((byte) BossState.Idle);
                return;
            }

            TurnToPlayer();
            MoveToPlayer(deltaTime);
            
            float speedPercent = Core.NavMeshAgent.velocity.magnitude / StateMachine.MovementSpeed;
            SetAnimatorFloat(speedHash, speedPercent, AnimatorDampTime, deltaTime);
        }

        public override void Exit()
        {
            if (!StateMachine.IsServer) return; // Only on Server

            Core.NavMeshAgent.ResetPath(); 
            Core.NavMeshAgent.velocity = Vector3.zero;
            
            // Adjust Transform by Force
            Core.NetworkTransform.Teleport(StateMachine.transform.position, StateMachine.transform.rotation, StateMachine.transform.localScale);
        }

        private void MoveToPlayer(float deltaTime)
        {
            if (!StateMachine.IsServer) return; // Only on Server
            
            if (Core.BossCharacter.GetTargetPlayer(out Transform targetPlayer))
            {
                Core.NavMeshAgent.destination = targetPlayer.transform.position;
            }
        }
    }

    public class BossAttackState : BossBaseState
    {
        public BossAttackState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsServer) return; // Only on Server
            Debug.Log("[Boss] Attack State");
            Core.BossSkillController.ActivateSkill(); // TODO: -> ActivateSkill
        }
        
        public override void Tick(float deltaTime)
        {
            if (!StateMachine.IsServer) return; // Only on Server
            
            if (!Core.BossSkillController.isSkillActive.Value)
            {
                StateMachine.ChangeState((byte) BossState.Idle);
            }
        }

        public override void Exit() { }
    }

    public class BossDeathState : BossBaseState
    {
        private readonly int deathHash = Animator.StringToHash("Death");
        public BossDeathState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            if (!StateMachine.IsServer) return; // Only on Server
            Debug.Log("[Boss] Death State");
            SetAnimatorTrigger(deathHash);
        }

        public override void Tick(float deltaTime) { }

        public override void Exit() { }
    }

    public class BossSleepState : BossBaseState
    {
        public BossSleepState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter() { }

        public override void Tick(float deltaTime) { }

        public override void Exit() { }
    }
    
}
