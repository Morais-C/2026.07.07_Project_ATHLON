using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Llm;

public sealed class OpenRouterProvider : ILLMProvider, IDisposable
{
    private const string DefaultReferer = "https://github.com/athlon-project";
    private const string DefaultTitle = "Project Athlon Spike_03";

    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly bool _ownsHttpClient;

    public OpenRouterProvider(HttpClient? httpClient = null, string? apiKey = null, string? model = null)
    {
        _model = model
            ?? Environment.GetEnvironmentVariable("OPENROUTER_MODEL")
            ?? throw new InvalidOperationException(
                "OpenRouter model is required (pass ctor model, or set OpenRouter:Model / OPENROUTER_MODEL).");

        var key = apiKey
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? throw new InvalidOperationException(
                "OpenRouter API key is required (pass ctor apiKey, or set OpenRouter:ApiKey / OPENROUTER_API_KEY).");

        if (httpClient is null)
        {
            // LLM calls (especially Planner/Coder) can exceed the default 100s HttpClient timeout.
            // OPENROUTER_BASE_URL overrides the default OpenRouter origin (OpenAI-compatible relays).
            var resolvedBase = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL");
            if (string.IsNullOrWhiteSpace(resolvedBase))
            {
                resolvedBase = "https://openrouter.ai/";
            }

            if (!resolvedBase.EndsWith('/'))
            {
                resolvedBase += "/";
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(resolvedBase),
                Timeout = TimeSpan.FromMinutes(5)
            };
            _ownsHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", DefaultReferer);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", DefaultTitle);
    }

    public async Task<LlmCompletionResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        var request = new OpenRouterChatRequest(
            Model: _model,
            Messages:
            [
                new OpenRouterMessage("system", systemPrompt),
                new OpenRouterMessage("user", userPrompt)
            ]);

        var started = Stopwatch.StartNew();

        using var response = await _httpClient
            .PostAsJsonAsync("api/v1/chat/completions", request, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    new InvalidOperationException(
                        "OpenRouter rejected the API key (401 Unauthorized). Check OpenRouter:ApiKey."),
                HttpStatusCode.TooManyRequests =>
                    new InvalidOperationException(
                        "OpenRouter rate limit exceeded (429 Too Many Requests). Retry later or use a smaller model."),
                _ => new InvalidOperationException(
                    $"OpenRouter request failed ({(int)response.StatusCode} {response.ReasonPhrase}): {Truncate(body)}")
            };
        }

        var parsed = System.Text.Json.JsonSerializer.Deserialize<OpenRouterChatResponse>(body)
            ?? throw new InvalidOperationException("OpenRouter returned an empty response body.");

        var content = parsed.Choices?.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException("OpenRouter response did not include message content.");

        var promptTokens = parsed.Usage?.PromptTokens ?? 0;
        var completionTokens = parsed.Usage?.CompletionTokens ?? 0;
        var totalTokens = parsed.Usage?.TotalTokens ?? promptTokens + completionTokens;
        var reportedCost = parsed.Usage?.Cost ?? parsed.Usage?.TotalCost;
        var estimatedCost = reportedCost ?? LlmCostEstimator.Estimate(_model, promptTokens, completionTokens);

        return new LlmCompletionResult(
            Content: content,
            Model: parsed.Model ?? _model,
            PromptTokens: promptTokens,
            CompletionTokens: completionTokens,
            TotalTokens: totalTokens,
            Duration: started.Elapsed,
            EstimatedCostUsd: estimatedCost);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static string Truncate(string value, int maxLength = 500) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";

    private sealed record OpenRouterChatRequest(
        string Model,
        IReadOnlyList<OpenRouterMessage> Messages);

    private sealed record OpenRouterMessage(string Role, string Content);

    private sealed class OpenRouterChatResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("choices")]
        public List<OpenRouterChoice>? Choices { get; init; }

        [JsonPropertyName("usage")]
        public OpenRouterUsage? Usage { get; init; }
    }

    private sealed class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterMessageBody? Message { get; init; }
    }

    private sealed class OpenRouterMessageBody
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }

    private sealed class OpenRouterUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; init; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; init; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; init; }

        [JsonPropertyName("cost")]
        public decimal? Cost { get; init; }

        [JsonPropertyName("total_cost")]
        public decimal? TotalCost { get; init; }
    }
}
