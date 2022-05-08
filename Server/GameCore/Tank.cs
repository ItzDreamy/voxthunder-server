﻿using Serilog;
using VoxelTanksServer.Library;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public class Tank {
    private bool _initialized;

    public Tank(string name) {
        Name = name;
        Thread databaseThread = new(GetStats);
        databaseThread.Start();
    }

    public string Name { get; }
    public int Damage { get; private set; }
    public int MaxHealth { get; private set; }
    public float TowerRotateSpeed { get; private set; }
    public float TankRotateSpeed { get; private set; }
    public float AngleUp { get; private set; }
    public float AngleDown { get; private set; }
    public float MaxSpeed { get; private set; }
    public float Acceleration { get; private set; }
    public float MaxBackSpeed { get; private set; }
    public float BackAcceleration { get; private set; }
    public float Cooldown { get; private set; }

    public int Cost { get; private set; }

    private async void GetStats() {
        var table = await DatabaseUtils.RequestData(
            $"SELECT * FROM `tanksstats` WHERE `tankname` = '{Name.ToLower()}'");

        Damage = (int) table.Rows[0][2];
        MaxHealth = (int) table.Rows[0][3];
        TowerRotateSpeed =
            (float) table.Rows[0][4];
        TankRotateSpeed = (float) table.Rows[0][5];
        AngleUp = (float) table.Rows[0][6];
        AngleDown = (float) table.Rows[0][7];
        MaxSpeed = (float) table.Rows[0][8];
        Acceleration = (float) table.Rows[0][9];
        MaxBackSpeed = (float) table.Rows[0][10];
        BackAcceleration =
            (float) table.Rows[0][11];
        Cooldown = (float) table.Rows[0][12];
        Cost = (int) table.Rows[0][14];

        Log.Information($"Tank {Name.ToUpper()} initialized");
        _initialized = true;

        if (!Server.IsOnline) CheckForInitialize();
    }

    private void CheckForInitialize() {
        if (Server.Tanks.Any(tank => !tank._initialized)) return;

        Server.BeginListenConnections();
    }
}