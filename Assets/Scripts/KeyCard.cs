using UnityEngine;

public class Keycard : MonoBehaviour
{
    [Header("Efectos Visuales y Sonido")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private AudioClip pickupSound;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void Collect()
    {
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCard();
        }

        gameObject.SetActive(false);
    }
}