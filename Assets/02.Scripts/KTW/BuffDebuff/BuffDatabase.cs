using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuffData {
    public string name;
    public int cost;
    public string target;
    public string type;
    public float value;
}

[System.Serializable]
public class BuffDatabase
{
    public List<BuffData> playerBuffs = new List<BuffData>();
    public List<BuffData> enemyBuffs = new List<BuffData>();
    public List<BuffData> bossBuffs = new List<BuffData>();
}
