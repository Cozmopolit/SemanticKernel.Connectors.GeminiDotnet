# SemanticKernel.Connectors.GeminiDotnet

A [Semantic Kernel](https://github.com/microsoft/semantic-kernel) connector for Google Gemini using [GeminiDotnet](https://github.com/rabuckley/GeminiDotnet).

## Features

- ✅ Full Semantic Kernel `IChatCompletionService` and `ITextGenerationService` support
- ✅ Gemini 2.5/3 Pro with thinking mode and `thoughtSignature` handling
- ✅ Function calling / tool use
- ✅ Streaming support
- ✅ Built on [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) architecture
- ✅ OpenTelemetry integration

## Installation

```bash
dotnet add package SemanticKernel.Connectors.GeminiDotnet
```

## Usage

### Basic Usage with Kernel Builder

```csharp
using Microsoft.SemanticKernel;

var kernel = Kernel.CreateBuilder()
    .AddGeminiChatCompletion(
        modelId: "gemini-2.5-flash",
        apiKey: Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY")!)
    .Build();

var response = await kernel.InvokePromptAsync("What is AI?");
Console.WriteLine(response);
```

### Direct Instantiation

```csharp
using SemanticKernel.Connectors.GeminiDotnet;

var geminiService = new GeminiChatCompletionService(
    modelId: "gemini-2.5-pro",
    apiKey: Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY")!);

var chatHistory = new ChatHistory();
chatHistory.AddUserMessage("Explain quantum computing in simple terms.");

var response = await geminiService.GetChatMessageContentsAsync(chatHistory);
Console.WriteLine(response[0].Content);
```

### With Dependency Injection

```csharp
services.AddGeminiChatCompletion(
    modelId: "gemini-2.5-flash",
    apiKey: configuration["GoogleAI:ApiKey"]!);
```

## Why This Connector?

This connector provides an alternative to the official `Microsoft.SemanticKernel.Connectors.Google` package, offering:

- **Based on GeminiDotnet**: Uses the lightweight, modern [GeminiDotnet](https://github.com/rabuckley/GeminiDotnet) library
- **M.E.AI Architecture**: Built on Microsoft.Extensions.AI for consistent patterns across providers
- **Actively Maintained**: Quick fixes and updates for Gemini API changes

## Supported Models

- `gemini-2.5-flash` / `gemini-2.5-flash-preview-*`
- `gemini-2.5-pro` / `gemini-2.5-pro-preview-*`
- `gemini-2.0-flash` / `gemini-2.0-flash-lite`
- All other Gemini models supported by the Google AI API

## License

MIT License - see [LICENSE](LICENSE) for details.

