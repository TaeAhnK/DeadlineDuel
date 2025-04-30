using System;
using Unity.Netcode;
using UnityEngine;

namespace Stats.Boss
{
    public class BossStats : NetworkBehaviour, IDamageable
    {
        [SerializeField] private BossInitStatData initStatData;

        public NetworkVariable<float> MaxHealth = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Atk = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Def = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> AtkSpeed = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> CurrentHealth = new(writePerm: NetworkVariableWritePermission.Server);
        
        public bool IsDead => CurrentHealth.Value <= 0f;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                MaxHealth.Value = initStatData.MaxHealth;
                Atk.Value = initStatData.Atk;
                Def.Value = initStatData.Def;
                AtkSpeed.Value = initStatData.AtkSpeed;
                CurrentHealth.Value = initStatData.MaxHealth;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            if (IsDead) return;
            
            CurrentHealth.Value = Math.Max(0, CurrentHealth.Value - damage);
            if (IsDead) DieServerRpc();
        }

        [ServerRpc]
        private void DieServerRpc()
        {
            
        }

        [ClientRpc]
        private void DieClientRpc()
        {
            
        }

    }
}