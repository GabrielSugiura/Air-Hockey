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

    [Tooltip("Tempo que o puck fica escondido antes de reaparecer.")]
    [SerializeField] private float resetDelay = 1f;

    [Header("Posição de Spawn")]
    [Tooltip("Posição exata onde o puck deve reaparecer após cada gol.")]
    [SerializeField] private Vector2 puckStartPosition = Vector2.zero;

    private Coroutine resetCoroutine;

    private void Awake()
    {
        // Garante que exista apenas um ScoreManager na cena.
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
        // Impede que o mesmo gol seja contado várias vezes
        // enquanto o puck está sendo resetado.
        if (resetCoroutine != null)
            return;

        if (scoredOnSide == GoalOwner.Red)
        {
            // Puck entrou no gol vermelho -> azul marcou.
            blueScore++;
        }
        else
        {
            // Puck entrou no gol azul -> vermelho marcou.
            redScore++;
        }

        UpdateUI();

        resetCoroutine = StartCoroutine(ResetPuckAfterDelay());
    }

    private void UpdateUI()
    {
        if (redScoreText != null)
            redScoreText.text = redScore.ToString();

        if (blueScoreText != null)
            blueScoreText.text = blueScore.ToString();
    }

    private IEnumerator ResetPuckAfterDelay()
    {
        if (puck == null)
        {
            Debug.LogError("ScoreManager: O campo 'Puck' não está configurado!");
            resetCoroutine = null;
            yield break;
        }

        
        puck.linearVelocity = Vector2.zero;
        puck.angularVelocity = 0f;
        

        puck.gameObject.SetActive(false);

        // Aguarda antes de iniciar o próximo ponto.
        yield return new WaitForSeconds(resetDelay);
        

        Vector3 spawnPosition = new Vector3(
            puckStartPosition.x,
            puckStartPosition.y,
            puck.transform.position.z
        );

        puck.transform.position = spawnPosition;

        

        puck.linearVelocity = Vector2.zero;
        puck.angularVelocity = 0f;

        puck.gameObject.SetActive(true);
        
        puck.transform.position = spawnPosition;

        // Garante que o puck não carregue movimento do gol anterior.
        puck.linearVelocity = Vector2.zero;
        puck.angularVelocity = 0f;

        Debug.Log(
            "PUCK RESETADO! Posição: " +
            puck.transform.position
        );

        resetCoroutine = null;
    }
}
