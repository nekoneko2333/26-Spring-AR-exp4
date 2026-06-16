using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

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
    public event Action<bool> BattleFinished;

    [ContextMenu("Begin Battle")]
    public void BeginBattle()
    {
        if (battleRoutine != null || IsRunning)
        {
            return;
        }

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
        battleRoutine = null;
    }

    public void SetIntroOpponent(GameObject opponent)
    {
        introHiddenOpponent = opponent;
    }

    private IEnumerator PlayerTurnRoutine()
    {
        IsBusy = true;

        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(false);
        }

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
        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(true);
        }

        IsBusy = false;
    }

    private IEnumerator FinishBattleRoutine(bool playerWon)
    {
        IsRunning = false;

        if (playerWon)
        {
            player.PlayCaptureSuccess();
            SetMessage("\u6218\u6597\u80dc\u5229");
            onPlayerVictory?.Invoke();
        }
        else
        {
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

    private void SetMessage(string value)
    {
        if (messageText != null)
        {
            messageText.text = value;
        }
    }
}
