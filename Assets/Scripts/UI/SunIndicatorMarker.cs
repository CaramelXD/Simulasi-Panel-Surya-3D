using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Moves a sun marker along a UILineRenderer path based on SunController's timeOfDay.
/// Also updates a percentage label showing current sun intensity based on sunrise-sunset sine curve.
/// </summary>
public class SunIndicatorMarker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The UILineRenderer that defines the sun path curve.")]
    [SerializeField] private UILineRenderer sunPathLine;

    [Tooltip("The SunController providing the current time of day.")]
    [SerializeField] private SunController sunController;

    [Tooltip("TMP label to display the current sun intensity percentage.")]
    [SerializeField] private TMP_Text percentageLabel;

    [Header("Time Range")]
    [Tooltip("The hour represented by the start of the line (leftmost waypoint).")]
    [SerializeField] private float startHour = 5f;

    [Tooltip("The hour represented by the end of the line (rightmost waypoint).")]
    [SerializeField] private float endHour = 19f;

    [Header("Sun Intensity Range")]
    [Tooltip("Sunrise hour for intensity calculation (intensity = 0% at this hour).")]
    [SerializeField] private float sunriseHour = 6f;

    [Tooltip("Sunset hour for intensity calculation (intensity = 0% at this hour).")]
    [SerializeField] private float sunsetHour = 18f;

    [Header("Visibility")]
    [Tooltip("Hide the marker when timeOfDay is outside the start-end range.")]
    [SerializeField] private bool hideOutsideRange = true;

    private RectTransform _rectTransform;
    private Image _image;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    void Start()
    {
        if (sunController == null)
            sunController = FindFirstObjectByType<SunController>();

        if (sunPathLine == null)
            sunPathLine = GetComponentInParent<UILineRenderer>();

        if (percentageLabel == null)
        {
            // Auto-find the TMP_Text sibling under the same parent
            Transform parent = transform.parent;
            if (parent != null)
            {
                foreach (Transform sibling in parent)
                {
                    if (sibling == transform) continue;
                    var tmp = sibling.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        percentageLabel = tmp;
                        break;
                    }
                }
            }
        }
    }

    void Update()
    {
        if (sunController == null || sunPathLine == null) return;

        float timeOfDay = sunController.TimeOfDay;
        float range = endHour - startHour;

        // Calculate progress (0-1) along the line based on current time
        float progress = (timeOfDay - startHour) / range;

        bool isInRange = progress >= 0f && progress <= 1f;

        // Hide/show marker based on time range
        if (_image != null && hideOutsideRange)
            _image.enabled = isInRange;

        // Calculate sun intensity using sunrise-sunset sine curve
        // sin((timeOfDay - sunrise) / (sunset - sunrise) * PI)
        // Hour 6 = 0%, Hour 7 ≈ 26%, Hour 12 = 100%, Hour 18 = 0%
        float percentage = CalculateSunIntensityPercentage(timeOfDay);

        // Update percentage label
        if (percentageLabel != null)
            percentageLabel.text = $"{percentage:F0}%";

        if (!isInRange) return;

        // Get position on the spline and move marker
        Vector2 localPos = sunPathLine.GetPositionAtProgress(progress);
        Vector3 worldPos = sunPathLine.LocalToWorld(localPos);
        _rectTransform.position = worldPos;
    }

    /// <summary>
    /// Calculates the sun intensity percentage based on a sine curve between sunrise and sunset.
    /// Returns 0 outside the sunrise-sunset range.
    /// </summary>
    private float CalculateSunIntensityPercentage(float timeOfDay)
    {
        if (timeOfDay <= sunriseHour || timeOfDay >= sunsetHour)
            return 0f;

        float sunDuration = sunsetHour - sunriseHour;
        float progressInSunlight = (timeOfDay - sunriseHour) / sunDuration;

        // Sine curve: 0 at sunrise, peaks at solar noon, 0 at sunset
        float intensity = Mathf.Sin(progressInSunlight * Mathf.PI);

        return Mathf.Clamp(intensity * 100f, 0f, 100f);
    }
}
