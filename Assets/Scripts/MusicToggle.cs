using UnityEngine;
using UnityEngine.UI;

public class MusicToggle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("AudioSource yang memutar musik background.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Komponen Image pada tombol yang icon-nya akan diganti.")]
    [SerializeField] private Image buttonIconImage;

    [Header("Icons")]
    [Tooltip("Sprite icon saat musik ON (misal: Gambar Speaker Bunyi).")]
    [SerializeField] private Sprite iconMusicOn;

    [Tooltip("Sprite icon saat musik OFF (misal: Gambar Speaker Mute/Silang).")]
    [SerializeField] private Sprite iconMusicOff;

    private Button _button;

    private void Awake()
    {
        // Otomatis mengambil komponen Button dari GameObject ini
        _button = GetComponent<Button>();

        if (_button != null)
        {
            _button.onClick.AddListener(ToggleMusic);
        }
    }

    private void Start()
    {
        // Update tampilan icon sesuai status awal AudioSource saat game mulai
        UpdateUI();
    }

    /// <summary>
    /// Membalikkan status mute/unmute pada AudioSource dan memperbarui UI.
    /// </summary>
    public void ToggleMusic()
    {
        if (musicSource == null) return;

        // Toggle status mute
        musicSource.mute = !musicSource.mute;

        // Perbarui icon
        UpdateUI();
    }

    /// <summary>
    /// Mengubah icon tombol berdasarkan kondisi AudioSource.
    /// </summary>
    private void UpdateUI()
    {
        if (musicSource == null || buttonIconImage == null) return;

        // Jika mute == true, berarti musik OFF (pakai icon OFF).
        // Jika mute == false, berarti musik ON (pakai icon ON).
        buttonIconImage.sprite = musicSource.mute ? iconMusicOff : iconMusicOn;
    }
}