using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library.Quests;

namespace VoxelTanksServer.DB;

public struct PlayerData {
    public string Username;
    public Rank Rank;
    public int Battles;
    public int Damage;
    public int Kills;
    public int Wins;
    public int Loses;
    public int Draws;
    public float WinRate;
    public int AvgDamage;
    public int AvgKills;
    public int AvgExperience;
    public int Balance;
    public int Experience;
    public List<Tank> OwnedTanks;
    public List<Quest> Quests;

    public override string ToString() {
        return
            $"Rank: {Rank.Name} Battles: {Battles} Damage: {Damage} Kills: {Kills} Wins: {Wins} Loses: {Loses} WinRate: {WinRate} AvgDamage: {AvgDamage} AvgExperience: {AvgExperience} AvgKills: {AvgKills} Balance: {Balance} Experience: {Experience}";
    }

    public void Deconstruct(out string username, out int battles, out int damage, out int kills, out int wins,
        out int loses, out int draws,
        out float winRate, out int avgDamage, out int avgKills, out int avgExperience, out int balance,
        out int experience, out Rank rank) {
        username = Username;
        battles = Battles;
        damage = Damage;
        kills = Kills;
        wins = Wins;
        loses = Loses;
        draws = Draws;
        winRate = WinRate;
        avgDamage = AvgDamage;
        avgExperience = AvgExperience;
        avgKills = AvgKills;
        balance = Balance;
        experience = Experience;
        rank = Rank;
    }
}