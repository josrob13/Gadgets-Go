using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField, Tooltip("Nickname del jugador")] private string playerName = "Player";

    [Header("Player Components")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteraction playerInteraction;

    private void Awake()
    {
        playerMovement = playerMovement ?? GetComponent<PlayerMovement>();
        playerInteraction = playerInteraction ?? GetComponent<PlayerInteraction>();
    }

    void Update()
    {
        playerMovement.UpdateMovement();
        playerInteraction.KeyInteract();
    }

    private void FixedUpdate()
    {
        playerMovement.FixedMovement();
    }

    private void HandleMovement()
    {
        
    }
}
