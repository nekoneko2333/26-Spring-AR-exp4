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

        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(false);
        }

        session?.Exit();
        BattleFinished?.Invoke(false);
        onBattleExited?.Invoke();
    }

    private IEnumerator BeginBattleRoutine()
    {
        IsBusy = true;
        SetMessage("战斗开始");

        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(false);
        }

        if (session != null)
        {
            yield return session.Enter();
        }

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

        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(true);
        }

        SetMessage("选择行动");
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

        SetMessage($"{player.displayName} 发动攻击");
        player.PlayAttack();
        yield return new WaitForSecondsRealtime(attackImpactDelay);
        enemy.TakeDamage(player.attackPower);

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
            SetMessage("战斗胜利");
            onPlayerVictory?.Invoke();
        }
        else
        {
            enemy.PlayVictory();
            SetMessage("战斗失败");
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

        if (battleControlsRoot != null)
        {
            battleControlsRoot.SetActive(false);
        }

        session?.Exit();
        BattleFinished?.Invoke(playerWon);
        onBattleExited?.Invoke();
    }

    private void SetMessage(string value)
    {
        if (messageText != null)
        {
            messageText.text = value;
        }
    }
}
