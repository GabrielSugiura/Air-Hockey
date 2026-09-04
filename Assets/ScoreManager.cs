using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Gerencia a pontuação do jogo e o reset do puck após um gol.

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Pontuação")]
    [SerializeField] private int redScore = 0;
    [SerializeField] private int blueScore = 0;

    [Header("UI (opcional — pode deixar vazio se ainda não tiver UI)")]
    [SerializeField] private Text redScoreText;
    [SerializeField] private Text blueScoreText;

    [Header("Puck")]
    [SerializeField] private Rigidbody2D puck;
    [SerializeField] private Vector2 puckStartPosition = Vector2.zero;
    [Tooltip("Tempo (segundos) que o puck fica 'sumido' antes de reaparecer no centro após um gol.")]
    [SerializeField] private float resetDelay = 1f;

    private void Awake()
    {
        // Garante que só existe uma instância do ScoreManager na cena.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void RegisterGoal(GoalOwner scoredOnSide)
    {
        if (scoredOnSide == GoalOwner.Red)
        {
            // Puck entrou no gol vermelho: azul marcou.
            blueScore++;
        }
        else
        {
            // Puck entrou no gol azul: vermelho marcou.
            redScore++;
        }

        UpdateUI();
        StartCoroutine(ResetPuckAfterDelay());
    }

    private void UpdateUI()
    {
        if (redScoreText != null) redScoreText.text = redScore.ToString();
        if (blueScoreText != null) blueScoreText.text = blueScore.ToString();
    }

    private IEnumerator ResetPuckAfterDelay()
    {
        if (puck == null) yield break;

        puck.gameObject.SetActive(false);

        yield return new WaitForSeconds(resetDelay);

        puck.position = puckStartPosition;
        puck.linearVelocity = Vector2.zero;
        puck.angularVelocity = 0f;

        puck.gameObject.SetActive(true);
    }
}