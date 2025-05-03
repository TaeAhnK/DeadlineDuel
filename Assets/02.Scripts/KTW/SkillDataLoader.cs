using UnityEngine;
using System.Reflection;

[System.Serializable]
public class CharacterSkillData {
    public string[] skillNames;
    public string[] skillDescriptions;
}

[System.Serializable]
public class SkillDataWrapper {
    public CharacterSkillData Cosmo;
    public CharacterSkillData Ronald;
}

public class SkillDataLoader : MonoBehaviour {
    public static SkillDataLoader Instance;

    [Header("JSON Assign")]
    [SerializeField] private TextAsset jsonFile;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    public CharacterSkillData LoadSkillData(string characterName) {
        if (jsonFile == null) {
            Debug.LogError("JSON is not Assigned");
            return null;
        }

        SkillDataWrapper wrapper = JsonUtility.FromJson<SkillDataWrapper>(jsonFile.text);

        FieldInfo field = typeof(SkillDataWrapper).GetField(characterName);
        if (field != null) {
            return (CharacterSkillData)field.GetValue(wrapper);
        }
        else {
            Debug.LogError($"There is no {characterName}'s Data");
            return null;
        }
    }
}
