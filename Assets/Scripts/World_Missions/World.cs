using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "World", menuName = "Scriptable Objects/World")]
public class World : ScriptableObject
{
    public string worldName;
    public List<Mission> missions;

    public void StartWorld()
    {
        // Aquí puedes inicializar el mundo, cargar misiones, etc.
        Debug.Log($"Starting world: {worldName}");
    }
}
