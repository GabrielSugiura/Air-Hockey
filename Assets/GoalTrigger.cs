using UnityEngine;


public enum GoalOwner { Red, Blue }

[RequireComponent(typeof(Collider2D))]
public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private GoalOwner owner;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"O Collider2D de {gameObject.name} não está marcado como 'Is Trigger'. Marcando automaticamente.");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Compara pela tag "puck", que já está configurada no seu puck_0.
        if (!other.CompareTag("puck")) return;

        if (ScoreManager.Instance == null)
        {
            Debug.LogError("Nenhum ScoreManager encontrado na cena. Crie um GameObject com o script ScoreManager.");
            return;
        }

        ScoreManager.Instance.RegisterGoal(owner);
    }
}