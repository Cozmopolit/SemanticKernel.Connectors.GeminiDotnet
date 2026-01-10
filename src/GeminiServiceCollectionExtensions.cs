// Copyright (c) Cozmopolit. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;

using SemanticKernel.Connectors.GeminiDotnet;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Gemini services in the dependency injection container.
/// Follows Semantic Kernel's service registration patterns.
/// </summary>
public static class GeminiServiceCollectionExtensions
{
    private const string DefaultHttpClientName = "GeminiApi";

    /// <summary>
    /// Adds Gemini chat completion service to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="modelId">The Gemini model ID (e.g., gemini-2.0-flash, gemini-2.5-pro).</param>
    /// <param name="apiKey">The Google AI API key.</param>
    /// <param name="endpointId">Optional endpoint identifier for telemetry.</param>
    /// <param name="httpClientName">Optional named HttpClient to use from IHttpClientFactory (default: "GeminiApi").</param>
    /// <param name="serviceId">Optional service identifier for keyed registration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Uses IHttpClientFactory to create HttpClient instances, ensuring proper lifecycle management.
    /// If IHttpClientFactory is not registered, creates service without custom HttpClient.
    /// </remarks>
    public static IServiceCollection AddGeminiChatCompletion(
        this IServiceCollection services,
        string modelId,
        string apiKey,
        string? endpointId = null,
        string? httpClientName = null,
        string? serviceId = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        var clientName = httpClientName ?? DefaultHttpClientName;

        GeminiChatCompletionService Factory(IServiceProvider serviceProvider, object? _)
        {
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory?.CreateClient(clientName);
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            return new GeminiChatCompletionService(
                modelId,
                apiKey,
                endpointId,
                httpClient,
                loggerFactory);
        }

        // Register concrete type once, then forward interfaces to ensure single instance
        if (serviceId is null)
        {
            // Non-keyed registration
            services.AddSingleton(sp => Factory(sp, null));
            services.AddSingleton<IChatCompletionService>(sp => sp.GetRequiredService<GeminiChatCompletionService>());
            services.AddSingleton<ITextGenerationService>(sp => sp.GetRequiredService<GeminiChatCompletionService>());
        }
        else
        {
            // Keyed registration
            services.AddKeyedSingleton(serviceId, Factory);
            services.AddKeyedSingleton<IChatCompletionService>(serviceId, (sp, key) => sp.GetRequiredKeyedService<GeminiChatCompletionService>(key));
            services.AddKeyedSingleton<ITextGenerationService>(serviceId, (sp, key) => sp.GetRequiredKeyedService<GeminiChatCompletionService>(key));
        }

        return services;
    }

    /// <summary>
    /// Adds Gemini chat completion service to the kernel builder.
    /// </summary>
    /// <param name="builder">The kernel builder to add the service to.</param>
    /// <param name="modelId">The Gemini model ID (e.g., gemini-2.0-flash, gemini-2.5-pro).</param>
    /// <param name="apiKey">The Google AI API key.</param>
    /// <param name="endpointId">Optional endpoint identifier for telemetry.</param>
    /// <param name="httpClientName">Optional named HttpClient to use from IHttpClientFactory (default: "GeminiApi").</param>
    /// <param name="serviceId">Optional service identifier for keyed registration.</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder AddGeminiChatCompletion(
        this IKernelBuilder builder,
        string modelId,
        string apiKey,
        string? endpointId = null,
        string? httpClientName = null,
        string? serviceId = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddGeminiChatCompletion(modelId, apiKey, endpointId, httpClientName, serviceId);
        return builder;
    }
}
