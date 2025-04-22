using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "Game/Character Stats")]
public class CharacterStatsData : ScriptableObject
{
    [Header("캐릭터 기본 정보")]
    public string characterName;
    public CharacterType characterType; // 검사, 마법사, 궁수 등
    
    [Header("기본 스탯")]
    public float maxHealth = 100f;
    public float attackPower = 10f;
    public float defense = 5f;
    public float moveSpeed = 5f;
}

public enum CharacterType
{
    Sword, // 검사
    Mage,  // 마법사
    Archer // 궁수
}