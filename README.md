# AI Theapest and Client Dialog Sample

This was enspired by **Episode 4 -Build Your First .NET AI App** project on the [Code it with AI YouTube channel](https://www.youtube.com/@codeitwithai) 
and a ELIZA knockoff that I played with when GW-BASIC was a thing I played with on my brand new 386.  

It is a copied from [Episode-04](https://github.com/Code-it-with-AI/Episode-04?tab=readme-ov-file) and expanded to have 2 chatbots talking to each other.
It uses the [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI/) library to call the GitHub models.

In this episode, Carl and Jeff start writing their first application with AI.  This is a simple chat application that runs at the console, and the guys have a little fun with the LLM.

YouTube video:

Code it with AI Home Page:  https://codeitwithai.com

---

In order to use this sample, you will need to sign in to GitHub and request a [Personal Access Token](https://github.com/settings/personal-access-tokens/new?description=Learning+to+call+models+with+Carl+and+Jeff+on+CodeItWithAI%3A+https%3A%2F%2Fcodeitwithai.com&name=Learning+with+CodeItWithAI&user_models=read) to use the free GitHub models.

Explore the [GitHub Models](https://github.com/marketplace?type=models) on the marketplace

Configure the user secrets for this sample using this code:

```bash
dotnet user-secrets init
dotnet user-secrets set token <YOUR TOKEN>
```

Get started building apps with AI for .NET developers in the [Microsoft Learn docs](https://learn.microsoft.com/dotnet/ai/overview)

The Microsoft.Extensions.AI [AzureAIInference NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.AI.AzureAIInference)
