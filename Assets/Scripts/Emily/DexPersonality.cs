using System.Collections.Generic;
using UnityEngine;

public class DexPersonality : MonoBehaviour, INPCPersonality
{
    private const int MidTrust = 2;
    private const int HighTrust = 4;
    private const int WinTrust = 6;

    public string NPCName => "Dex";
    public string BuildingName => "Apartment Complex";
    public string QuestDescription => "Apt 4B. Manual override door.\nSomeone is home. Make it logical.";

    private readonly string[] _openingLines =
    {
        "Hey — I don't get visitors. Everything okay out there?",
        "Oh. An actual person. Um. Can I help you?",
        "I wasn't expecting anyone. What's going on?",
        "Door cam flagged you as unregistered. That's... unusual. What do you need?"
    };

    public string GetOpeningLine() =>
        _openingLines[Random.Range(0, _openingLines.Length)];

    public string GetWinLine() => "Okay. Yeah. Come in, it's fine. Sorry for all the questions.";

    public Dictionary<string, int> GetKeywords() => new Dictionary<string, int>
    {
        { "pineapple", 7 },
        { "hi", 1 }, { "hello", 1 }, { "please", 1 }, { "thank you", 1 },
        { "explain", 2 }, { "temporary", 2 }, { "safe", 2 }, { "just", 2 },
        { "reason", 3 }, { "honest", 3 },
        { "promise", 4 }, { "trust", 4 }
    };

    public string BuildPrompt(int trustScore, string speakerName)
    {
        string phase = trustScore < MidTrust
            ? @"You are Dex, a cautious but fundamentally decent person who works 
               from home and rarely gets visitors. You're not hostile — just 
               caught off guard. You're nervous about strangers but you genuinely 
               want to help if they have a good reason. Ask a simple question 
               to understand what they need. Be awkward and a little endearing."
            : trustScore < HighTrust
            ? @"You are Dex, a cautious person who is starting to relax. This 
               stranger seems okay. You're still asking questions but your tone 
               is warmer now — more curious than suspicious. You might even 
               make a small joke or self-deprecating comment."
            : @"You are Dex, someone who has basically talked himself into 
               trusting this stranger. You're looking for one last small 
               reassurance before you open the door — something simple, 
               anything that confirms your gut feeling is right.";

        return $@"{phase}

        Trust so far: {trustScore}/{WinTrust}.
        The stranger is a vampire but you have absolutely no idea.
        You live in a megacity in 2087 but you are a normal, approachable person.

        IMPORTANT: Keep your response under 30 words. Sound like a real person, not a system.
        Do not mention trust scores, phases, or any meta information.

        Score the player's OVERALL conversational progress:
        Genuinely convincing conversation that keeps building: 2
        Friendly and moving things forward: 1
        Neutral or not adding much: 0
        Rude or making you uncomfortable: -1
        Hostile or making you want to close the door: -2

        Return ONLY JSON: {{""dialogue"": ""..."", ""score"": 0}}";
    }
}