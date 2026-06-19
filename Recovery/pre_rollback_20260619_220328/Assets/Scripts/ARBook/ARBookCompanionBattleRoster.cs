using System;
using System.Collections.Generic;
using UnityEngine;

public static class ARBookCompanionBattleRoster
{
    public const string PartyAKey = "CompanionBattleParty_A";
    public const string PartyBKey = "CompanionBattleParty_B";

    private const string MoodPrefix = "CompanionMood_";
    private const string LastMoodUtcPrefix = "CompanionMoodLastUtc_";

    public const int MaxMood = 100;
    public const int MinMoodForBattle = 25;
    public const int BattleActionMoodCost = 8;
    public const int BattleFinishMoodCost = 10;
    public const int InteractionMoodGain = 12;
    public const float MoodRecoverPerHour = 6f;

    public static readonly Dictionary<string, int> AttackById =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bulbasaur", 18 },
            { "Talonflame", 28 },
            { "Axew", 24 },
            { "Pikachu", 26 },
            { "Meowth", 17 },
            { "Infernape", 30 },
            { "Squirtle", 19 },
            { "Jirachi", 22 },
            { "Sneasler", 27 },
            { "Zorua", 21 },
            { "Zekrom", 42 },
            { "Zygarde", 34 },
            { "Toxtricity", 29 },
            { "Scizor", 31 },
            { "Mismagius", 25 },
            { "Mew", 32 },
            { "Manaphy", 16 },
            { "ElectrodeHisuian", 23 },
            { "Dragapult", 36 },
            { "Celebi", 24 }
        };

    public static string[] GetParty()
    {
        return new[]
        {
            PlayerPrefs.GetString(PartyAKey, string.Empty),
            PlayerPrefs.GetString(PartyBKey, string.Empty)
        };
    }

    public static void TogglePartyMember(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return;
        }

        string a = PlayerPrefs.GetString(PartyAKey, string.Empty);
        string b = PlayerPrefs.GetString(PartyBKey, string.Empty);

        if (SameId(a, captureId))
        {
            PlayerPrefs.DeleteKey(PartyAKey);
        }
        else if (SameId(b, captureId))
        {
            PlayerPrefs.DeleteKey(PartyBKey);
        }
        else if (string.IsNullOrWhiteSpace(a))
        {
            PlayerPrefs.SetString(PartyAKey, captureId);
        }
        else
        {
            PlayerPrefs.SetString(PartyBKey, captureId);
        }

        PlayerPrefs.Save();
    }

    public static bool IsInParty(string captureId)
    {
        string[] party = GetParty();
        return SameId(party[0], captureId) || SameId(party[1], captureId);
    }

    public static int GetAttack(string captureId)
    {
        if (!string.IsNullOrWhiteSpace(captureId) &&
            AttackById.TryGetValue(captureId, out int attack))
        {
            return attack;
        }

        return 20;
    }

    public static bool IsHealer(string captureId)
    {
        return SameId(captureId, "Manaphy");
    }

    public static int GetMood(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return 0;
        }

        ApplyPassiveMoodRecovery(captureId);
        return PlayerPrefs.GetInt(GetMoodKey(captureId), MaxMood);
    }

    public static bool CanBattle(string captureId)
    {
        return !string.IsNullOrWhiteSpace(captureId) &&
               GetMood(captureId) >= MinMoodForBattle;
    }

    public static void AddMood(string captureId, int amount)
    {
        if (string.IsNullOrWhiteSpace(captureId) || amount == 0)
        {
            return;
        }

        int next = Mathf.Clamp(GetMood(captureId) + amount, 0, MaxMood);
        PlayerPrefs.SetInt(GetMoodKey(captureId), next);
        StampMoodTime(captureId);
        PlayerPrefs.Save();
    }

    public static void SpendMood(string captureId, int amount)
    {
        AddMood(captureId, -Mathf.Abs(amount));
    }

    public static void SpendMoodForPartyAfterBattle()
    {
        string[] party = GetParty();
        for (int i = 0; i < party.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(party[i]))
            {
                SpendMood(party[i], BattleFinishMoodCost);
            }
        }
    }

    public static string GetActionLabel(string slotName, string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return $"{slotName} 未携带";
        }

        int mood = GetMood(captureId);
        int attack = GetAttack(captureId);
        string action = IsHealer(captureId) ? "回复" : "攻击";
        return $"{slotName} {captureId} {action}  攻{attack}  心情{mood}";
    }

    public static bool TryUseAction(
        string captureId,
        ARBookBattleCombatant player,
        ARBookBattleCombatant enemy,
        out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(captureId))
        {
            message = "这个携带位还没有宝可梦。";
            return false;
        }

        int mood = GetMood(captureId);
        if (mood < MinMoodForBattle)
        {
            message = $"{captureId} 心情太低，不愿意出战。";
            return false;
        }

        SpendMood(captureId, BattleActionMoodCost);
        if (IsHealer(captureId))
        {
            int heal = Mathf.Max(20, GetAttack(captureId) + 12);
            player?.Heal(heal);
            message = $"{captureId} 为主角回复 {heal} 点生命。";
            return true;
        }

        int damage = GetAttack(captureId);
        enemy?.TakeDamage(damage);
        message = $"{captureId} 助战攻击，造成 {damage} 点伤害。";
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PartyAKey);
        PlayerPrefs.DeleteKey(PartyBKey);
    }

    public static void ClearMood(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return;
        }

        PlayerPrefs.DeleteKey(GetMoodKey(captureId));
        PlayerPrefs.DeleteKey(GetLastMoodUtcKey(captureId));
    }

    public static void ClearAll(string[] captureIds)
    {
        Clear();
        if (captureIds == null)
        {
            PlayerPrefs.Save();
            return;
        }

        for (int i = 0; i < captureIds.Length; i++)
        {
            ClearMood(captureIds[i]);
        }

        PlayerPrefs.Save();
    }

    private static void ApplyPassiveMoodRecovery(string captureId)
    {
        string key = GetLastMoodUtcKey(captureId);
        string ticksText = PlayerPrefs.GetString(key, string.Empty);
        if (!long.TryParse(ticksText, out long ticks))
        {
            StampMoodTime(captureId);
            return;
        }

        DateTime last = new DateTime(ticks, DateTimeKind.Utc);
        float hours = Mathf.Max(0f, (float)(DateTime.UtcNow - last).TotalHours);
        if (hours <= 0.01f)
        {
            return;
        }

        int recovered = Mathf.FloorToInt(hours * MoodRecoverPerHour);
        if (recovered <= 0)
        {
            return;
        }

        int current = PlayerPrefs.GetInt(GetMoodKey(captureId), MaxMood);
        PlayerPrefs.SetInt(
            GetMoodKey(captureId),
            Mathf.Clamp(current + recovered, 0, MaxMood));
        StampMoodTime(captureId);
    }

    private static void StampMoodTime(string captureId)
    {
        PlayerPrefs.SetString(
            GetLastMoodUtcKey(captureId),
            DateTime.UtcNow.Ticks.ToString());
    }

    private static string GetMoodKey(string captureId)
    {
        return MoodPrefix + captureId;
    }

    private static string GetLastMoodUtcKey(string captureId)
    {
        return LastMoodUtcPrefix + captureId;
    }

    private static bool SameId(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
