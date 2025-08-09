using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "World", menuName = "Scriptable Objects/World")]
public class World : ScriptableObject
{
    [SerializeField] private string worldName;
    [SerializeField] private string sceneName;
    [SerializeField] private List<Mission> missions;

    public List<Mission> Missions => missions;
    public string WorldName => worldName;
    public string SceneName => sceneName;

    public void StartWorld()
    {
        // Aquí puedes inicializar el mundo, cargar misiones, etc.
        Debug.Log($"Starting world: {worldName}");
    }
}
