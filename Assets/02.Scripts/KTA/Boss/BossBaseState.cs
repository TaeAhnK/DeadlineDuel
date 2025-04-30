using System;
using StateMachine;
using UnityEngine;

namespace Boss
{
    public abstract class BossBaseState : State
    {
        protected BossStateMachine StateMachine;

        public BossBaseState(BossStateMachine stateMachine)
        {
            this.StateMachine = stateMachine;
        }

        protected void Turn(Vector3 lookPos)
        {
            if (!StateMachine.IsHost) return;
            
            lookPos.y = 0;
            // 현재 오브젝트의 방향
            Quaternion currentRotation = StateMachine.transform.rotation;
    
            // 목표 방향
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
    
            // 회전 속도 (초당 각도)
            float rotationSpeed = 120.0f;
            
            StateMachine.transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        protected void TurnToPlayer()
        {
            if (StateMachine.Player)
            {
                Turn(StateMachine.Player.transform.position - StateMachine.transform.position);                
            }
        }
        
        protected bool IsPlayerInRange()
        {
            if (!StateMachine.Player) return false;
            float distSqr = (StateMachine.Player.transform.position - StateMachine.transform.position).sqrMagnitude;
            
            return distSqr <= StateMachine.PlayerDetectRange * StateMachine.PlayerDetectRange;
        }
        
        protected void SetAnimatorFloat(int hash, float value, float animationDampTime, float deltaTime)
        {
            if (!StateMachine.IsHost) return; // On Host

            if (Math.Abs(StateMachine.NetworkAnimator.Animator.GetFloat(hash) - value) > 0.1f)
            {
                StateMachine.NetworkAnimator.Animator.SetFloat(hash, value,  animationDampTime, deltaTime);    
            }
        }

        protected void SetAnimatorTrigger(int hash)
        {
            if (!StateMachine.IsHost) return; // On Host
            
            StateMachine.NetworkAnimator.SetTrigger(hash);
        }
    }
}