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
            if (StateMachine.BossCharacter.GetTargetPlayer(out Transform targetPlayer))
            {
                Turn(targetPlayer.position - StateMachine.transform.position);                
            }
        }
        
        protected bool IsPlayerInRange()
        {
            if (StateMachine.BossCharacter.GetTargetPlayer(out Transform targetPlayer))
            {
                float distSqr = (targetPlayer.position - StateMachine.gameObject.transform.position).sqrMagnitude;
                return distSqr <= StateMachine.PlayerDetectRange * StateMachine.PlayerDetectRange;
            }

            return false;
        }
        
        protected void SetAnimatorFloat(int hash, float value, float animationDampTime, float deltaTime)
        {
            if (!StateMachine.IsHost) return; // On Host

            if (Math.Abs(StateMachine.NetworkAnimator.Animator.GetFloat(hash) - value) > 0.1f)
            {
                StateMachine.NetworkAnimator.Animator.SetFloat(hash, value, animationDampTime, deltaTime);    
            }
        }

        protected void SetAnimatorTrigger(int hash)
        {
            if (!StateMachine.IsHost) return; // On Host
            
            StateMachine.NetworkAnimator.SetTrigger(hash);
        }
    }
}