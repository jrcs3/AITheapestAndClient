# AI Theapest and Client Dialog Sample

This was enspired by **Episode 4 -Build Your First .NET AI App** project on the [Code it with AI YouTube channel](https://www.youtube.com/@codeitwithai) 
and a ELIZA knockoff that I played with when GW-BASIC was a thing I played with on my brand new 386.  

It is a copied from [Episode-04](https://github.com/Code-it-with-AI/Episode-04?tab=readme-ov-file) and expanded to have 2 chatbots with different prompts talking to each other.
It uses the [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI/) library to call the GitHub models.

## Prompts

I learned more from playing with the prompts than I did from the code.

## Setup / Environment

Prerequisites:
- Install the .NET 10 SDK: https://dotnet.microsoft.com/download
- Git (to clone the repository)

Quick start:

1. Clone the repo and restore packages:

   ```bash
   git clone <repo-url>
   cd AITheapestAndClient
   dotnet restore
   ```

2. Set a GitHub Personal Access Token for the AI inference service as user secrets
   (the code reads the `ai_token` or `token` key):

   ```bash
   cd FirstApp
   dotnet user-secrets init
   dotnet user-secrets set "ai_token" "<YOUR_GITHUB_PAT>"
   ```

   Create a token at: https://github.com/settings/personal-access-tokens/new

3. Ensure prompt files are present in `FirstApp/Prompts` (the app will load `*.md` files
   from there). If you run the project from a different working directory make sure the
   `FirstApp/Prompts` path is available at runtime.

4. Run the app:

   ```bash
   dotnet run --project FirstApp
   ```

5. Run tests:

   ```bash
   dotnet test
   ```

Notes:
- The project currently reads the token from user secrets. If you prefer an environment
  variable or other store, update `AiTools.GetToken()` in `FirstApp/AiTools.cs`.
- If prompts are not loaded at runtime, add them to `FirstApp/Prompts` or update the
  search paths in `AiTools.LoadPromptFiles()`.
