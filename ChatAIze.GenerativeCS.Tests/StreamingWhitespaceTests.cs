using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ClaudeClientOptions = ChatAIze.GenerativeCS.Options.Claude.ClaudeClientOptions;
using ClaudeChatCompletionOptions = ChatAIze.GenerativeCS.Options.Claude.ChatCompletionOptions;
using GeminiClientOptions = ChatAIze.GenerativeCS.Options.Gemini.GeminiClientOptions;
using GeminiChatCompletionOptions = ChatAIze.GenerativeCS.Options.Gemini.ChatCompletionOptions;
using GrokClientOptions = ChatAIze.GenerativeCS.Options.Grok.GrokClientOptions;
using GrokChatCompletionOptions = ChatAIze.GenerativeCS.Options.Grok.ChatCompletionOptions;
using OpenAIClientOptions = ChatAIze.GenerativeCS.Options.OpenAI.OpenAIClientOptions;
using OpenAIChatCompletionOptions = ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions;

namespace ChatAIze.GenerativeCS.Tests;

public sealed class StreamingWhitespaceTests
{
    [Fact]
    public async Task OpenAIStreaming_PreservesWhitespaceOnlyChunks()
    {
        var client = CreateOpenAIClient("""
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"gpt-5.2","choices":[{"index":0,"delta":{"role":"assistant","content":"Over"},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"gpt-5.2","choices":[{"index":0,"delta":{"content":" "},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"gpt-5.2","choices":[{"index":0,"delta":{"content":"5"},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"gpt-5.2","choices":[{"index":0,"delta":{"content":" million"},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"gpt-5.2","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}
            data: [DONE]
            """);

        var chat = new Chat();
        _ = await chat.FromUserAsync("How can you help?");

        var chunks = await CollectAsync(client.StreamCompletionAsync(chat));

        Assert.Equal(["Over", " ", "5", " million"], chunks);
        Assert.Equal("Over 5 million", string.Concat(chunks));

        var lastMessage = Assert.IsType<ChatMessage>(chat.Messages.Last());
        Assert.Equal(ChatRole.Chatbot, lastMessage.Role);
        Assert.Equal("Over 5 million", lastMessage.Content);
    }

    [Fact]
    public async Task GeminiStreaming_PreservesWhitespaceOnlyChunks()
    {
        var client = CreateGeminiClient("""
            data: {"candidates":[{"content":{"parts":[{"text":"Over"}]}}]}
            data: {"candidates":[{"content":{"parts":[{"text":" "}]}}]}
            data: {"candidates":[{"content":{"parts":[{"text":"5"}]}}]}
            data: {"candidates":[{"content":{"parts":[{"text":" million"}]},"finishReason":"STOP"}]}
            data: [DONE]
            """);

        var chat = new Chat();
        _ = await chat.FromUserAsync("How can you help?");

        var chunks = await CollectAsync(client.StreamCompletionAsync(chat));

        Assert.Equal(["Over", " ", "5", " million"], chunks);
        Assert.Equal("Over 5 million", string.Concat(chunks));

        var lastMessage = Assert.IsType<ChatMessage>(chat.Messages.Last());
        Assert.Equal(ChatRole.Chatbot, lastMessage.Role);
        Assert.Equal("Over 5 million", lastMessage.Content);
    }

    [Fact]
    public async Task ClaudeStreaming_PreservesWhitespaceOnlyChunks()
    {
        var client = CreateClaudeClient("""
            data: {"type":"message_start","message":{"usage":{"input_tokens":1,"output_tokens":0}}}
            data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Over"}}
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" "}}
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"5"}}
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" million"}}
            data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"output_tokens":4}}
            """);

        var chat = new Chat();
        _ = await chat.FromUserAsync("How can you help?");

        var chunks = await CollectAsync(client.StreamCompletionAsync(chat));

        Assert.Equal(["Over", " ", "5", " million"], chunks);
        Assert.Equal("Over 5 million", string.Concat(chunks));

        var lastMessage = Assert.IsType<ChatMessage>(chat.Messages.Last());
        Assert.Equal(ChatRole.Chatbot, lastMessage.Role);
        Assert.Equal("Over 5 million", lastMessage.Content);
    }

    [Fact]
    public async Task GrokStreaming_PreservesWhitespaceOnlyChunks()
    {
        var client = CreateGrokClient("""
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"grok-4","choices":[{"index":0,"delta":{"role":"assistant","content":"Over"},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"grok-4","choices":[{"index":0,"delta":{"content":" "},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"grok-4","choices":[{"index":0,"delta":{"content":"5"},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"grok-4","choices":[{"index":0,"delta":{"content":" million"},"finish_reason":null}]}
            data: {"id":"chatcmpl_test","object":"chat.completion.chunk","created":0,"model":"grok-4","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}
            data: [DONE]
            """);

        var chat = new Chat();
        _ = await chat.FromUserAsync("How can you help?");

        var chunks = await CollectAsync(client.StreamCompletionAsync(chat));

        Assert.Equal(["Over", " ", "5", " million"], chunks);
        Assert.Equal("Over 5 million", string.Concat(chunks));

        var lastMessage = Assert.IsType<ChatMessage>(chat.Messages.Last());
        Assert.Equal(ChatRole.Chatbot, lastMessage.Role);
        Assert.Equal("Over 5 million", lastMessage.Content);
    }

    private static OpenAIClient CreateOpenAIClient(string responseBody)
    {
        var httpClient = CreateHttpClient(responseBody);
        var options = Microsoft.Extensions.Options.Options.Create(new OpenAIClientOptions
        {
            ApiKey = "test-key",
            DefaultCompletionOptions = new OpenAIChatCompletionOptions
            {
                MaxAttempts = 1
            }
        });

        return new OpenAIClient(httpClient, options);
    }

    private static GeminiClient CreateGeminiClient(string responseBody)
    {
        var httpClient = CreateHttpClient(responseBody);
        var options = Microsoft.Extensions.Options.Options.Create(new GeminiClientOptions
        {
            ApiKey = "test-key",
            DefaultCompletionOptions = new GeminiChatCompletionOptions
            {
                MaxAttempts = 1
            }
        });

        return new GeminiClient(httpClient, options);
    }

    private static ClaudeClient CreateClaudeClient(string responseBody)
    {
        var httpClient = CreateHttpClient(responseBody);
        var options = Microsoft.Extensions.Options.Options.Create(new ClaudeClientOptions
        {
            ApiKey = "test-key",
            DefaultCompletionOptions = new ClaudeChatCompletionOptions
            {
                MaxAttempts = 1
            }
        });

        return new ClaudeClient(httpClient, options);
    }

    private static GrokClient CreateGrokClient(string responseBody)
    {
        var httpClient = CreateHttpClient(responseBody);
        var options = Microsoft.Extensions.Options.Options.Create(new GrokClientOptions
        {
            ApiKey = "test-key",
            DefaultCompletionOptions = new GrokChatCompletionOptions
            {
                MaxAttempts = 1
            }
        });

        return new GrokClient(httpClient, options);
    }

    private static HttpClient CreateHttpClient(string responseBody)
    {
        var handler = new StubHttpMessageHandler(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8)
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
            return response;
        });

        return new HttpClient(handler);
    }

    private static async Task<List<string>> CollectAsync(IAsyncEnumerable<string> stream)
    {
        var chunks = new List<string>();

        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);
        }

        return chunks;
    }

    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory());
        }
    }
}
