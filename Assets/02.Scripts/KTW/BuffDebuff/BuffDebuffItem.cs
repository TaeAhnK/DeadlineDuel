using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BuffTypeEnum{
    Attack,
    Defense,
    Cooltime,
    MoveSpeed,
    AttackSpeed,
}

public enum BuffTargetEnum {
    Player,
    Enemy,
    Boss
}

[RequireComponent(typeof(Button))]
public class BuffDebuffItem : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;

    [Header("Buff Data")]
    public string buffName;
    public int buffCost = 1;
    public BuffTargetEnum target;
    public BuffTypeEnum type;
    public float value = 10;

    private UI_BuffDebuff uiController;

    public void Init(BuffData data, UI_BuffDebuff ui) {
        buffName = data.name;
        buffCost = data.cost;
        target = (BuffTargetEnum)System.Enum.Parse(typeof(BuffTargetEnum), data.target);
        type = (BuffTypeEnum)System.Enum.Parse(typeof(BuffTypeEnum), data.type);
        value = data.value;

        uiController = ui;
        gameObject.GetComponent<Button>().onClick.AddListener(OnClickBuffDebuffItem);
        SetUIText();
    }

    private void SetUIText() {
        nameText.text = buffName;
        costText.text = buffCost.ToString();
    }

    public void OnClickBuffDebuffItem() {
        uiController.SelectItem(this);        
    }
}
