using System;
using StateMachine;
using UnityEngine;

namespace Boss
{
    public abstract class BossBaseState : State
    {
        protected BossStateMachine StateMachine;
        protected BossCore Core;

        public BossBaseState(BossStateMachine stateMachine)
        {
            this.StateMachine = stateMachine;
            this.Core = stateMachine.BossCore;
        }

        protected void Turn(Vector3 lookPos)
        {
            if (!StateMachine.IsServer) return; // Only On Server
            
            lookPos.y = 0;
            // Current Object Rotation
            Quaternion currentRotation = StateMachine.transform.rotation;
    
            // Target Rotation
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
    
            // Rotation Speed
            float rotationSpeed = 120.0f;
            
            StateMachine.transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        protected void TurnToPlayer()
        {
            if (!StateMachine.IsServer) return; // Only On Server
            
            if (Core.BossCharacter.GetTargetPlayer(out Transform targetPlayer))
            {
                Turn(targetPlayer.position - StateMachine.transform.position);                
            }
        }
        
        protected bool IsPlayerInRange() // Use with IsServer Check and TargetPlayer != null Check
        {
            if (Core.BossCharacter.GetTargetPlayer(out Transform targetPlayer))
            {
                float distSqr = (targetPlayer.position - StateMachine.gameObject.transform.position).sqrMagnitude;
                return distSqr <= StateMachine.PlayerDetectRange * StateMachine.PlayerDetectRange;
            }
            
            return false;
        }
        
        protected void SetAnimatorFloat(int hash, float value, float animationDampTime, float deltaTime)
        {
            if (!StateMachine.IsServer) return; // On Only On Server

            if (Math.Abs(Core.NetworkAnimator.Animator.GetFloat(hash) - value) > 0.1f)
            {
                Core.NetworkAnimator.Animator.SetFloat(hash, value, animationDampTime, deltaTime);    
            }
        }

        protected void SetAnimatorTrigger(int hash)
        {
            if (!StateMachine.IsServer) return; // On Only On Server
            
            Core.NetworkAnimator.SetTrigger(hash);
        }
    }
}