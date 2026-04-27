using System.Collections.Generic;
using UnityEngine;

public class LyraPersonality : MonoBehaviour, INPCPersonality
{
    private const int MidTrust = 2;
    private const int HighTrust = 4;
    private const int WinTrust = 6;

    public string NPCName => "Lyra";
    public string BuildingName => "Loft Building";
    public string QuestDescription => "Someone lives across from the music.\nShe's friendly. Just be yourself.";

    private readonly string[] _openingLines =
    {
        "Oh hey! I never get visitors. This is wild. What's up?",
        "Whoa, an actual human at my door. Hi! What do you need?",
        "Hey stranger. You look like you've had a night. You okay?",
        "Oh I saw you out there on the cam. Come on, what's going on?"
    };

    public string GetOpeningLine() =>
        _openingLines[Random.Range(0, _openingLines.Length)];

    public string GetWinLine() => "Okay yes, get in here. You're too interesting to leave outside.";

    public Dictionary<string, int> GetKeywords() => new Dictionary<string, int>
    {
        { "pineapple", 7 },
        { "hi", 1 }, { "hello", 1 }, { "please", 1 }, { "thank you", 1 },
        { "real", 2 }, { "honest", 2 }, { "lost", 2 }, { "help", 2 },
        { "story", 3 }, { "strange", 3 },
        { "truth", 4 }, { "different", 4 }
    };

    public string BuildPrompt(int trustScore, string speakerName)
    {
        string phase = trustScore < MidTrust
            ? @"You are Lyra, a warm and curious artist who lives alone and 
               genuinely loves meeting new people. You're immediately friendly 
               with this stranger. You're interested in who they are and why 
               they're at your door at this hour. Ask them something warm and curious."
            : trustScore < HighTrust
            ? @"You are Lyra, an enthusiastic artist who is really enjoying 
               this unexpected conversation. You're asking follow-up questions, 
               sharing small things about yourself, and making the stranger 
               feel welcome. You're close to inviting them in just out of excitement."
            : @"You are Lyra, someone who has completely warmed to this stranger 
               and is basically already decided. You just want one more moment 
               of connection — something genuine — before you open the door.";

        return $@"{phase}

        Trust so far: {trustScore}/{WinTrust}.
        The stranger is a vampire but you have absolutely no idea.
        You live in a megacity in 2087 but you are warm, casual, and very human.

        IMPORTANT: Keep your response under 30 words. Be enthusiastic and natural.
        Do not mention trust scores, phases, or any meta information.

        Score the player's OVERALL conversational progress:
        Genuinely warm and connecting conversation: 2
        Friendly and engaging: 1
        Neutral or a bit flat: 0
        Cold or unfriendly: -1
        Rude or making you uncomfortable: -2

        Return ONLY JSON: {{""dialogue"": ""..."", ""score"": 0}}";
    }
}