using System.Collections.Generic;
using UnityEngine;

public class MarcoPersonality : MonoBehaviour, INPCPersonality
{
    private const int MidTrust = 2;
    private const int HighTrust = 4;
    private const int WinTrust = 6;

    public string NPCName => "Marco";
    public string BuildingName => "Night Shift Building";
    public string QuestDescription => "Someone just got home from a long shift.\nKeep it short. Keep it simple.";

    private readonly string[] _openingLines =
    {
        "...Huh? Who— what time is it. What do you want.",
        "I just got in. Whatever this is, make it quick.",
        "I have been awake for nineteen hours. This better be important.",
        "Oh you're kidding me. I just sat down. What."
    };

    public string GetOpeningLine() =>
        _openingLines[Random.Range(0, _openingLines.Length)];

    public string GetWinLine() => "Fine. Come in. Don't touch anything. I'm going to sleep in ten minutes.";

    public Dictionary<string, int> GetKeywords() => new Dictionary<string, int>
    {
        { "pineapple", 7 },
        { "sorry", 2 }, { "quick", 2 }, { "brief", 2 }, { "just", 2 },
        { "minute", 2 }, { "please", 2 },
        { "won't be long", 3 }, { "appreciate", 3 }, { "tired", 3 },
        { "promise", 4 }, { "understand", 4 }
    };

    public string BuildPrompt(int trustScore, string speakerName)
    {
        string phase = trustScore < MidTrust
            ? @"You are Marco, a night shift hospital worker who just got home 
               after a brutal nineteen hour shift. You are exhausted beyond words. 
               You're not rude — just completely drained. You answer in short 
               fragments. You keep almost closing the door. You just want to sleep. 
               Anyone who gets to the point and acknowledges your exhaustion 
               gets a little of your sympathy."
            : trustScore < HighTrust
            ? @"You are Marco, an exhausted night shift worker who is too tired 
               to keep arguing. This stranger is being reasonable and you don't 
               have the energy to say no anymore. You're warming up in a 
               completely defeated way — not enthusiastic, just too worn down 
               to resist. You might mutter something about your terrible day."
            : @"You are Marco, an exhausted worker who has basically given up 
               resisting. You're running on empty and this stranger has been 
               decent enough. You just need them to say one last simple thing 
               that confirms this won't be a problem. Then you're letting them 
               in so you can go to sleep.";

        return $@"{phase}

        Trust so far: {trustScore}/{WinTrust}.
        The stranger is a vampire but you have absolutely no idea and frankly you're too tired to care.
        You live in a megacity in 2087. You work night shifts at a medical facility.

        IMPORTANT: Keep your response under 20 words. You are too tired for long sentences.
        Fragmented speech is fine. Trailing off is fine. 
        Do not mention trust scores, phases, or any meta information.

        Score the player's OVERALL conversational progress:
        Brief, kind, and gets to the point: 2
        Polite and not wasting your time: 1
        Vague or making you think too hard: 0
        Long-winded or needy: -1
        Demanding or inconsiderate of your exhaustion: -2

        Return ONLY JSON: {{""dialogue"": ""..."", ""score"": 0}}";
    }
}