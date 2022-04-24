namespace VoxelTanksServer.DB
{
    public struct PlayerStats
    {
        public int Battles;
        public int Damage;
        public int Kills;
        public int Wins;
        public int Loses;
        public float WinRate;
        public int AvgDamage;
        public int AvgKills;
        public int Balance;

        public override string ToString()
        {
            return $"Battles: {Battles} Damage: {Damage} Kills: {Kills} Wins: {Wins} Loses: {Loses} WinRate: {WinRate} AvgDamage: {AvgDamage} AvgKills: {AvgKills} Balance: {Balance}";
        }

        public void Deconstruct(out int battles, out int damage, out int kills, out int wins, out int loses, out float winRate, out int avgDamage, out int avgKills, out int balance)
        {
            battles = Battles;
            damage = Damage;
            kills = Kills;
            wins = Wins;
            loses = Loses;
            winRate = WinRate;
            avgDamage = AvgDamage;
            avgKills = AvgKills;
            balance = Balance;
        }
    }
}