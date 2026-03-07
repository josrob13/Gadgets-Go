using UnityEngine;

// Renombramos la clase para mantener consistencia en tu arquitectura VR
public class PlayerVR : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField, Tooltip("Nickname del jugador")] private string playerName = "Player VR";

    [Header("Player Components")]
    [SerializeField] private PlayerInteraction playerInteraction;

    private void Awake()
    {
        // Buena práctica: asegurarnos de obtener la referencia si se nos olvidó arrastrarla en el Inspector
        playerInteraction = playerInteraction ?? GetComponent<PlayerInteraction>();
        Debug.Log("PlayerVR script started.");
        Debug.Log("Player Name: " + playerName);
    }

    private void Update()
    {
        // Delegamos la lectura de los botones de los mandos de Quest al script de interacción
        if (playerInteraction != null)
        {
            playerInteraction.KeyInteract();
        }
    }
}