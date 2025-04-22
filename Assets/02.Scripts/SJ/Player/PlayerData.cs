/// <summary>
/// 플레이어 데이터 컨테이너 - 다른 씬으로 전달 용도
/// </summary>
[System.Serializable]
public class PlayerData
{
    public string PlayerId;
    public string Nickname;
    public int MMR;

    public PlayerData(string playerId, string nickname, int mmr)
    {
        PlayerId = playerId;
        Nickname = nickname;
        MMR = mmr;
    }
}