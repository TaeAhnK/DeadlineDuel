using System.Collections;
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
        public NetworkVariable<bool> isSkillActive = new (writePerm: NetworkVariableWritePermission.Server);

        [SerializeField] private List<BossSkill> skillsPrefab =  new List<BossSkill>();
        [SerializeField] private byte currentSkillIndex;
        private BossSkill currentSkill;

        private byte SelectSkill() // TODO : Need to Add Logic
        {
            return (byte) Random.Range(0, skillsPrefab.Count);
        }

        public void ActivateSkill()
        {
            if (!IsServer) return;  // Only on Server

            currentSkillIndex = SelectSkill();
            currentSkill = skillsPrefab[currentSkillIndex];
            SetCurrentSkillClientRpc(currentSkillIndex);
            StartCoroutine(ExecuteSkillSequence());
            StartCoroutine(EndSkillAnimation(currentSkill.SkillAnimationTime));
        }

        [ClientRpc]
        private void SetCurrentSkillClientRpc(byte skillIndex)
        {
            currentSkillIndex = skillIndex;
            currentSkill = skillsPrefab[skillIndex];
        }
        
        private IEnumerator ExecuteSkillSequence()
        {
            // Start Skill
            isSkillActive.Value = true;
            currentSkill.ActivateSkill(gameObject.transform.position, gameObject.transform.position); // TODO : Get TargetPlayer
            networkAnimator.SetTrigger(currentSkill.BossSkillHash); // Play Skill Animation
            
            // Play Indicator
            yield return new WaitForSeconds(currentSkill.IndicatorTime);
            currentSkill.ActivateIndicatorClientRpc();
            
            // Play Effect and Damage Collider
            yield return new WaitForSeconds(currentSkill.EffectTime);
            currentSkill.ActivateSkillEffectClientRpc();
            currentSkill.ActivateDamageCollider(bossStateMachine.BossStats.Atk.Value);
        }

        private IEnumerator EndSkillAnimation(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            isSkillActive.Value = false;
        }
    }
}