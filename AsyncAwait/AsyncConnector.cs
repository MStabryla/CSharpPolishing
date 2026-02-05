namespace AsyncAwait;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

public class AsyncConnector : IDisposable
{

    private HttpClient _httpClient;

    public AsyncConnector()
    {
        _httpClient = new HttpClient();
    }

    public AsyncConnector(string url)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(url)
        };
    }


    public string Url
    {
        get
        {
            return _httpClient.BaseAddress != null ? _httpClient.BaseAddress.AbsolutePath : "";
        }
        set
        {
            _ = _httpClient.BaseAddress;
            Uri? tempUri;
            if (Uri.TryCreate(value, UriKind.Absolute, out tempUri))
                _httpClient.BaseAddress = tempUri;
        }
    }


    public async Task<object> RetrieveData(string path)
    {
        if(_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("No url has been set");
        }
        var respMessage = await _httpClient.GetAsync(path);
        var respObject = await respMessage.Content.ReadFromJsonAsync<object>() ?? throw new NullReferenceException("Returned object is null");
        return respObject;
    }

    public async Task<string> RetrieveStringData(string path)
    {
        if(_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("No url has been set");
        }
        var respMessage = await _httpClient.GetAsync(path);
        var respObject = await respMessage.Content.ReadAsStringAsync() ?? throw new NullReferenceException("Returned string is null");
        return respObject;
    }


    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
