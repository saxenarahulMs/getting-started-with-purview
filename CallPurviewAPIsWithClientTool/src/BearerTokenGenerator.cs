using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class BearerTokenGenerator
{
    private readonly string AadResourceUrlProdPrefix = "https://login.windows.net/";
    private readonly string AadResourceUrlProdSuffix = "/oauth2/token";
    private readonly string resource = "https://purview.azure.net/";

    public async Task<string> GenerateAadTokenAsync(string spId, string spKey, string tenantDomain)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(spId)) throw new ArgumentException("Service principal ID cannot be null or empty.", nameof(spId));
        if (string.IsNullOrWhiteSpace(spKey)) throw new ArgumentException("Service principal key cannot be null or empty.", nameof(spKey));
        if (string.IsNullOrWhiteSpace(tenantDomain)) throw new ArgumentException("Tenant domain cannot be null or empty.", nameof(tenantDomain));

        string resourceUrl = $"{AadResourceUrlProdPrefix}{tenantDomain}{AadResourceUrlProdSuffix}";

        var values = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", spId },
            { "client_secret", spKey },
            { "resource", resource }
        };

        using (var content = new FormUrlEncodedContent(values))
        using (var authClient = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await authClient.PostAsync(resourceUrl, content);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                if (responseData != null && responseData.TryGetValue("access_token", out var token))
                {
                    return token?.ToString() ?? throw new InvalidOperationException("Access token is null.");
                }

                throw new InvalidOperationException("Access token not found in the response.");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to generate AAD token: {ex.Message}", ex);
            }
        }
    }

    static async Task Main(string[] args)
    {
        string spId = "your-service-principal-id"; // Replace with your actual SP ID
        string spKey = "your-service-principal-key"; // Replace with your actual SP Key
        string tenantDomain = "your-tenant-domain.onmicrosoft.com"; // Replace with your tenant domain

        try
        {
            var tokenGenerator = new BearerTokenGenerator();
            string token = await tokenGenerator.GenerateAadTokenAsync(spId, spKey, tenantDomain);
            Console.WriteLine($"Generated Token: {"Bearer "+token}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
