using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace Services;

// Service for interacting with HashiCorp Vault to retrieve secrets.
public class VaultService
{
    private readonly string _vaultToken;
    private readonly string _vaultEndPoint;
    private readonly HttpClientHandler _httpClientHandler;

    public VaultService(string vaultToken, string vaultEndPoint)
    {
        _vaultToken = vaultToken;
        _vaultEndPoint = vaultEndPoint;

        // Setup HttpClientHandler to bypass SSL certificate validation
        _httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, SslPolicyErrors) => true
        };
    }

    public async Task<(string, string)> GetSecretAndIssuerAsync()
    {
        // Define the authentication method using a token
        IAuthMethodInfo authMethod = new TokenAuthMethodInfo(_vaultToken);

        // Configure Vault client settings
        var vaultClientSettings = new VaultClientSettings(_vaultEndPoint, authMethod)
        {
            MyHttpClientProviderFunc = _ => new HttpClient(_httpClientHandler)
            {
                BaseAddress = new Uri(_vaultEndPoint)
            }
        };

        // Initialize the Vault client
        var vaultClient = new VaultClient(vaultClientSettings);

        // Get environment variables
        var path = Environment.GetEnvironmentVariable("VAULT_PATH");
        var mount_point = Environment.GetEnvironmentVariable("VAULT_MOUNTPOINT");
        var key1 = Environment.GetEnvironmentVariable("VAULT_KEY1");
        var key2 = Environment.GetEnvironmentVariable("VAULT_KEY2");

        // Read secret data from the specified path in Vault
        Secret<SecretData> kv2Secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: path, mountPoint: mount_point);

        // Extract secret and issuer from the retrieved data
        var vault_key1 = kv2Secret.Data.Data[key1].ToString();
        var vault_key2 = kv2Secret.Data.Data[key2].ToString();

        // Return the secret and issuer
        return (vault_key1, vault_key2);
    }
}
