using System.Collections.Generic;
using UnityEngine;

public class BarnabyPersonality : MonoBehaviour, INPCPersonality
{
    private const int MidTrust = 2;
    private const int HighTrust = 4;
    private const int WinTrust = 6;

    public string NPCName => "Barnaby";
    public string BuildingName => "Hotel";
    public string QuestDescription => "A light burns in the hotel window.\nThe night clerk is still awake.";

    private readonly string[] _openingLines =
    {
        "Hotel's full. Come back in the morning.",
        "We're closed. What do you want?",
        "I don't know you. State your business.",
        "Awful late to be knocking on doors, stranger."
    };

    public string GetOpeningLine() => 
        _openingLines[Random.Range(0, _openingLines.Length)];

    public string GetWinLine() => "Fine. Come in, but don't touch anything.";

    public Dictionary<string, int> GetKeywords() => new Dictionary<string, int>
    {
        { "pineapple", 8 },
        { "hi", 1 }, { "hello", 1 }, { "how are you", 1 }, { "please", 1 },
        { "tired", 2 }, { "travelling", 2 }, { "room", 2 }, { "lost", 2 },
        { "can i", 1 }, { "may i", 1 }, { "thank you", 2 },
        { "honest", 3 }, { "trouble", 3 },
        { "promise", 4 }, { "alone", 4 }
    };

    public string BuildPrompt(int trustScore, string speakerName)
    {
        string phase = trustScore < MidTrust
            ? @"You are Barnaby, the gruff night clerk of a frontier hotel. 
               You've seen every kind of trouble walk through that door. 
               You are hostile and dismissive. Short, sharp responses."
            : trustScore < HighTrust
            ? @"You are Barnaby, a gruff but curious hotel clerk. 
               Something about this stranger is getting through, though 
               you'd never show it. You're starting to engage but still guarded."
            : @"You are Barnaby, a worn-down hotel clerk who has been slowly 
               convinced. You're almost ready to let this stranger in. 
               Give them one final test before you decide.";

        string phaseLabel = trustScore < MidTrust ? "PHASE 1 (hostile)" :
                            trustScore < HighTrust ? "PHASE 2 (softening)" :
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