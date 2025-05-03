using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffDebuff : MonoBehaviour
{
    public string buffName;
    public BuffTypeEnum buffType;
    public float value;

    private Object_Base target;    // TODO �÷��̾� �� ������ ����� ���� ��ũ��Ʈ

    private void Start() {
        target = GetComponent<Object_Base>();
        if (target == null) {
            Debug.LogError($"BuffDebuf | {name} has no Object_Base");
            Destroy(this);
            return;
        }
        ApplyEffect(true);
    }

    private void ApplyEffect(bool apply) {
        if (apply) {
            target.ApplyBuffDebuff(this);
        }
        else {
            target.RemoveBuffDebuff(this);
        }
        
    }

    private void OnDestroy() {
        ApplyEffect(false);
    }

}
