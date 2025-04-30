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

        [SerializeField] private List<BossSkill> skills =  new List<BossSkill>();
        [SerializeField] private List<BossSkill> skillsPrefab =  new List<BossSkill>();
        private BossSkill currentSkill;

        private void Awake()
        {
            skills.Add(Instantiate(skillsPrefab[0], transform));
        }

        private BossSkill SelectSkill()
        {
            return skills[0];
        }
        
        [ServerRpc]
        public void ActivateSkillServerRpc()
        {
            if (!IsServer) return; // Only on Host
            
            currentSkill = SelectSkill();
            IsSkillActive.Value = true;
            networkAnimator.SetTrigger(currentSkill.BossSkillHash); // Play Skill Animation
        }
        
        private void OnPlayIndicator() // Animation Event
        {
            PlayIndicatorServerRpc();
        }
        
        [ServerRpc]
        private void PlayIndicatorServerRpc()
        {
            if (!IsServer) return;
            currentSkill.PlayIndicatorServerRpc(transform.position, bossStateMachine.Player.transform.position);
        }

        private void OnPlaySkill() // Animation Event
        {
            PlaySkillServerRpc();
        }

        [ServerRpc]
        private void PlaySkillServerRpc()
        {
            if (!IsServer) return;
            PlaySkillClientRpc();
            currentSkill.PlayColliderServerRpc();
        }

        [ClientRpc]
        private void PlaySkillClientRpc()
        {
            currentSkill.PlayEffectClientRpc();
        }
        
        private void OnAnimationEnd() // Animation Event
        {
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