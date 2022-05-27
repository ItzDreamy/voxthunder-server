using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library.LevelingSystem;
using VoxelTanksServer.Library.Quests;

namespace VoxelTanksServer.Database.Models;

public class PlayerData : ICloneable {
    [Key] public int Id { get; set; }
    public string Nickname { get; set; }
    public int RankId { get; set; }
    public int Battles { get; set; }
    public float WinRate { get; set; }
    public int AvgDamage { get; set; }
    public int AvgKills { get; set; }
    public int AvgExp { get; set; }
    public int Damage { get; set; }
    public int Kills { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Loses { get; set; }
    public int Balance { get; set; }
    public int Exp { get; set; }
    public int Mamont { get; set; }
    public int Raider { get; set; }
    public int Berserk { get; set; }
    public string SelectedTank { get; set; }

    [NotMapped] public Rank Rank => Leveling.GetRank(RankId);
    [NotMapped] public QuestsData QuestsData;

    public override string ToString() {
        return
            $"Rank: {Rank.Name} Battles: {Battles} Damage: {Damage} Kills: {Kills} Wins: {Wins} Loses: {Loses} WinRate: {WinRate} AvgDamage: {AvgDamage} AvgExperience: {AvgExp} AvgKills: {AvgKills} Balance: {Balance} Experience: {Exp}";
    }

    public object Clone() {
        return MemberwiseClone();
    }

    public void Deconstruct(out string username, out int battles, out int damage, out int kills, out int wins,
        out int loses, out int draws,
        out float winRate, out int avgDamage, out int avgKills, out int avgExperience, out int balance,
        out int experience, out Rank rank) {
        username = Nickname;
        battles = Battles;
        damage = Damage;
        kills = Kills;
        wins = Wins;
        loses = Loses;
        draws = Draws;
        winRate = WinRate;
        avgDamage = AvgDamage;
        avgExperience = AvgExp;
        avgKills = AvgKills;
        balance = Balance;
        experience = Exp;
        rank = Rank;
    }
}