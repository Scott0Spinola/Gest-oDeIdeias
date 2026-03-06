using System.Net.Http.Json;

namespace GestãoIdeas.Services;

public interface IAdviceService
{
    Task<string> GetRandomAdviceAsync(CancellationToken cancellationToken = default);
}

public sealed class AdviceService : IAdviceService
{
    private readonly HttpClient _httpClient;

    public AdviceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.adviceslip.com/");
    }

    public async Task<string> GetRandomAdviceAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<AdviceSlipResponse>("advice", cancellationToken);
        return response?.Slip?.Advice ?? string.Empty;
    }

    private sealed class AdviceSlipResponse
    {
        public SlipDto? Slip { get; set; }
    }

    private sealed class SlipDto
    {
        public int Id { get; set; }
        public string Advice { get; set; } = string.Empty;
    }
}
