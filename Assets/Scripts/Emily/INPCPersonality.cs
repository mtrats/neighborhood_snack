using System.Collections.Generic;

public interface INPCPersonality
{
    string NPCName { get; }
    string BuildingName { get; }
    string QuestDescription { get; }
    string GetOpeningLine();
    string GetWinLine();
    string BuildPrompt(int trustScore, string speakerName);
    Dictionary<string, int> GetKeywords();
}