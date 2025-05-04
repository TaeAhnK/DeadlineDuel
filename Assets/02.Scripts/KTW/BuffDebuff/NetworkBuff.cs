using Unity.Netcode;

public struct NetworkBuff :
    INetworkSerializable,
    System.IEquatable<NetworkBuff> {
    public BuffTypeEnum buffType; // ���� Ÿ�� (����, ��� ��)
    public float value; // ���� ȿ�� ��

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref buffType);
        serializer.SerializeValue(ref value);
    }

    public bool Equals(NetworkBuff other) => (buffType == other.buffType && value == other.value);
}
