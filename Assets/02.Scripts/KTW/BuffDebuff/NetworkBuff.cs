using Unity.Netcode;

public struct NetworkBuff :
    INetworkSerializable,
    System.IEquatable<NetworkBuff> {
    public BuffTypeEnum buffType; // 버프 타입 (공격, 방어 등)
    public float value; // 버프 효과 값

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref buffType);
        serializer.SerializeValue(ref value);
    }

    public bool Equals(NetworkBuff other) => (buffType == other.buffType && value == other.value);
}
