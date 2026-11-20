using UnityEngine;

public class ProximityNoise : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemy;
    [SerializeField] private Material noiseMaterial;

    [Header("Distancias de Activación")]
    [Tooltip("Distancia a partir de la cual empieza a aparecer la interferencia")]
    [SerializeField] private float maxDistance = 15f;

    [Tooltip("Distancia donde la interferencia llega a su nivel máximo")]
    [SerializeField] private float minDistance = 2f;

    [Header("Configuración del Shader")]
    [Tooltip("Reference Name exacto de la propiedad en Shader Graph")]
    [SerializeField] private string propertyName = "_Nitidez";

    [Tooltip("Valor de RuidoMultiply cuando el enemigo está lejos")]
    [SerializeField] private float minNoise = 0f;

    [Tooltip("Valor de RuidoMultiply cuando el enemigo está pegado")]
    [SerializeField] private float maxNoise = 1f;

    private int propertyID;

    private void Awake()
    {
        propertyID = Shader.PropertyToID(propertyName);

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (enemy == null)
        {
            GameObject e = GameObject.FindGameObjectWithTag("Enemy");
            if (e != null) enemy = e.transform;
        }
    }

    private void Update()
    {
        if (player == null || enemy == null || noiseMaterial == null) return;

        float distance = Vector3.Distance(player.position, enemy.position);

        float factor = 1f - Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));

        float currentNoise = Mathf.Lerp(minNoise, maxNoise, factor);

        noiseMaterial.SetFloat(propertyID, currentNoise);
    }

    private void OnDisable()
    {
        if (noiseMaterial != null)
        {
            noiseMaterial.SetFloat(propertyID, minNoise);
        }
    }
}