// Copyright (c) Cozmopolit. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TextGeneration;

// Resolve ambiguous references between M.E.AI and SK
using TextContent = Microsoft.SemanticKernel.TextContent;
using StreamingTextContent = Microsoft.SemanticKernel.StreamingTextContent;

namespace SemanticKernel.Connectors.GeminiDotnet;

/// <summary>
/// Google Gemini chat completion service using Microsoft.Extensions.AI (M.E.AI) architecture.
/// Uses GeminiDotnet SDK with M.E.AI integration for unified connector patterns.
/// </summary>
/// <remarks>
/// <para>
/// This implementation follows the same M.E.AI pattern as OpenRouterChatCompletionService,
/// enabling unified execution settings (OpenAIPromptExecutionSettings) across all providers.
/// </para>
/// <para>
/// ThoughtSignature handling for Gemini 2.5/3 Pro is handled automatically by the
/// GeminiDotnet.Extensions.AI SDK's mapper layer.
/// </para>
/// </remarks>
public sealed class GeminiChatCompletionService : IChatCompletionService, ITextGenerationService, IDisposable
{
    private readonly IChatClient _chatClient;
    private readonly IChatCompletionService _innerService;
    private readonly Dictionary<string, object?> _attributes = new();
    private readonly ILogger _logger;
    private int _disposed;

    /// <summary>
    /// Create an instance of the Gemini chat completion connector with M.E.AI architecture.
    /// </summary>
    /// <param name="modelId">Model name (e.g., gemini-2.0-flash, gemini-2.5-pro)</param>
    /// <param name="apiKey">Google AI API Key</param>
    /// <param name="endpointId">Optional endpoint identifier for telemetry</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging.</param>
    /// <param name="defaultChatOptions">Optional default <see cref="ChatOptions"/> merged into every request.
    /// Used to inject provider-specific settings (e.g., ThinkingConfiguration from ProviderSpecificData).
    /// Request-level options take precedence over these defaults.</param>
    public GeminiChatCompletionService(
        string modelId,
        string apiKey,
        string? endpointId = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null,
        ChatOptions? defaultChatOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        loggerFactory ??= NullLoggerFactory.Instance;
        this._logger = loggerFactory.CreateLogger<GeminiChatCompletionService>();

        this._logger.LogDebug(
            "Creating GeminiChatCompletionService: ModelId={ModelId}, EndpointId={EndpointId}",
            modelId, endpointId);

        // 1. Create GeminiClient - either with custom HttpClient or via options
        IGeminiClient geminiClient;
        if (httpClient != null)
        {
            // Configure HttpClient with Gemini API requirements (only if not already configured)
            // This is defensive: if caller provides a pre-configured client, we respect their settings
            httpClient.BaseAddress ??= new Uri("https://generativelanguage.googleapis.com");

            // Only set timeout if it's the default (100 seconds) - respect caller's explicit timeout
            if (httpClient.Timeout == TimeSpan.FromSeconds(100))
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan; // Prevent timeouts on long LLM generations
            }

            // API key header - set if not already present (caller's pre-configured client is respected)
            if (!httpClient.DefaultRequestHeaders.Contains("x-goog-api-key"))
            {
                httpClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
            }

            geminiClient = new GeminiClient(httpClient, modelId);
        }
        else
        {
            var options = new GeminiClientOptions
            {
                ApiKey = apiKey,
                ModelId = modelId
            };
            geminiClient = new GeminiClient(options);
        }

        // 2. Build M.E.AI Pipeline with GeminiChatClient
        var builder = new GeminiChatClient(geminiClient)
            .AsBuilder();

        // Inject provider-specific default ChatOptions (e.g., ThinkingConfiguration from ProviderSpecificData).
        // Uses the same .Use() middleware pattern as the Anthropic connector's Temperature/TopP handling.
        if (defaultChatOptions?.AdditionalProperties is { Count: > 0 })
        {
            var defaults = defaultChatOptions;
            this._logger.LogDebug(
                "Gemini pipeline middleware configured with {Count} default AdditionalProperties for EndpointId={EndpointId}",
                defaults.AdditionalProperties.Count, endpointId);

            builder.Use(async (messages, options, next, cancellationToken) =>
            {
                options = MergeWithDefaults(options, defaults);
                await next(messages, options, cancellationToken).ConfigureAwait(false);
            });
        }

        builder
            .UseKernelFunctionInvocation(loggerFactory) // SK Filter-Integration for IAutoFunctionInvocationFilter
            .UseOpenTelemetry(loggerFactory, sourceName: "SemanticKernel.Connectors.GeminiDotnet")
            .UseLogging(loggerFactory);

        this._chatClient = builder.Build();

        // 3. SK Wrapper
        this._innerService = this._chatClient.AsChatCompletionService();

        // Attributes
        this._attributes[AIServiceExtensions.ModelIdKey] = modelId;
        this._attributes[AIServiceExtensions.EndpointKey] = "https://generativelanguage.googleapis.com/v1beta/models/";

        this._logger.LogDebug(
            "GeminiChatCompletionService initialized: ModelId={ModelId}, EndpointId={EndpointId}",
            modelId, endpointId);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => this._attributes;

    #region IChatCompletionService Implementation

    /// <inheritdoc/>
    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogDebug("GetChatMessageContentsAsync called with {MessageCount} messages", chatHistory.Count);
        return this._innerService.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogDebug("GetStreamingChatMessageContentsAsync called with {MessageCount} messages", chatHistory.Count);
        return this._innerService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
    }

    #endregion

    #region ITextGenerationService Implementation

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        // Delegate to chat completion (same pattern as OpenRouter connector)
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var results = await this.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken)
            .ConfigureAwait(false);

        return results
            .Select(m => new TextContent(m.Content, m.ModelId, m.InnerContent, Encoding.UTF8, m.Metadata))
            .ToList();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Delegate to chat completion (same pattern as OpenRouter connector)
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        await foreach (var chunk in this.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return new StreamingTextContent(
                chunk.Content,
                chunk.ChoiceIndex,
                chunk.ModelId,
                chunk.InnerContent,
                Encoding.UTF8,
                chunk.Metadata);
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Merges default <see cref="ChatOptions"/> into per-request options.
    /// Request-level values take precedence — defaults are only applied for keys not already present.
    /// </summary>
    private static ChatOptions MergeWithDefaults(ChatOptions? options, ChatOptions defaults)
    {
        options = options?.Clone() ?? new ChatOptions();

        if (defaults.AdditionalProperties is { Count: > 0 } defaultProps)
        {
            options.AdditionalProperties ??= new AdditionalPropertiesDictionary();

            foreach (var kvp in defaultProps)
            {
                if (!options.AdditionalProperties.ContainsKey(kvp.Key))
                {
                    options.AdditionalProperties[kvp.Key] = kvp.Value;
                }
            }
        }

        return options;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the service and releases the underlying chat client resources.
    /// Thread-safe: multiple calls are safe, only the first call disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref this._disposed, 1) == 1)
        {
            return;
        }

        this._chatClient.Dispose();
    }

    #endregion
}
