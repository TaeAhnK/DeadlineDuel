using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class BuffDebuff : MonoBehaviour
{
    public string buffName;
    public BuffTypeEnum buffType;
    public float value;

    private Object_Base _targetObjectBase;    // �÷��̾� �� ������ ����� ���� ��ũ��Ʈ
    private BuffableEntity _targetBuffable;    // 

    private void Start() {
        _targetObjectBase = GetComponent<Object_Base>();
        _targetBuffable = GetComponent<BuffableEntity>();

        if (_targetObjectBase != null) {
            _targetObjectBase.ApplyBuffDebuff(buffType, value);
        }
        else {
            _targetBuffable.ApplyBuffDebuff(buffType, value);
        }
    }

    private void OnDestroy() {
        if (_targetObjectBase != null) {
            _targetObjectBase.ApplyBuffDebuff(buffType, -value);
        }

        // ����
        else {
            _targetBuffable.ApplyBuffDebuff(buffType, -value);
        }
    }
}
