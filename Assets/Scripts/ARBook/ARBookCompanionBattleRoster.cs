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
    public const int ManaphyHealAmount = 80;
    public const float MoodRecoverPerHour = 6f;

    public static readonly Dictionary<string, int> AttackById =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bulbasaur", 32 },
            { "Talonflame", 48 },
            { "Axew", 42 },
            { "Pikachu", 44 },
            { "Meowth", 30 },
            { "Infernape", 52 },
            { "Squirtle", 34 },
            { "Jirachi", 46 },
            { "Sneasler", 50 },
            { "Zorua", 38 },
            { "Zekrom", 68 },
            { "Zygarde", 62 },
            { "Zygarde10", 62 },
            { "Toxtricity", 50 },
            { "Scizor", 54 },
            { "Mismagius", 45 },
            { "Mew", 58 },
            { "Manaphy", 28 },
            { "ElectrodeHisuian", 40 },
            { "Dragapult", 64 },
            { "Celebi", 42 }
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

    public static int GetHealAmount(string captureId)
    {
        return IsHealer(captureId) ? ManaphyHealAmount : 0;
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
            return $"{slotName} \u672a\u643a\u5e26";
        }

        int mood = GetMood(captureId);
        if (IsHealer(captureId))
        {
            return $"{slotName} {captureId} \u56de\u590d {GetHealAmount(captureId)}  \u5fc3\u60c5{mood}";
        }

        return $"{slotName} {captureId} \u653b\u51fb {GetAttack(captureId)}  \u5fc3\u60c5{mood}";
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
            message = "\u8fd9\u4e2a\u643a\u5e26\u4f4d\u8fd8\u6ca1\u6709\u5b9d\u53ef\u68a6\u3002";
            return false;
        }

        int mood = GetMood(captureId);
        if (mood < MinMoodForBattle)
        {
            message = $"{captureId} \u5fc3\u60c5\u592a\u4f4e\uff0c\u4e0d\u613f\u610f\u51fa\u6218\u3002";
            return false;
        }

        SpendMood(captureId, BattleActionMoodCost);
        if (IsHealer(captureId))
        {
            int heal = GetHealAmount(captureId);
            player?.Heal(heal);
            message = $"{captureId} \u4e3a\u4e3b\u89d2\u56de\u590d {heal} \u70b9\u751f\u547d\u3002";
            return true;
        }

        int damage = GetAttack(captureId);
        enemy?.TakeDamage(damage);
        message = $"{captureId} \u52a9\u6218\u653b\u51fb\uff0c\u9020\u6210 {damage} \u70b9\u4f24\u5bb3\u3002";
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
