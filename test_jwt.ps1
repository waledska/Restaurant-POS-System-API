$source = @"
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

public class Program {
    public static async Task Main() {
        var handler = new HttpClientHandler {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
        var client = new HttpClient(handler);
        var body = new StringContent("{\"emailOrUserName\":\"admin\", \"password\":\"admin123\"}", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync("https://localhost:7286/api/Auth/login", body);
        var content = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        if(!doc.RootElement.GetProperty("success").GetBoolean()) {
            Console.WriteLine("Login failed"); return;
        }
        var token = doc.RootElement.GetProperty("data").GetProperty("token").GetString();
        
        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7286/api/Auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meResp = await client.SendAsync(request);
        Console.WriteLine((int)meResp.StatusCode);
        Console.WriteLine(await meResp.Content.ReadAsStringAsync());
    }
}
"@

Add-Type -TypeDefinition $source -Language CSharp -ReferencedAssemblies System.Net.Http, System.Text.Json
[Program]::Main().GetAwaiter().GetResult()
