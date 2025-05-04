using Unity.Netcode;
using UnityEngine;

public class PlayerCustomizer : NetworkBehaviour
{
    [SerializeField] private GameObject[] _characterModels;
    
    // 네트워크로 동기화되는 캐릭터 인덱스
    private NetworkVariable<int> _characterIndex = new NetworkVariable<int>(0);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 네트워크 변수 변경 콜백 등록
        _characterIndex.OnValueChanged += OnCharacterIndexChanged;
        
        // 현재 값으로 캐릭터 모델 설정
        UpdateCharacterModel(_characterIndex.Value);
    }
    
    // 서버에서 호출 - 캐릭터 모델 설정
    public void SetCharacterModel(int index)
    {
        if (!IsServer)
            return;
            
        _characterIndex.Value = Mathf.Clamp(index, 0, _characterModels.Length - 1);
    }
    
    // 클라이언트에서 실행 - 캐릭터 인덱스 변경 시 호출
    private void OnCharacterIndexChanged(int oldIndex, int newIndex)
    {
        UpdateCharacterModel(newIndex);
    }
    
    // 캐릭터 모델 업데이트
    private void UpdateCharacterModel(int index)
    {
        // 모든 모델 비활성화
        foreach (GameObject model in _characterModels)
        {
            model.SetActive(false);
        }
        
        // 선택한 모델 활성화
        if (index >= 0 && index < _characterModels.Length)
        {
            _characterModels[index].SetActive(true);
        }
    }
}
