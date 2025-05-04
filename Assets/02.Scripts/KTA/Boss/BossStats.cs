using System;
using Unity.Netcode;
using UnityEngine;

namespace Stats.Boss
{
    public class BossStats : NetworkBehaviour, IDamageable // Currently Does Nothing
    {
        [field: Header("Dependencies")]
        [SerializeField] private BossInitStatData initStatData;

        [field: Header("Stats")]
        public NetworkVariable<float> MaxHealth = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Atk = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Def = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> AtkSpeed = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Speed = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<float> CurrentHealth = new(writePerm: NetworkVariableWritePermission.Server);

        public Action OnDeath;
        
        public bool IsDead => CurrentHealth.Value <= 0f;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                MaxHealth.Value = initStatData.MaxHealth;
                Atk.Value = initStatData.Atk;
                Def.Value = initStatData.Def;
                AtkSpeed.Value = initStatData.AtkSpeed;
                Speed.Value = initStatData.Speed;
                CurrentHealth.Value = initStatData.MaxHealth;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            if (IsDead) return;
            
            // TODO : Add Def Calculation
            CurrentHealth.Value = Math.Max(0, CurrentHealth.Value - damage);
            if (IsDead)
            {
                OnDeath?.Invoke();
            }
        }

        // TODO : Erase
        public void KillBossTest()
        {
            if (!IsServer) return;
            if (IsDead) return;
            
            CurrentHealth.Value = Math.Max(0, CurrentHealth.Value - 50000);
            if (IsDead)
            {
                OnDeath?.Invoke();
            }   
        }
        
    }
}