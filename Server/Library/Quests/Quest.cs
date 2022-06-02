using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VoxelTanksServer.Library.Quests;

public struct Quest {
    [JsonConverter(typeof(StringEnumConverter))]
    public QuestType Type;
    [JsonIgnore]
    public bool Completed => Progress >= Require;
    public string Description { get; set; }

    public int Require;
    public Reward Reward;
    public int Progress;

    public override string ToString() {
        return $"Type: {Type.ToString()} Count: {Require} Reward: {Reward.ToString()}, Progress: {Progress}";
    }
}