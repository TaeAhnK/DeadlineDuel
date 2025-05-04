using Boss.Skills;
using Stats.Boss;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace Boss
{
    public class BossCore : NetworkBehaviour
    {
        [field: Header("Dependencies")]
        [field: SerializeField] public BossCharacter BossCharacter { get; set; }
        [field: SerializeField] public BossStats BossStats { get; private set; }
        [field: SerializeField] public BossStateMachine BossStateMachine { get; private set; }
        [field: SerializeField] public BossSkillController BossSkillController { get; private set; }
        [field: SerializeField] public NetworkAnimator NetworkAnimator { get; private set; }
        [field: SerializeField] public NetworkTransform NetworkTransform { get; private set; }
        [field: SerializeField] public NavMeshAgent NavMeshAgent { get; private set; }

        private void Init() // Just in case
        {
            if (BossCharacter == null) BossCharacter = GetComponent<BossCharacter>();
            if (BossStats == null) BossStats = GetComponent<BossStats>();
            if (BossStateMachine == null) BossStateMachine = GetComponent<BossStateMachine>();
            if (BossSkillController == null) BossSkillController = GetComponent<BossSkillController>();
            if (NetworkAnimator == null) NetworkAnimator = GetComponent<NetworkAnimator>();
            if (NetworkTransform == null) NetworkTransform = GetComponent<NetworkTransform>();
            if (NavMeshAgent == null) NavMeshAgent = GetComponent<NavMeshAgent>();
        }
    }
}