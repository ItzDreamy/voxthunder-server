﻿using System.Drawing;
using System.Numerics;
using Serilog;

namespace VoxelTanksServer
{
    public class Player
    {
        public readonly int Id;
        public readonly string? Username;
        public string? TankName;
        public readonly Room? ConnectedRoom = null;
        public Team? Team;

        public Vector3 Position;
        public Quaternion Rotation;
        public Quaternion BarrelRotation;
        public Quaternion TurretRotation;
        public int Health;
        public int MaxHealth;
        public float MaxSpeed;
        public float MaxBackSpeed;
        public int Damage;
        public float Cooldown;
        public int TotalDamage;
        public int Kills;
        public bool CanShoot;
        public bool IsAlive;

        public Player(int id, string? username, Vector3 spawnPosition, Quaternion rotation, string? tankName,
            Room? room)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = rotation;
            TankName = tankName;
            IsAlive = true;
            ConnectedRoom = room;

            Tank tank = Server.Tanks.Find(tank => tank.Name == TankName.ToLower());

            if (tank == null)
            {
                Server.Clients[Id].Disconnect("Танк игрока не проинициализирован");
                return;
            }

            MaxHealth = tank.MaxHealth;
            Damage = tank.Damage;
            MaxSpeed = tank.MaxSpeed;
            MaxBackSpeed = tank.MaxBackSpeed;
            Cooldown = tank.Cooldown;

            Health = MaxHealth;

            ConnectedRoom.CachedPlayers.Add(CachePlayer());

            Task.Run(async () =>
            {
                await Task.Delay((int) (Cooldown * 1000));
                CanShoot = true;
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Создает новый экземпляр игрока из кеша.
        /// </summary>
        /// <param name="cachedPlayer">Кэшированый игрок</param>
        /// <param name="id">ID игрока</param>
        public Player(CachedPlayer cachedPlayer, int id)
        {
            Id = id;
            Username = cachedPlayer.Username;
            TankName = cachedPlayer.TankName;
            Team = cachedPlayer.Team;

            Position = cachedPlayer.Position;
            Rotation = cachedPlayer.Rotation;
            BarrelRotation = cachedPlayer.BarrelRotation;
            TurretRotation = cachedPlayer.TurretRotation;
            CanShoot = cachedPlayer.CanShoot;
            IsAlive = cachedPlayer.IsAlive;
            Health = cachedPlayer.Health;
            TotalDamage = cachedPlayer.TotalDamage;
            Kills = cachedPlayer.Kills;
            ConnectedRoom = Server.Clients[Id].ConnectedRoom;

            Tank tank = Server.Tanks.Find(tank => tank.Name == cachedPlayer.TankName.ToLower());
            if(tank != null)
            {
                MaxHealth = tank.MaxHealth;
                Damage = tank.Damage;
                MaxSpeed = tank.MaxSpeed;
                MaxBackSpeed = tank.MaxBackSpeed;
                Cooldown = tank.Cooldown;
            }


            ServerSend.PlayerReconnected(Username, ConnectedRoom);

            ConnectedRoom.CachedPlayers.Remove(cachedPlayer);
            ConnectedRoom.CachedPlayers.Add(CachePlayer());

            if (!CanShoot && IsAlive)
            {
                Task.Run(async () =>
                {
                    await Task.Delay((int) (Cooldown * 1000));
                    CanShoot = true;
                    return Task.CompletedTask;
                });
            }
        }

        public void Move(Vector3 nextPos, Quaternion rotation, Quaternion barrelRotation, float speed, bool isForward)
        {
            //Anti-speedhack
            if (isForward)
            {
                if (speed < MaxSpeed && IsAlive)
                {
                    Position = nextPos;
                }
                else if (speed > MaxSpeed + 2)
                {
                    Server.Clients[Id].Disconnect("Подозревание в спидкахе");
                    return;
                }
            }
            else
            {
                if (speed < MaxBackSpeed && IsAlive)
                {
                    Position = nextPos;
                }
                else if (speed > MaxBackSpeed + 2)
                {
                    Server.Clients[Id].Disconnect("Подозревание в спидхаке");
                    return;
                }
            }

            if (IsAlive)
            {
                Rotation = rotation;
                BarrelRotation = barrelRotation;
            }

            ServerSend.MovePlayer(ConnectedRoom, this);
        }

        public void RotateTurret(Quaternion turretRotation)
        {
            if (IsAlive)
                TurretRotation = turretRotation;
            ServerSend.RotateTurret(this);
        }

        public void TakeDamage(int damage, Player? enemy)
        {
            if (Health <= 0)
            {
                return;
            }

            Health -= damage;

            if (Health <= 0)
            {
                Health = 0;
                Die(enemy);
            }

            ServerSend.TakeDamage(Id, MaxHealth, Health);
        }

        private void Die(Player enemy)
        {
            enemy.Kills++;

            IsAlive = false;
            ServerSend.PlayerDead(Id);

            if (Team.PlayersDeathCheck())
            {
                //TODO: End game
            }

            //Send kill feed for each team
            ServerSend.ShowKillFeed(Team, Color.Red, enemy.Username, Username);
            ServerSend.ShowKillFeed(enemy.Team, Color.Lime, enemy.Username, Username);
        }

        public void Shoot(string? bulletPrefab, string? particlePrefab, Vector3 position, Quaternion rotation)
        {
            if (!CanShoot || !IsAlive)
                return;

            CanShoot = false;
            ServerSend.InstantiateObject(bulletPrefab, position, rotation, Id, ConnectedRoom);
            ServerSend.InstantiateObject(particlePrefab, position, rotation, Id, ConnectedRoom);

            Task.Run(async () =>
            {
                await Task.Delay((int) (Cooldown * 1000));
                CanShoot = true;
                return Task.CompletedTask;
            });
        }

        public CachedPlayer? CachePlayer()
        {
            return new CachedPlayer(this);
        }
    }
}