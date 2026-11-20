using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class FogVFXController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VisualEffect fogVFX;

    [Header("Nombres en el Blackboard de VFX_Fog")]
    [Tooltip("Dejar vacío si el bloque Spawn de VFX_Fog no usa un evento personalizado.")]
    [SerializeField] private string startEventName = "OnSceneStart";
    [SerializeField] private string stopEventName = "OnStop";
    [SerializeField] private string spawnRateProperty = "SpawnRate";
    [SerializeField] private string colorProperty = "Color";
    [SerializeField] private string alphaProperty = "Alpha";

    [Header("Valores de Niebla Normal")]
    [SerializeField] private float normalSpawnRate = 15f;
    [SerializeField] private Color normalFogColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float normalAlpha = 0.6f;

    private void Awake()
    {
        if (fogVFX == null)
        {
            fogVFX = GetComponent<VisualEffect>();
        }
    }

    private void Start()
    {
        ApplyFogSettings(normalSpawnRate, normalFogColor, normalAlpha);
        PlayFog();
    }

    public void PlayFog()
    {
        if (fogVFX == null) return;

        if (!string.IsNullOrEmpty(startEventName))
        {
            fogVFX.SendEvent(startEventName);
        }
        else
        {
            fogVFX.Play();
        }
    }

    public void StopFog()
    {
        if (fogVFX == null) return;

        if (!string.IsNullOrEmpty(stopEventName))
        {
            fogVFX.SendEvent(stopEventName);
        }
        else
        {
            fogVFX.Stop();
        }
    }

    public void ApplyFogSettings(float spawnRate, Color color, float alpha)
    {
        if (fogVFX == null) return;

        if (fogVFX.HasFloat(spawnRateProperty))
        {
            fogVFX.SetFloat(spawnRateProperty, spawnRate);
        }

        if (fogVFX.HasVector4(colorProperty))
        {
            fogVFX.SetVector4(colorProperty, color);
        }

        if (fogVFX.HasFloat(alphaProperty))
        {
            fogVFX.SetFloat(alphaProperty, alpha);
        }
    }

    public void ResetNormalFog()
    {
        ApplyFogSettings(normalSpawnRate, normalFogColor, normalAlpha);
    }
}