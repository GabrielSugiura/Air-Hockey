using UnityEngine;

/// <summary>
/// IA do mallet do oponente para Air Hockey (2D top-down).
/// Usa uma máquina de estados simples para se comportar de forma mais
/// natural do que apenas "grudar no gol e defender":
///
///   - Patrulha: quando o puck está longe, do lado do jogador, e não
///     representa ameaça imediata. A IA balança levemente perto da
///     posição defensiva, acompanhando o X do puck com folga.
///   - Defesa: quando o puck está vindo em direção ao gol da IA, ela
///     calcula onde o puck vai cruzar a linha do gol e se posiciona lá
///     antecipadamente.
///   - Ataque: quando o puck está no território da IA e "parado" o
///     suficiente pra valer a pena, ela se posiciona do lado oposto ao
///     gol do jogador e avança através do puck, empurrando-o na direção
///     certa.
///   - Retorno: por um curto período depois de acertar o puck, ela volta
///     pra posição defensiva em vez de ficar "grudada" no ataque,
///     evitando ficar exposta a contra-ataques.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AI : MonoBehaviour
{
    private enum State { Patrol, Defend, Attack, Return }

    [Header("Referências")]
    [SerializeField] private Rigidbody2D puck;

    [Header("Limites do Campo da IA")]
    [SerializeField] private float minX = -4.5f;
    [SerializeField] private float maxX = 4.5f;
    [SerializeField] private float minY = 0.5f;   // linha central do campo
    [SerializeField] private float maxY = 6.9f;   // fundo do lado da IA

    [Header("Gols (para mirar o ataque e prever a defesa)")]
    [SerializeField] private Vector2 ownGoalPosition = new Vector2(0f, 7.2f);
    [SerializeField] private Vector2 playerGoalPosition = new Vector2(0f, -7.2f);

    [Header("Dificuldade")]
    [Tooltip("Velocidade máxima de deslocamento normal (patrulha/defesa).")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("Velocidade ao atacar, geralmente mais rápida que o movimento normal.")]
    [SerializeField] private float attackSpeed = 12f;

    [Tooltip("Tempo (segundos) entre cada 'decisão' da IA. Valores mais altos simulam reação mais lenta/humana.")]
    [SerializeField] private float reactionDelay = 0.08f;

    [Tooltip("Erro de mira em unidades de mundo, somado aleatoriamente ao ponto de ataque. 0 = mira perfeita.")]
    [SerializeField] private float aimError = 0.3f;

    [Header("Comportamento")]
    [Tooltip("A partir de quanto o puck precisa estar dentro do território da IA (contando a partir de minY) para valer a pena atacar.")]
    [SerializeField] private float attackZoneDepth = 2.5f;

    [Tooltip("Velocidade máxima do puck para a IA considerar seguro atacar (puck muito rápido = deixa passar/defende).")]
    [SerializeField] private float maxPuckSpeedToAttack = 6f;

    [Tooltip("Distância atrás do puck (do lado oposto ao gol do jogador) de onde a IA parte para golpear.")]
    [SerializeField] private float approachDistance = 0.6f;

    [Tooltip("Amplitude do balanço lateral durante a patrulha.")]
    [SerializeField] private float patrolAmplitude = 1.5f;

    [Tooltip("Velocidade do balanço lateral durante a patrulha.")]
    [SerializeField] private float patrolFrequency = 0.5f;

    [Tooltip("Duração (segundos) do estado de retorno após acertar o puck.")]
    [SerializeField] private float returnDuration = 0.4f;

    private Rigidbody2D rb2d;
    private State currentState = State.Patrol;
    private Vector2 currentTarget;
    private float reactionTimer;
    private float returnTimer;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();

        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        currentTarget = ClampToBounds(ownGoalPosition);
    }

    private void FixedUpdate()
    {
        if (puck == null) return;

        // Simula tempo de reação: só recalcula a decisão a cada
        // "reactionDelay" segundos, em vez de todo frame.
        reactionTimer -= Time.fixedDeltaTime;
        if (reactionTimer <= 0f)
        {
            reactionTimer = reactionDelay;
            UpdateState();
            currentTarget = CalculateTargetForState();
        }

        float speed = currentState == State.Attack ? attackSpeed : moveSpeed;
        Vector2 newPosition = Vector2.MoveTowards(rb2d.position, currentTarget, speed * Time.fixedDeltaTime);
        rb2d.MovePosition(newPosition);
    }

    private void UpdateState()
    {
        // Estado de retorno tem prioridade temporária após um golpe.
        if (currentState == State.Return)
        {
            returnTimer -= reactionDelay;
            if (returnTimer > 0f) return;
        }

        bool puckInAiTerritory = puck.position.y > minY;
        float depthInTerritory = puck.position.y - minY;
        bool puckSlowEnough = puck.linearVelocity.magnitude <= maxPuckSpeedToAttack;
        bool puckHeadingToOwnGoal = IsPuckHeadingTowardGoal(ownGoalPosition);

        if (puckHeadingToOwnGoal)
        {
            currentState = State.Defend;
        }
        else if (puckInAiTerritory && depthInTerritory >= attackZoneDepth && puckSlowEnough)
        {
            currentState = State.Attack;
        }
        else
        {
            currentState = State.Patrol;
        }
    }

    private Vector2 CalculateTargetForState()
    {
        switch (currentState)
        {
            case State.Defend:
                return ClampToBounds(PredictInterceptOnGoalLine());

            case State.Attack:
                return ClampToBounds(CalculateAttackApproachPoint());

            case State.Return:
                return ClampToBounds(ownGoalPosition);

            case State.Patrol:
            default:
                return ClampToBounds(CalculatePatrolPoint());
        }
    }

    /// <summary>
    /// Estima onde o puck vai cruzar a linha Y do próprio gol da IA,
    /// projetando a trajetória atual dele em linha reta.
    /// </summary>
    private Vector2 PredictInterceptOnGoalLine()
    {
        Vector2 puckPos = puck.position;
        Vector2 puckVel = puck.linearVelocity;

        if (Mathf.Abs(puckVel.y) < 0.01f)
        {
            // Puck quase parado: só acompanha o X dele na altura de defesa.
            return new Vector2(puckPos.x, ownGoalPosition.y - 0.8f);
        }

        float timeToReachGoalLine = (ownGoalPosition.y - puckPos.y) / puckVel.y;
        timeToReachGoalLine = Mathf.Max(timeToReachGoalLine, 0f);

        float interceptX = puckPos.x + puckVel.x * timeToReachGoalLine;
        return new Vector2(interceptX, ownGoalPosition.y - 0.8f);
    }

    /// <summary>
    /// Calcula um ponto do lado oposto ao gol do jogador, "atrás" do puck,
    /// para que ao avançar em direção ao puck a IA o empurre para o gol
    /// adversário em vez de apenas encostar nele sem direção.
    /// </summary>
    private Vector2 CalculateAttackApproachPoint()
    {
        Vector2 puckPos = puck.position;
        Vector2 directionToPlayerGoal = (playerGoalPosition - puckPos).normalized;

        // Ponto de aproximação: do lado oposto à direção do gol do jogador.
        Vector2 approachPoint = puckPos - directionToPlayerGoal * approachDistance;

        // Erro de mira: desloca aleatoriamente o alvo, simulando imprecisão.
        if (aimError > 0f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * aimError;
            approachPoint += randomOffset;
        }

        return approachPoint;
    }

    private Vector2 CalculatePatrolPoint()
    {
        float sway = Mathf.Sin(Time.time * patrolFrequency) * patrolAmplitude;
        float trackedX = Mathf.Lerp(0f, puck.position.x, 0.3f); // acompanha o X do puck com folga
        return new Vector2(trackedX + sway, ownGoalPosition.y - 1.2f);
    }

    /// <summary>
    /// Verifica se, seguindo a velocidade atual, o puck vai cruzar a
    /// linha Y do gol informado dentro dos limites X do gol (aproximado
    /// pela largura do campo). Usado para decidir se entra em modo Defesa.
    /// </summary>
    private bool IsPuckHeadingTowardGoal(Vector2 goalPosition)
    {
        Vector2 puckVel = puck.linearVelocity;
        bool movingTowardGoalY = Mathf.Sign(goalPosition.y - minY) == Mathf.Sign(puckVel.y) && Mathf.Abs(puckVel.y) > 0.5f;

        if (!movingTowardGoalY) return false;

        Vector2 puckPos = puck.position;
        float timeToReachGoalLine = (goalPosition.y - puckPos.y) / puckVel.y;
        if (timeToReachGoalLine < 0f) return false;

        float interceptX = puckPos.x + puckVel.x * timeToReachGoalLine;
        return interceptX >= minX && interceptX <= maxX;
    }

    private Vector2 ClampToBounds(Vector2 position)
    {
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (puck != null && collision.rigidbody == puck)
        {
            // Depois de acertar o puck, volta pra posição defensiva por um
            // instante em vez de continuar avançando, evitando ficar
            // exposta a um contra-ataque imediato.
            currentState = State.Return;
            returnTimer = returnDuration;
        }
    }
}