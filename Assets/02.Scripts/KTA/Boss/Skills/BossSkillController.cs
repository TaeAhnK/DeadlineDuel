using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Boss.Skills
{
    public class BossSkillController : NetworkBehaviour
    {
        [SerializeField] private NetworkAnimator networkAnimator;
        [SerializeField] private BossStateMachine bossStateMachine;
        public NetworkVariable<bool> IsSkillActive = new (writePerm: NetworkVariableWritePermission.Server);

        [SerializeField] private List<BossSkill> skills =  new List<BossSkill>(); // TODO: Will Erase
        [SerializeField] private List<BossSkill> skillsPrefab =  new List<BossSkill>();
        private byte currentSkillIndex;
        private BossSkill currentSkill;

        private void Awake()
        {

        }

        private byte SelectSkill()
        {
            return 0;
        }
        
        [ServerRpc]
        public void ActivateSkillServerRpc()
        {
            if (!IsServer) return; // Only on Host
            
            currentSkillIndex = SelectSkill();
            currentSkill = skillsPrefab[currentSkillIndex];
            ActivateSkillClientRpc(currentSkillIndex);
            IsSkillActive.Value = true;
            networkAnimator.SetTrigger(currentSkill.BossSkillHash); // Play Skill Animation
        }

        [ClientRpc]
        private void ActivateSkillClientRpc(byte skillIndex)
        {
            currentSkill = skillsPrefab[skillIndex];
        }
        
        private void OnPlayIndicator() // Animation Event
        {
            if (bossStateMachine.BossCharacter.GetTargetPlayer(out Transform targetPlayer))
            {
                currentSkill.PlayIndicatorClient(transform.position, targetPlayer.transform.position);
            }
            else
            {
                Debug.Log("[Boss] [Error] No Indicator due to No Target");
            }
        }

        private void OnPlaySkill() // Animation Event
        {
            if (IsServer)
            {
                PlaySkillServerRpc();   
            }
            if (IsClient)
            {
                PlaySkillClient();   
            }
        }

        [ServerRpc]
        private void PlaySkillServerRpc()
        {
            if (!IsServer) return;
            currentSkill.PlayColliderServerRpc();
        }
        
        private void PlaySkillClient()
        {
            currentSkill.PlayEffectClient();
        }
        
        private void OnAnimationEnd() // Animation Event
        {
            if (!IsServer) return;
            AnimationEndServerRpc();
        }

        [ServerRpc]
        private void AnimationEndServerRpc()
        {
            if (!IsServer) return;
            IsSkillActive.Value = false;
        }
        
    }
}