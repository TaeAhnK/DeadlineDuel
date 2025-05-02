using UnityEngine;

namespace Stats.Boss
{
    [CreateAssetMenu(fileName = "BossInitStatData", menuName = "BossInitStatData")]
    public class BossInitStatData : ScriptableObject
    {
        [SerializeField] public float MaxHealth;
        [SerializeField] public float Atk;
        [SerializeField] public float Def;
        [SerializeField] public float AtkSpeed;

        [SerializeField] public float Speed;
        

    }
}