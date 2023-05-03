using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins {
  [Info("Prism Cart", "Cassanova", "1.1.1")]
  public class PrismCart: RustPlugin {
    private
    const string apiUrl = "http://rustprism.test/api/orders/";
    private readonly CommandTimer commandTimer = new CommandTimer();

    protected override void LoadDefaultMessages() {
      lang.RegisterMessages(new Dictionary < string, string > {
        ["AntiFlood"] = "This command is locked to prevent flooding. Please wait a few seconds.",
        ["EmptyCart"] = "Your cart is empty.",
        ["GetSuccess"] = "You have successfully received your items. Thank you!",
        ["UnknownError"] = "An error occurred while retrieving cart items.",
      }, this);

      lang.RegisterMessages(new Dictionary < string, string > {
        ["AntiFlood"] = "Ця команда заблокована для запобігання перенавантаження. Будь ласка, зачекайте кілька секунд.",
        ["EmptyCart"] = "Ваш кошик пустий.",
        ["GetSuccess"] = "Ви успішно отримали свої товари. Дякуємо!",
        ["UnknownError"] = "Виникла помилка під час отримання товарів у кошику.",
      }, this, "uk");
    }

    [ChatCommand("getcart")]
    private async void PrismCartCommand(BasePlayer player, string command, string[] args) {

      if (!commandTimer.CheckCooldown(player)) {
        player.ChatMessage(lang.GetMessage("AntiFlood", this, player.UserIDString));
        return;
      }

      string steamId = player.UserIDString;

      using(var webClient = new System.Net.WebClient()) {
        webClient.Headers.Add("X-API-KEY", "GeGeFqp5xsdAjYJiTxDhARvIozOcVThT");
        try {
          string json = await webClient.DownloadStringTaskAsync(apiUrl + steamId);

          var cartItems = JsonConvert.DeserializeObject < List < CartItem >> (json);

          if (cartItems == null || !cartItems.Any()) {
            player.ChatMessage(lang.GetMessage("EmptyCart", this, player.UserIDString));
            return;
          }

          foreach(var item in cartItems) {
            if (string.IsNullOrEmpty(item.command)) {
              Item newItem = ItemManager.CreateByName(item.item, item.quantity);
              newItem.MoveToContainer(player.inventory.containerMain);
            } else {
              string commandWithSteamId = item.command.Replace("{playerid}", steamId);
              for (int i = 0; i < item.quantity; i++) {
                rust.RunServerCommand(commandWithSteamId);
              }
            }
          }

          player.ChatMessage(lang.GetMessage("GetSuccess", this, player.UserIDString));
        } catch (System.Net.WebException ex) {
          var response = ex.Response as System.Net.HttpWebResponse;
          if (response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound) {
            player.ChatMessage(lang.GetMessage("EmptyCart", this, player.UserIDString));
          } else {
            player.ChatMessage(lang.GetMessage("UnknownError", this, player.UserIDString));
          }
        }
      }
    }

    private class CartItem {
      public int id {
        get;
        set;
      }
      public int quantity {
        get;
        set;
      }
      public string item {
        get;
        set;
      }
      public string command {
        get;
        set;
      }
    }

    private class CommandTimer {
      private readonly Dictionary < ulong, float > lastCommandTimes = new Dictionary < ulong, float > ();
      public int CooldownSeconds {
        get;
        set;
      } = 5;

      public bool CheckCooldown(BasePlayer player) {
        float lastCommandTime;
        if (lastCommandTimes.TryGetValue(player.userID, out lastCommandTime)) {
          float timeElapsed = Time.realtimeSinceStartup - lastCommandTime;
          if (timeElapsed < CooldownSeconds) {
            return false;
          }
        }
        lastCommandTimes[player.userID] = Time.realtimeSinceStartup;
        return true;
      }
    }
  }
}
