# AI Theapest and Client Dialog Sample

This was enspired by **Episode 4 -Build Your First .NET AI App** project on the [Code it with AI YouTube channel](https://www.youtube.com/@codeitwithai) 
and a ELIZA knockoff that I played with when GW-BASIC was a thing I played with on my brand new 386.  

It is a copied from [Episode-04](https://github.com/Code-it-with-AI/Episode-04?tab=readme-ov-file) and expanded to have 2 chatbots with different prompts talking to each other.
It uses the [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI/) library to call the GitHub models.

## Prompts

I learned more from playing with the prompts than I did from the code. I noticed that some small
changes to the prompts results in big changes of behavior and the other way around. Some "bugs" 
have appeared that required me to "fix" things, for example:
- 2 therapists: the client didn't clearly understand that they were supposed to assume the personality of the client, not treat that persona.
- Recurring themes:
  - Record Stores: Several clients worked/owned records stores, and the therapist had a strong opinion about vinyl vs streaming.
  - Lots of music: Besides record store workers, there are DJs, and dreams of band memberships for clients and their relitives 
  - Time Warp: Was the $decade in the past or present? Both characters would be inconsistent about it, sometimes treating it as the present and sometimes as the past. 

	
Prompts in this application are plain markdown files stored in `FirstApp/Prompts` and loaded at
startup. `AiTools.LoadPromptFiles()` reads and concatenates the listed `.md` files, and the combined
text is prepended to the system messages for each chatbot persona. The app also supports simple
`$`-variables (for example `$MaxRounds`, `$MinRounds`, `$HalfMaxRounds`) which are injected from
`Program.cs` into the prompt text before the conversation begins; those variables let you control
session behavior from code while keeping the descriptive prompt content in separate files. Persona-
specific files (for example `Therapist.md` or `Client.md`) are applied only to the corresponding
system prompt so you can reuse shared session details across multiple characters.

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
