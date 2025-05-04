using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class BuffDebuff : MonoBehaviour
{
    public string buffName;
    public BuffTypeEnum buffType;
    public float value;

    private BuffableEntity _target;    // TODO 플레이어 적 보스에 상속할 상위 스크립트

    private void Start() {
        _target = GetComponent<BuffableEntity>();
        if (_target == null) {
            Debug.LogError($"BuffDebuf | {name} has no BuffableEntity");
            Destroy(this);
            return;
        }

        _target.ApplyBuffDebuffServerRpc(buffType, value);
    }

    private void OnDestroy() {
        if (_target != null) {
            _target.RemoveBuffDebuffServerRpc(buffType, value);
        }
    }
}
