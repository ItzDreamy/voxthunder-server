using Newtonsoft.Json;
using Serilog;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Library.Quests;

public class QuestManager {
    private const string QuestsPath = "Library/Quests/quests.json";

    public static List<Quest> AllowedQuests {
        get {
            string json = File.ReadAllText(QuestsPath);
            var quests = JsonConvert.DeserializeObject<List<Quest>>(json);
            if (quests == null) {
                throw new NullReferenceException();
            }

            return quests;
        }
    }

    public static void CheckAndUpdateQuests(Client client) {
        string questsDataPath = Path.Combine("PlayersData", "Quests", $"{client.Data.Username}.json");
        QuestsData questsData;

        if (File.Exists(questsDataPath)) {
            string dataJson = File.ReadAllText(questsDataPath);
            questsData = JsonConvert.DeserializeObject<QuestsData>(dataJson);
            if (questsData.GeneratedDate.Date != DateTime.Today) {
                Log.Information($"New day. Generating new quests for {client.Data.Username}");
                questsData = GenerateQuests();
                UpdateQuests(questsData, questsDataPath);
            }
        }
        else {
            Log.Information($"New player. Generating new quests for {client.Data.Username}");
            questsData = GenerateQuests();
            UpdateQuests(questsData, questsDataPath);
        }

        client.Data.QuestsData = questsData;
    }

    public static QuestsData GenerateQuests() {
        var data = new QuestsData {
            GeneratedDate = DateTime.Today,
            Quests = new List<Quest>()
        };

        var random = new Random();
        var questsCopy = new List<Quest>(AllowedQuests);
        
        for (var i = 0; i < AllowedQuests.Count && data.Quests.Count < 3; i++)
        {
            var pickIndex = random.Next(questsCopy.Count);
            var randomQuest = questsCopy[pickIndex];
            data.Quests.Add(randomQuest);
            questsCopy.RemoveAt(pickIndex);
        }

        return data;
    }

    public static void UpdateQuests(QuestsData questsData, string questsDataPath) {
        if (File.Exists(questsDataPath)) {
            File.Delete(questsDataPath);
        }
        var dataJson = JsonConvert.SerializeObject(questsData);

        using (StreamWriter writer = File.AppendText(questsDataPath)) {
            writer.Write(dataJson);
        }
    }
}