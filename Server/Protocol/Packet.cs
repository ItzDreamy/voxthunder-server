using System.Drawing;
using System.Numerics;
using System.Text;
using VoxelTanksServer.GameCore;

namespace VoxelTanksServer.Protocol;

public enum ServerPackets
{
    Welcome = 1,
    SpawnPlayer,
    RotateTurret,
    LoginResult,
    PlayerDisconnected,
    InstantiateObject,
    TakeDamage,
    PlayerDead,
    LoadGame,
    AbleToReconnect,
    ShowDamage,
    ShowKillFeed,
    PlayerReconnected,
    ShowPlayersCountInRoom,
    PlayersStats,
    EndGame,
    InitializeTankStats,
    SwitchTank,
    LeaveToLobby,
    TakeDamageOtherPlayer,
    Timer,
    UnlockPlayers,
    SendMovement,
    PlayerData,
    AuthId,
    SignOut,
    SendMessage,
    OpenProfile,
    BoughtTankInfo
}

public enum ServerApiPackets
{
    SendPlayersCount = 1,
    Ping,
    SendServerState,
    Welcome
}

public enum ClientPackets
{
    WelcomeReceived = 1,
    RotateTurret,
    ReadyToSpawn,
    SelectTank,
    TryLogin,
    TakeDamage,
    InstantiateObject,
    ShootBullet,
    JoinRoom,
    LeaveRoom,
    CheckAbleToReconnect,
    ReconnectRequest,
    CancelReconnect,
    RequestPlayersStats,
    LeaveToLobby,
    BuyTank,
    SendMovement,
    RequestData,
    AuthById,
    SignOut,
    ReceiveMessage,
    OpenProfile,
    GetLastSelectedTank
}

public enum ClientApiPackets
{
    GetPlayersCount = 1,
    GetServerState
}

public class Packet : IDisposable
{
    private List<byte> _buffer;
    private byte[] _readableBuffer;
    private int _readPos;

    public Packet()
    {
        _buffer = new List<byte>();
        _readPos = 0;
    }

    public Packet(int id)
    {
        _buffer = new List<byte>();
        _readPos = 0;

        Write(id);
    }

    public Packet(byte[] data)
    {
        _buffer = new List<byte>();
        _readPos = 0;

        SetBytes(data);
    }

    #region Functions

    public void SetBytes(byte[] data)
    {
        Write(data);
        _readableBuffer = _buffer.ToArray();
    }

    public void WriteLength()
    {
        _buffer.InsertRange(0,
            BitConverter.GetBytes(_buffer.Count));
    }

    public void InsertInt(int value)
    {
        _buffer.InsertRange(0, BitConverter.GetBytes(value));
    }

    public byte[] ToArray()
    {
        _readableBuffer = _buffer.ToArray();
        return _readableBuffer;
    }

    public int Length()
    {
        return _buffer.Count;
    }

    public int UnreadLength()
    {
        return Length() - _readPos;
    }

    public void Reset(bool shouldReset = true)
    {
        if (shouldReset)
        {
            _buffer.Clear();
            _readableBuffer = null;
            _readPos = 0;
        }
        else
        {
            _readPos -= 4;
        }
    }

    #endregion

    #region Write Data

    public void Write(byte value)
    {
        _buffer.Add(value);
    }

    public void Write(byte[] value)
    {
        _buffer.AddRange(value);
    }

    public void Write(short value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(int value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(long value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(float value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(bool value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(string? value)
    {
        Write(value.Length);
        _buffer.AddRange(Encoding.GetEncoding(1251).GetBytes(value));
    }

    public void Write(Color color)
    {
        Write(color.R);
        Write(color.G);
        Write(color.B);
        Write(color.A);
    }

    public void Write(Vector3 value)
    {
        Write((float) Math.Round(value.X, 2, MidpointRounding.AwayFromZero));
        Write((float) Math.Round(value.Y, 2, MidpointRounding.AwayFromZero));
        Write((float) Math.Round(value.Z, 2, MidpointRounding.AwayFromZero));
    }

    public void Write(Quaternion value)
    {
        Write((float) Math.Round(value.X, 2, MidpointRounding.AwayFromZero));
        Write((float) Math.Round(value.Y, 2, MidpointRounding.AwayFromZero));
        Write((float) Math.Round(value.Z, 2, MidpointRounding.AwayFromZero));
        Write((float) Math.Round(value.W, 2, MidpointRounding.AwayFromZero));
    }

    public void Write(List<Player> players)
    {
        Write(players.Count);

        foreach (var player in players)
        {
            Write(player.Id);
            Write(player.Kills);
            Write(player.TotalDamage);
            Write(player.SelectedTank.Name);
            Write(player.IsAlive ? "Alive" : "Dead");
        }
    }

    public void Write(DateTime timestamp)
    {
        Write(timestamp.Year);
        Write(timestamp.Month);
        Write(timestamp.Day);
        Write(timestamp.Hour);
        Write(timestamp.Minute);
        Write(timestamp.Second);
        Write(timestamp.Millisecond);
    }

    public void Write(MovementData movementData)
    {
        Write(movementData.Position);
        Write(movementData.Rotation);
        Write(movementData.Velocity);
        Write(movementData.AngularVelocity);
    }

    #endregion

    #region Read Data

    public byte ReadByte(bool moveReadPos = true)
    {
        if (_buffer.Count > _readPos)
        {
            byte value = _readableBuffer[_readPos];
            if (moveReadPos)
            {
                _readPos += 1;
            }

            return value;
        }

        throw new Exception("Could not read value of type 'byte'!");
    }

    public byte[] ReadBytes(int length, bool moveReadPos = true)
    {
        if (_buffer.Count > _readPos)
        {
            byte[] value =
                _buffer.GetRange(_readPos, length)
                    .ToArray();
            if (moveReadPos)
            {
                _readPos += length;
            }

            return value;
        }

        throw new Exception("Could not read value of type 'byte[]'!");
    }

    public short ReadShort(bool moveReadPos = true)
    {
        if (_buffer.Count > _readPos)
        {
            short value = BitConverter.ToInt16(_readableBuffer, _readPos);
            if (moveReadPos)
            {
                _readPos += 2;
            }

            return value;
        }

        throw new Exception("Could not read value of type 'short'!");
    }

    public int ReadInt(bool moveReadPos = true)
    {
        if (_buffer.Count > _readPos)
        {
            int value = BitConverter.ToInt32(_readableBuffer, _readPos);
            if (moveReadPos)
            {
                _readPos += 4;
            }

            return value;
        }

        throw new Exception("Could not read value of type 'int'!");
    }

    public long ReadLong(bool moveReadPos = true)
    {
        if (_buffer.Count > _readPos)
        {
            long value = BitConverter.ToInt64(_readableBuffer, _readPos);
            if (moveReadPos)
            {
                _readPos += 8;
            }

            return value;
        }

        throw new Exception("Could not read value of type 'long'!");
    }

    public float ReadFloat(bool moveReadPos = true)
    {
        if (_buffer.Count > _readPos)
        {
            float value = BitConverter.ToSingle(_readableBuffer, _readPos);
            if (moveReadPos)
            {
                _readPos += 4;
            }

            return value;
        }

        throw new Exception("Could not read value of type 'float'!");
    }

    public MovementData ReadMovement(bool moveReadPos = true)
    {
        MovementData data = default(MovementData);
        data.Position = ReadVector3(moveReadPos);
        data.Rotation = ReadQuaternion(moveReadPos);
        data.Velocity = ReadVector3(moveReadPos);
        data.AngularVelocity = ReadVector3(moveReadPos);
        return data;
    }

    public bool ReadBool(bool moveReadPos = true)
    {
        if (_buffer.Count > _readPos)
        {
            bool value = BitConverter.ToBoolean(_readableBuffer, _readPos);
            if (moveReadPos)
            {
                _readPos += 1;
            }

            return value;
        }

        throw new Exception("Could not read value of type 'bool'!");
    }

    public string? ReadString(bool moveReadPos = true)
    {
        try
        {
            int length = ReadInt();
            string? value =
                Encoding.GetEncoding(1251)
                    .GetString(_readableBuffer, _readPos, length);
            if (moveReadPos && value.Length > 0)
            {
                _readPos += length;
            }

            return value;
        }
        catch
        {
            throw new Exception("Could not read value of type 'string'!");
        }
    }


    public Vector3 ReadVector3(bool moveReadPos = true)
    {
        return new Vector3(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
    }

    public Quaternion ReadQuaternion(bool moveReadPos = true)
    {
        return new Quaternion(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos),
            ReadFloat(moveReadPos));
    }

    #endregion

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _buffer = null;
                _readableBuffer = null;
                _readPos = 0;
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}