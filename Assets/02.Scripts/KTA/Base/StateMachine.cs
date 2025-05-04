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
        
        private State currentState;
        protected Dictionary<byte, State> StateDict = new Dictionary<byte, State>();
        
        protected virtual void Update()
        {
            if (!IsServer) return; // Only Update by Sever
            
            currentState?.Tick(Time.deltaTime);
        }
        
        public void ChangeState(byte newStateID)
        {
            if (!IsServer) return; // Only Change State on Server

            if (StateDict.TryGetValue(newStateID, out State newState))
            {
                currentState?.Exit();
                currentState = newState;
                currentStateID.Value = newStateID;
                currentState?.Enter();
            }
        }

        public override void OnNetworkSpawn()
        {
            currentStateID.OnValueChanged += (prev, next) =>  // Sync Client State on currentStateID Update
            {
                if (!IsServer && StateDict.TryGetValue(next, out State newState))
                {
                    currentState?.Exit();
                    currentState = newState;
                    currentState?.Enter();
                }
            };
        }
    }
}