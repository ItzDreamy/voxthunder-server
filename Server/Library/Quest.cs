using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VoxelTanksServer.Library;

public struct Quest
{
    [JsonConverter(typeof(StringEnumConverter))]
    public QuestType Type;
    public int Count;
    public Reward Reward;

    public override string ToString()
    {
        return $"Type: {Type.ToString()} Count: {Count} Reward: {Reward.ToString()}";
    }
}