using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Cove.Server;
using Cove.Server.Actor;
using Cove.Server.Plugins;
using Steamworks;
using System.Xml;

namespace Cove2Discord
{
    public class Cove2Discord : CovePlugin
    {
        private const string WebhookConfigFile = "webhook_config.json";
        private string webhookUrl;

        CoveServer Server { get; set; }

        public Cove2Discord(CoveServer server) : base(server) { }

        public override void onInit()
        {
            base.onInit();

            Log("Initializing Cove2Discord...");
            LoadWebhookUrl();

            // Validate the webhook URL
            if (!ValidateWebhook())
            {
                Log("Invalid webhook URL. Please provide a valid URL.");
                AskForWebhookUrl();
            }
        }

        public override void onChatMessage(WFPlayer sender, string message)
        {
            base.onChatMessage(sender, message);
            Log($"{sender.Username}: {message}");

            if (!string.IsNullOrEmpty(webhookUrl))
            {
                SendMessageToDiscord(sender.Username, message);
            }
        }

        private void LoadWebhookUrl()
        {
            if (File.Exists(WebhookConfigFile))
            {
                var config = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(WebhookConfigFile));
                webhookUrl = config?.webhookUrl ?? string.Empty;
            }
        }

        private void SaveWebhookUrl(string url)
        {
            var config = new { webhookUrl = url };
            File.WriteAllText(WebhookConfigFile, JsonConvert.SerializeObject(config, Formatting.Indented));
            webhookUrl = url;
        }

        private bool ValidateWebhook()
        {
            try
            {
                if (string.IsNullOrEmpty(webhookUrl)) return false;

                using var client = new HttpClient();
                var response = client.PostAsync(webhookUrl, new StringContent(
                    JsonConvert.SerializeObject(new { content = "Webhook validated!" }),
                    Encoding.UTF8, "application/json")).Result;

                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void AskForWebhookUrl()
        {
            Console.Write("Enter Discord Webhook URL: ");
            string url = Console.ReadLine();

            while (!ValidateWebhookUrl(url))
            {
                Console.WriteLine("Invalid URL. Please try again.");
                Console.Write("Enter Discord Webhook URL: ");
                url = Console.ReadLine();
            }

            SaveWebhookUrl(url);
        }

        private bool ValidateWebhookUrl(string url)
        {
            try
            {
                using var client = new HttpClient();
                var response = client.PostAsync(url, new StringContent(
                    JsonConvert.SerializeObject(new { content = "Validating webhook..." }),
                    Encoding.UTF8, "application/json")).Result;

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void SendMessageToDiscord(string username, string message)
        {
            try
            {
                using var client = new HttpClient();
                var payload = new
                {
                    username,
                    content = message
                };

                var response = client.PostAsync(webhookUrl, new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8, "application/json")).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Log($"Failed to send message to Discord. Response: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error sending message to Discord: {ex.Message}");
            }
        }
    }
}