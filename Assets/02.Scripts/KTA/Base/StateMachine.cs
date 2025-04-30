using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace StateMachine
{
    public abstract class StateMachine : NetworkBehaviour
    {
        // Host State : State -> Byte
        [SerializeField] private NetworkVariable<byte> currentStateID =
            new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
        
        protected State CurrentState;
        protected Dictionary<byte, State> StateDict = new Dictionary<byte, State>();
        
        protected virtual void Update()
        {
            if (!IsHost) return; // Only Update by host
            
            CurrentState?.Tick(Time.deltaTime);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestStateChangeServerRpc(byte newStateID, ServerRpcParams param = default)
        {
            if (!IsHost) return; // Only Change State on Host

            if (StateDict.TryGetValue(newStateID, out State newState))
            {
                CurrentState?.Exit();
                CurrentState = newState;
                currentStateID.Value = newStateID;
                CurrentState?.Enter();
            }
        }

        public override void OnNetworkSpawn()
        {
            currentStateID.OnValueChanged += (prev, next) =>
            {
                if (!IsHost && StateDict.TryGetValue(next, out State newState)) // Client
                {
                    CurrentState?.Exit();
                    CurrentState = newState;
                    CurrentState?.Enter();
                }
            };
        }
    }
}