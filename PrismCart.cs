using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Prism Cart", "Cassanova", "1.2.0")]
    public class PrismCart : RustPlugin
    {
        private const string apiUrl = "http://rustprism.test/api/orders/";
        private readonly CommandTimer commandTimer = new CommandTimer();

        [ChatCommand("getcart")]
        private async void PrismCartCommand(BasePlayer player, string command, string[] args)
        {
            if (!commandTimer.CheckCooldown(player))
            {
                player.ChatMessage($"Please wait {commandTimer.CooldownSeconds} seconds before using this command again.");
                return;
            }

            string steamId = player.userID.ToString();

            using (var webClient = new System.Net.WebClient())
            {
                webClient.Headers.Add("X-API-KEY", "GeGeFqp5xsdAjYJiTxDhARvIozOcVThT");
                try
                {
                    string json = await webClient.DownloadStringTaskAsync(apiUrl + steamId);

                    var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(json);

                    if (cartItems == null || !cartItems.Any())
                    {
                        player.ChatMessage("Your cart is empty.");
                        return;
                    }

                    foreach (var item in cartItems)
                    {
                        if (string.IsNullOrEmpty(item.command))
                        {
                            Item newItem = ItemManager.CreateByName(item.item, item.quantity);
                            newItem.MoveToContainer(player.inventory.containerMain);
                            player.ChatMessage($"Gave {item.quantity} {item.item} to {player.displayName}.");
                        }
                        else
                        {
                            string commandWithPlayerId = item.command.Replace("{playerid}", steamId);
                            rust.RunServerCommand(commandWithPlayerId);
                            player.ChatMessage($"Executed command for {player.displayName}: {commandWithPlayerId}");
                        }
                    }
                }
                catch (System.Net.WebException ex)
                {
                    var response = ex.Response as System.Net.HttpWebResponse;
                    if (response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        player.ChatMessage("No cart items found.");
                    }
                    else
                    {
                        player.ChatMessage("An error occurred while retrieving cart items.");
                    }
                }
            }
        }

        private class CartItem
        {
            public int id { get; set; }
            public int quantity { get; set; }
            public string item { get; set; }
            public string command { get; set; }
        }

        private class CommandTimer
        {
            private readonly Dictionary<ulong, float> lastCommandTimes = new Dictionary<ulong, float>();
            public int CooldownSeconds { get; set; } = 5;

            public bool CheckCooldown(BasePlayer player)
            {
                float lastCommandTime;
                if (lastCommandTimes.TryGetValue(player.userID, out lastCommandTime))
                {
                    float timeElapsed = Time.realtimeSinceStartup - lastCommandTime;
                    if (timeElapsed < CooldownSeconds)
                    {
                        return false;
                    }
                }
                lastCommandTimes[player.userID] = Time.realtimeSinceStartup;
                return true;
            }
        }
    }
}
