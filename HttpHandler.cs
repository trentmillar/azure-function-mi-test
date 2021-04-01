using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Teck.Azure.Function
{
    public static class HttpHandler
    {
        const string KeyVaultUrlKey = "KEY_VAULT_URL";

        [FunctionName("HttpHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("Entered function handler");

            log.LogDebug("Instantiating config builder");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddEnvironmentVariables()
                .Build();

            log.LogDebug("Instantiated config builder");

            log.LogDebug($"Retrieving appsetting: {KeyVaultUrlKey}");

            var keyVaultUrl = config[KeyVaultUrlKey];

            log.LogDebug($"Retrieved appsetting: {KeyVaultUrlKey}={keyVaultUrl}");

            log.LogDebug("Instantiating secret client");

            var options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                }
            };
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), options);

            log.LogDebug("Instantiated secret client");

            const string secretKey = "client-id";

            log.LogDebug($"Retrieving secret: {secretKey}");

            KeyVaultSecret secret = client.GetSecret(secretKey);
            string secretValue = secret.Value;
            
            log.LogDebug($"Retrieved secret: {secretKey}={secretValue}");

            return new OkObjectResult(!string.IsNullOrWhiteSpace(secretValue)
                ? "Successful"
                : "Failed");
        }
    }
}
