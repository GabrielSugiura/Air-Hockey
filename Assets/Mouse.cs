using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Mouse : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Camera mainCamera;

    [Header("Limites do Campo do Jogador")]
    [SerializeField] private float minX = -4.5f;
    [SerializeField] private float maxX = 4.5f;
    [SerializeField] private float minY = -4.9f;
    [SerializeField] private float maxY = 0f;

    private Rigidbody2D rb2d;
    private Vector2 lastValidTarget;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();

        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (mainCamera == null) mainCamera = Camera.main;

        lastValidTarget = rb2d.position;
    }

    private void FixedUpdate()
    {
        // Usando a API antiga (Input.mousePosition) em vez do New Input
        // System's Pointer.current. Há bugs documentados e recorrentes de
        // leitura de posição do mouse instável no Linux com o New Input
        // System (valores zerados aleatoriamente, ou "vazando" para posições
        // erradas). A API antiga não sofre desse problema.
        Vector3 mouseScreen = Input.mousePosition;

        Vector3 mouseWorld3D = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreen.x,
            mouseScreen.y,
            Mathf.Abs(mainCamera.transform.position.z)
        ));

        Vector2 targetPosition = mouseWorld3D;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        Debug.Log($"RAW World Y (antes do clamp): {mouseWorld3D.y} | mouseScreen.y: {mouseScreen.y} | Screen.height: {Screen.height}");
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        Debug.Log($"World Y: {targetPosition.y} | minY: {minY} | maxY: {maxY}");

        // Camada extra de segurança: ignora leituras exatamente em (0,0) de
        // tela, que é o valor clássico de "glitch" desse bug — a chance de o
        // jogador estar genuinamente no pixel (0,0) da tela é desprezível.
        if (mouseScreen.x == 0f && mouseScreen.y == 0f)
        {
            rb2d.MovePosition(lastValidTarget);
            return;
        }

        lastValidTarget = targetPosition;
        rb2d.MovePosition(targetPosition);
    }
}