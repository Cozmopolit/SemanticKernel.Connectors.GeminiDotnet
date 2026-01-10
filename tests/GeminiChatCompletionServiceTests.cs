// Copyright (c) Cozmopolit. All rights reserved.
// Licensed under the MIT License.

using Microsoft.SemanticKernel.Services;
using SemanticKernel.Connectors.GeminiDotnet;
using Xunit;

namespace SemanticKernel.Connectors.GeminiDotnet.Tests;

public class GeminiChatCompletionServiceTests
{
    private const string TestModelId = "gemini-2.0-flash";
    private const string TestApiKey = "test-api-key-12345";

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesService()
    {
        // Act
        using var service = new GeminiChatCompletionService(TestModelId, TestApiKey);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullModelId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GeminiChatCompletionService(null!, TestApiKey));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyModelId_ThrowsArgumentException(string modelId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GeminiChatCompletionService(modelId, TestApiKey));
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GeminiChatCompletionService(TestModelId, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyApiKey_ThrowsArgumentException(string apiKey)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GeminiChatCompletionService(TestModelId, apiKey));
    }

    #endregion

    #region Attributes Tests

    [Fact]
    public void Attributes_ContainsModelId()
    {
        // Arrange
        using var service = new GeminiChatCompletionService(TestModelId, TestApiKey);

        // Act
        var modelId = service.Attributes[AIServiceExtensions.ModelIdKey];

        // Assert
        Assert.Equal(TestModelId, modelId);
    }

    [Fact]
    public void Attributes_ContainsEndpoint()
    {
        // Arrange
        using var service = new GeminiChatCompletionService(TestModelId, TestApiKey);

        // Act
        var endpoint = service.Attributes[AIServiceExtensions.EndpointKey];

        // Assert
        Assert.NotNull(endpoint);
        Assert.Contains("generativelanguage.googleapis.com", endpoint?.ToString());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var service = new GeminiChatCompletionService(TestModelId, TestApiKey);

        // Act & Assert - should not throw
        service.Dispose();
        service.Dispose();
        service.Dispose();
    }

    #endregion
}

