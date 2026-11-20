using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class CarSmokeController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VisualEffect smokeVFX;

    [Header("Nombres en Blackboard")]
    [SerializeField] private string spawnRateProperty = "SpawnRate";
    [SerializeField] private string colorProperty = "SmokeColor";
    [SerializeField] private string alphaProperty = "SmokeAlpha";

    [Header("Presets de Humo")]
    [SerializeField] private bool startActive = true;
    [SerializeField] private float lightSmokeRate = 12f;
    [SerializeField] private Color lightSmokeColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Space]
    [SerializeField] private float heavySmokeRate = 30f;
    [SerializeField] private Color heavySmokeColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private void Awake()
    {
        if (smokeVFX == null)
        {
            smokeVFX = GetComponent<VisualEffect>();
        }
    }

    private void Start()
    {
        if (startActive)
        {
            SetLightSmoke();
        }
        else
        {
            StopSmoke();
        }
    }

    public void SetLightSmoke()
    {
        ApplySmoke(lightSmokeRate, lightSmokeColor, 0.55f);
        smokeVFX.Play();
    }

    public void SetHeavyBreakdownSmoke()
    {
        ApplySmoke(heavySmokeRate, heavySmokeColor, 0.85f);
        smokeVFX.Play();
    }

    public void StopSmoke()
    {
        if (smokeVFX == null) return;
        smokeVFX.Stop();
    }

    private void ApplySmoke(float rate, Color color, float alpha)
    {
        if (smokeVFX == null) return;

        if (smokeVFX.HasFloat(spawnRateProperty))
            smokeVFX.SetFloat(spawnRateProperty, rate);

        if (smokeVFX.HasVector4(colorProperty))
            smokeVFX.SetVector4(colorProperty, color);

        if (smokeVFX.HasFloat(alphaProperty))
            smokeVFX.SetFloat(alphaProperty, alpha);
    }
}