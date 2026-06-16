using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ARBookBattleController : MonoBehaviour
{
    public ARBookPresentationSession session;
    public ARBookPresentationCameraRig cameraRig;
    public ARBookBattleCombatant player;
    public ARBookBattleCombatant enemy;
    public GameObject introHiddenOpponent;
    public GameObject battleUiRoot;
    public GameObject battleControlsRoot;
    public TMP_Text messageText;
    public Button battleAttackButton;
    public Button battleExitButton;
    public Button companionAButton;
    public Button companionBButton;
    public TMP_Text companionAButtonText;
    public TMP_Text companionBButtonText;

    [Header("Timing")]
    [Min(0f)] public float introDuration = 2.5f;
    [Min(0f)] public float attackImpactDelay = 0.6f;
    [Min(0f)] public float counterAttackDelay = 0.8f;
    [Min(0f)] public float finishDelay = 1.5f;

    [Header("Events")]
    public UnityEvent onBattleStarted;
    public UnityEvent onPlayerVictory;
    public UnityEvent onPlayerDefeat;
    public UnityEvent onBattleExited;

    public bool IsRunning { get; private set; }
    public bool IsBusy { get; private set; }

    private Coroutine battleRoutine;
    private string[] battleParty = new string[2];
    public event Action<bool> BattleFinished;

    [ContextMenu("Begin Battle")]
    public void BeginBattle()
    {
        if (battleRoutine != null || IsRunning)
        {
            return;
        }

        ResolveControlsRoot();
        ResolveAssistButtons();
        RefreshAssistButtons();
        battleRoutine = StartCoroutine(BeginBattleRoutine());
    }

    public void PlayerAttack()
    {
        if (!IsRunning || IsBusy || player == null || enemy == null)
        {
            return;
        }

        StartCoroutine(PlayerTurnRoutine());
    }

    public void CompanionAAttack()
    {
        CompanionAction(0);
    }

    public void CompanionBAttack()
    {
        CompanionAction(1);
    }

    public void SetBattleParty(string[] party)
    {
        battleParty = party ?? new string[2];
        RefreshAssistButtons();
    }

    public void ExitBattle()
    {
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        StopAllCoroutines();
        IsRunning = false;
        IsBusy = false;
        HideBattleUi();

        session?.Exit();
        BattleFinished?.Invoke(false);
        onBattleExited?.Invoke();
    }

    private IEnumerator BeginBattleRoutine()
    {
        IsBusy = true;
        HideBattleUi();
        SetMessage("\u6218\u6597\u5f00\u59cb");

        if (session != null)
        {
            yield return session.Enter();
        }

        ShowBattleUi(false);

        player?.ResetCombatant();
        enemy?.ResetCombatant();
        player?.PlayEntry();

        if (introHiddenOpponent != null)
        {
            introHiddenOpponent.SetActive(false);
        }

        onBattleStarted?.Invoke();
        if (cameraRig != null)
        {
            yield return cameraRig.PlayBattleIntro();
        }
        else
        {
            yield return new WaitForSecondsRealtime(introDuration);
        }

        if (introHiddenOpponent != null)
        {
            introHiddenOpponent.SetActive(true);
        }

        enemy?.PlayEntry();
        ShowBattleUi(true);
        SetMessage("\u9009\u62e9\u884c\u52a8");
        IsRunning = true;
        IsBusy = false;
        SetControlsInteractable(true);
        battleRoutine = null;
    }

    public void SetIntroOpponent(GameObject opponent)
    {
        introHiddenOpponent = opponent;
    }

    private IEnumerator PlayerTurnRoutine()
    {
        IsBusy = true;

        SetControlsInteractable(false);

        SetMessage($"{player.displayName} \u53d1\u52a8\u653b\u51fb");
        player.PlayAttack();
        yield return new WaitForSecondsRealtime(attackImpactDelay);
        enemy.TakeDamage(player.attackPower);

        if (enemy.IsDefeated)
        {
            yield return FinishBattleRoutine(true);
            yield break;
        }

        yield return new WaitForSecondsRealtime(counterAttackDelay);
        SetMessage($"{enemy.displayName} \u53d1\u52a8\u53cd\u51fb");
        enemy.PlayAttack();
        yield return new WaitForSecondsRealtime(attackImpactDelay);
        player.TakeDamage(enemy.attackPower);

        if (player.IsDefeated)
        {
            yield return FinishBattleRoutine(false);
            yield break;
        }

        SetMessage("\u9009\u62e9\u884c\u52a8");
        SetControlsInteractable(true);

        IsBusy = false;
    }

    private void CompanionAction(int index)
    {
        if (!IsRunning || IsBusy || player == null || enemy == null)
        {
            return;
        }

        string captureId =
            battleParty != null && index >= 0 && index < battleParty.Length
                ? battleParty[index]
                : string.Empty;
        StartCoroutine(CompanionTurnRoutine(captureId));
    }

    private IEnumerator CompanionTurnRoutine(string captureId)
    {
        if (!ARBookCompanionBattleRoster.TryUseAction(
            captureId,
            player,
            enemy,
            out string actionMessage))
        {
            SetMessage(actionMessage);
            RefreshAssistButtons();
            yield break;
        }

        IsBusy = true;
        SetControlsInteractable(false);

        SetMessage(actionMessage);
        yield return new WaitForSecondsRealtime(attackImpactDelay);

        if (enemy.IsDefeated)
        {
            yield return FinishBattleRoutine(true);
            yield break;
        }

        yield return new WaitForSecondsRealtime(counterAttackDelay);
        SetMessage($"{enemy.displayName} 发动反击");
        enemy.PlayAttack();
        yield return new WaitForSecondsRealtime(attackImpactDelay);
        player.TakeDamage(enemy.attackPower);

        if (player.IsDefeated)
        {
            yield return FinishBattleRoutine(false);
            yield break;
        }

        SetMessage("选择行动");
        SetControlsInteractable(true);

        RefreshAssistButtons();
        IsBusy = false;
    }

    private IEnumerator FinishBattleRoutine(bool playerWon)
    {
        IsRunning = false;

        if (playerWon)
        {
            ARBookCompanionBattleRoster.SpendMoodForPartyAfterBattle();
            player.PlayCaptureSuccess();
            SetMessage("\u6218\u6597\u80dc\u5229");
            onPlayerVictory?.Invoke();
        }
        else
        {
            ARBookCompanionBattleRoster.SpendMoodForPartyAfterBattle();
            enemy.PlayVictory();
            SetMessage("\u6218\u6597\u5931\u8d25");
            onPlayerDefeat?.Invoke();
        }

        yield return new WaitForSecondsRealtime(finishDelay);
        ExitBattleWithResult(playerWon);
    }

    private void ExitBattleWithResult(bool playerWon)
    {
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        IsRunning = false;
        IsBusy = false;
        HideBattleUi();

        session?.Exit();
        BattleFinished?.Invoke(playerWon);
        onBattleExited?.Invoke();
    }

    private void ShowBattleUi(bool showControls)
    {
        if (battleUiRoot != null)
        {
            battleUiRoot.SetActive(true);
        }

        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(showControls);
        }

        RefreshAssistButtons();
    }

    private void HideBattleUi()
    {
        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(false);
        }

        if (battleUiRoot != null)
        {
            battleUiRoot.SetActive(false);
        }
    }

    private void SetControlsActive(bool active)
    {
        ResolveControlsRoot();
        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(active);
        }
    }

    private void SetControlsInteractable(bool interactable)
    {
        ResolveAssistButtons();
        if (battleAttackButton != null)
        {
            battleAttackButton.interactable = interactable;
        }

        if (battleExitButton != null)
        {
            battleExitButton.interactable = interactable;
        }

        RefreshAssistButtons();
        if (!interactable)
        {
            if (companionAButton != null)
            {
                companionAButton.interactable = false;
            }

            if (companionBButton != null)
            {
                companionBButton.interactable = false;
            }
        }
    }

    private void SetMessage(string value)
    {
        if (messageText != null)
        {
            messageText.text = value;
        }
    }

    private void ResolveAssistButtons()
    {
        ResolveControlsRoot();
        if (battleControlsRoot == null)
        {
            return;
        }

        battleAttackButton = ResolveNamedOrExistingButton(
            battleControlsRoot.transform,
            "AttackButton",
            battleAttackButton);
        battleExitButton = ResolveNamedOrExistingButton(
            battleControlsRoot.transform,
            "ExitButton",
            battleExitButton);
        companionAButton = FindButton(battleControlsRoot.transform, "CompanionAButton");
        companionBButton = FindButton(battleControlsRoot.transform, "CompanionBButton");
        companionAButtonText =
            companionAButton != null
                ? companionAButton.GetComponentInChildren<TMP_Text>(true)
                : null;
        companionBButtonText =
            companionBButton != null
                ? companionBButton.GetComponentInChildren<TMP_Text>(true)
                : null;

        WireAssistButton(companionAButton, CompanionAAttack);
        WireAssistButton(companionBButton, CompanionBAttack);
    }

    private void ResolveControlsRoot()
    {
        if (battleControlsRoot == null)
        {
            return;
        }

        Transform controls = FindChildRecursive(
            battleControlsRoot.transform,
            "BattleControls");
        if (controls != null && controls.gameObject != battleControlsRoot)
        {
            battleControlsRoot = controls.gameObject;
        }
    }

    private void RefreshAssistButtons()
    {
        string a = battleParty != null && battleParty.Length > 0
            ? battleParty[0]
            : string.Empty;
        string b = battleParty != null && battleParty.Length > 1
            ? battleParty[1]
            : string.Empty;

        RefreshAssistButton(companionAButton, companionAButtonText, "A", a);
        RefreshAssistButton(companionBButton, companionBButtonText, "B", b);
    }

    private static void RefreshAssistButton(
        Button button,
        TMP_Text text,
        string slot,
        string captureId)
    {
        if (text != null)
        {
            text.text = ARBookCompanionBattleRoster.GetActionLabel(
                slot,
                captureId);
        }

        if (button != null)
        {
            if (button.name != "CompanionAButton" &&
                button.name != "CompanionBButton")
            {
                return;
            }

            button.interactable = ARBookCompanionBattleRoster.CanBattle(captureId);
        }
    }

    private static Button FindButton(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == name)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private static Button ResolveNamedOrExistingButton(
        Transform root,
        string name,
        Button existing)
    {
        Button named = FindButton(root, name);
        if (named != null)
        {
            return named;
        }

        if (existing != null &&
            existing.name != "CompanionAButton" &&
            existing.name != "CompanionBButton")
        {
            return existing;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void WireAssistButton(
        Button button,
        UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
