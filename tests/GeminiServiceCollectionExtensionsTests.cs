// Copyright (c) Cozmopolit. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using SemanticKernel.Connectors.GeminiDotnet;
using Xunit;

namespace SemanticKernel.Connectors.GeminiDotnet.Tests;

public class GeminiServiceCollectionExtensionsTests
{
    private const string TestModelId = "gemini-2.0-flash";
    private const string TestApiKey = "test-api-key-12345";

    #region IServiceCollection Extension Tests

    [Fact]
    public void AddGeminiChatCompletion_RegistersIChatCompletionService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddGeminiChatCompletion(TestModelId, TestApiKey);
        using var provider = services.BuildServiceProvider();

        // Assert
        var chatService = provider.GetService<IChatCompletionService>();
        Assert.NotNull(chatService);
        Assert.IsType<GeminiChatCompletionService>(chatService);
    }

    [Fact]
    public void AddGeminiChatCompletion_RegistersITextGenerationService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddGeminiChatCompletion(TestModelId, TestApiKey);
        using var provider = services.BuildServiceProvider();

        // Assert
        var textService = provider.GetService<ITextGenerationService>();
        Assert.NotNull(textService);
        Assert.IsType<GeminiChatCompletionService>(textService);
    }

    [Fact]
    public void AddGeminiChatCompletion_ReturnsSameInstance_ForBothInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddGeminiChatCompletion(TestModelId, TestApiKey);
        using var provider = services.BuildServiceProvider();

        // Assert
        var chatService = provider.GetService<IChatCompletionService>();
        var textService = provider.GetService<ITextGenerationService>();
        Assert.Same(chatService, textService);
    }

    [Fact]
    public void AddGeminiChatCompletion_WithServiceId_RegistersKeyedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        const string serviceId = "my-gemini";

        // Act
        services.AddGeminiChatCompletion(TestModelId, TestApiKey, serviceId: serviceId);
        using var provider = services.BuildServiceProvider();

        // Assert
        var chatService = provider.GetKeyedService<IChatCompletionService>(serviceId);
        var textService = provider.GetKeyedService<ITextGenerationService>(serviceId);
        Assert.NotNull(chatService);
        Assert.NotNull(textService);
        Assert.IsType<GeminiChatCompletionService>(chatService);
    }

    [Fact]
    public void AddGeminiChatCompletion_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddGeminiChatCompletion(TestModelId, TestApiKey));
    }

    #endregion

    #region IKernelBuilder Extension Tests

    [Fact]
    public void AddGeminiChatCompletion_OnKernelBuilder_RegistersService()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();

        // Act
        builder.AddGeminiChatCompletion(TestModelId, TestApiKey);
        var kernel = builder.Build();

        // Assert
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        Assert.NotNull(chatService);
        Assert.IsType<GeminiChatCompletionService>(chatService);
    }

    [Fact]
    public void AddGeminiChatCompletion_OnKernelBuilder_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IKernelBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddGeminiChatCompletion(TestModelId, TestApiKey));
    }

    #endregion
}

