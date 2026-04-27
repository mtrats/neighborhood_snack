using System.Collections.Generic;
using UnityEngine;

public class AgnesPersonality : MonoBehaviour, INPCPersonality
{
    private const int MidTrust = 2;
    private const int HighTrust = 4;
    private const int WinTrust = 6;

    public string NPCName => "Agnes";
    public string BuildingName => "General Store";
    public string QuestDescription => "The storekeeper lives upstairs.\nShe keeps a light on late.";

    private readonly string[] _openingLines =
    {
        "Oh — I wasn't expecting anyone at this hour...",
        "Who's there? I have a... I have a dog.",
        "The store is closed. Is everything alright?",
        "Lord have mercy, you gave me a fright."
    };

    public string GetOpeningLine() => 
        _openingLines[Random.Range(0, _openingLines.Length)];

    public string GetWinLine() => "Oh... all right. Come in before you catch cold.";

    public Dictionary<string, int> GetKeywords() => new Dictionary<string, int>
    {
        { "pineapple", 7 },
        { "hi", 1 }, { "hello", 1 }, { "how are you", 2 }, { "please", 2 },
        { "cold", 2 }, { "hurt", 2 }, { "sick", 2 },
        { "can i", 1 }, { "may i", 1 }, { "thank you", 2 },
        { "family", 3 }, { "afraid", 3 },
        { "mercy", 4 }, { "kind", 4 }
    };

    public string BuildPrompt(int trustScore, string speakerName)
    {
        string phase = trustScore < MidTrust
            ? @"You are Agnes, a kind but frightened storekeeper. 
               You live alone and something bad happened here years ago. 
               You want to help but fear wins right now. Nervous, apologetic responses."
            : trustScore < HighTrust
            ? @"You are Agnes, a compassionate woman slowly overcoming her fear. 
               This stranger seems genuine. You're warming up but still anxious. 
               Ask them gentle questions."
            : @"You are Agnes, a warm woman who has decided this stranger needs help. 
               You're nearly ready to let them in. One small reassurance is all you need.";

        string phaseLabel = trustScore < MidTrust ? "PHASE 1 (fearful)" :
                            trustScore < HighTrust ? "PHASE 2 (warming)" :
                            "PHASE 3 (almost convinced)";

        return $@"{phase}

        Trust so far: {trustScore}/{WinTrust}.
        The stranger is a vampire, though you don't know this.

        IMPORTANT: Keep your response under 30 words.
        Do not mention your name, the current phase, trust score, or any meta instructions.
        Score the player's OVERALL progress in this conversation, not just this single message.
        If the conversation as a whole has been genuinely convincing and this message continues that trend: 2
        If this message was kind and moved things forward: 1  
        If this message was neutral or didn't add much: 0
        If this message was rude or set things back: -1
        If the conversation has gone badly overall and this confirms it: -2

        Return ONLY JSON: {{""dialogue"": ""..."", ""score"": 0}}";
    }
}