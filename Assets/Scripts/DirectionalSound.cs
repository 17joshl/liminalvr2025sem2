using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DirectionalSound : MonoBehaviour
{
    [Header("Player Reference")]
    public Transform playerCamera; // Usually the XR headset or main camera

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float minVolume = 0f;
    [Range(0f, 1f)] public float maxVolume = 1f;
    public float fadeSpeed = 2f;

    [Header("Angle Settings")]
    [Tooltip("Angle (in degrees) where the sound starts to fade out as player turns away.")]
    [Range(0f, 180f)] public float fadeStartAngle = 45f;

    [Tooltip("Angle (in degrees) where the sound is fully faded out (player is looking away).")]
    [Range(0f, 180f)] public float fadeEndAngle = 90f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    void Update()
    {
        if (playerCamera == null) return;

        Vector3 toSound = (transform.position - playerCamera.position).normalized;
        Vector3 playerForward = playerCamera.forward;

        // Get the angle between the player's forward direction and the sound
        float angle = Vector3.Angle(playerForward, toSound);

        // Determine how much to fade based on angle range
        float t = Mathf.InverseLerp(fadeEndAngle, fadeStartAngle, angle);
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, t);

        // Smooth transition
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
    }
}
