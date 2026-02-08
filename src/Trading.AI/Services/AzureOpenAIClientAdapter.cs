using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Trading.AI.Configuration;

namespace Trading.AI.Services;

/// <summary>
/// Azure OpenAI客户端适配器
/// </summary>
public class AzureOpenAIClientAdapter : IUnifiedAIClient
{
    private readonly ILogger<AzureOpenAIClientAdapter> _logger;
    private readonly AzureOpenAISettings _settings;
    private readonly AzureOpenAIClient _client;

    public AzureOpenAIClientAdapter(
        ILogger<AzureOpenAIClientAdapter> logger,
        IOptions<AzureOpenAISettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        if (string.IsNullOrEmpty(_settings.Endpoint) || string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI Endpoint 和 ApiKey 必须配置");
        }

        _client = new AzureOpenAIClient(
            new Uri(_settings.Endpoint),
            new AzureKeyCredential(_settings.ApiKey));

        _logger.LogInformation("Azure OpenAI客户端已初始化");
    }

    public async Task<AIResponse> CompleteChatAsync(
        string systemPrompt,
        string userMessage,
        float temperature,
        int maxTokens,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(modelName);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = temperature,
            MaxOutputTokenCount = maxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

        if (response?.Value?.Content == null || response.Value.Content.Count == 0)
        {
            throw new InvalidOperationException("Azure OpenAI返回空响应");
        }

        return new AIResponse
        {
            Content = response.Value.Content[0].Text,
            InputTokens = response.Value.Usage.InputTokenCount,
            OutputTokens = response.Value.Usage.OutputTokenCount,
            TotalTokens = response.Value.Usage.TotalTokenCount
        };
    }
}
