using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VoxelTanksServer.Library;

[JsonConverter(typeof(StringEnumConverter))]
public enum QuestType
{
    Wins,
    Kills,
    Damage,
}