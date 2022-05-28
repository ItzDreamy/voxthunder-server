using Microsoft.EntityFrameworkCore;
using Serilog;
using VoxelTanksServer.Database.Models;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public static class AuthorizationHandler {
    public static Task<bool> TryLogin(string username, string password, bool rememberUser, string ip,
        int clientId) {
        try {
            var authClient = Server.DatabaseService.Context.authdata.ToList()
                .Find(data => data.Login.ToLower() == username.ToLower() && data.Password == password);

            if (authClient != null) {
                var nickname = authClient.Login;

                Log.Information($"[{ip}] {nickname} успешно зашел в аккаунт");

                var samePlayer = Server.Clients.Values.ToList()
                    .Find(player =>
                        player.Data != null && player?.Data.Nickname?.ToLower() == username.ToLower());
                samePlayer?.Disconnect("Другой игрок зашел в аккаунт");

                var client = Server.Clients[clientId];
                client.Data = new PlayerData();
                client.Data.Nickname = nickname;
                client.IsAuth = true;

                if (rememberUser) {
                    var guid = Guid.NewGuid();
                    authClient.AuthId = guid.ToString();
                    Server.DatabaseService.Context.SaveChanges();
                    ServerSend.SendAuthId(guid.ToString(), clientId);
                }

                return Task.FromResult(true);
            }

            Log.Information($"[{ip}] {username} ввел некорректные данные.");
            return Task.FromResult(false);
        }
        catch (Exception e) {
            Log.Error(e.ToString());

            return Task.FromResult(false);
        }
    }
}