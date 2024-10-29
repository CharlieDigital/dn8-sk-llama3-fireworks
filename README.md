# Need for Speed: LLMs Beyond OpenAI with .NET 8 SSE + Channels, Llama3, and Fireworks.ai

This repository demonstrates how to combine:

- .NET 8 Web APIs
- .NET 8 channels
- Microsoft Semantic Kernel
- Server Sent Events (SSE)
- Meta Llama 3 70B on Fireworks.ai

To build a high throughput recipe generator.

To see the full writeup, check out the article: [Need for Speed: LLMs Beyond OpenAI with .NET 8 SSE + Channels, Llama3, and Fireworks.ai](https://chrlschn.dev/blog/2024/05/need-for-speed-llms-beyond-openai-w-dotnet-sse-channels-llama3-fireworks-ai)

## Running the Demo

Follow these instructions to run the demo:

First, get an API key from [Fireworks.ai](https://fireworks.ai) and [Groq.com](https://groq.com) and add them to the user secrets.

```shell
cd api
dotnet user-secrets init
dotnet user-secrets set "RecipesConfig:FireworksKey" "YOUR_KEY_HERE"
dotnet user-secrets set "RecipesConfig:GroqKey" "YOUR_KEY_HERE"
```

To run the front-end:

```shell
cd web
yarn        # Restore packages
yarn dev    # Run the front-end demo.
```

In another terminal session:

```shell
cd api
dotnet run
```

If you're working with Semantic Kernel, also check out my other repo: [SKPromptGenerator](https://github.com/CharlieDigital/SKPromptGenerator)
