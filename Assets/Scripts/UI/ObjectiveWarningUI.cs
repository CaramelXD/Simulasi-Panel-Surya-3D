using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dialog warning yang muncul saat user mencoba memulai simulasi
/// tetapi ada objective yang belum terpenuhi.
///
/// Dua pilihan:
///   - "Lanjutkan" → lanjut ke konfigurasi simulasi meski belum lengkap.
///   - "Kembali"   → tutup dialog, kembali ke scene normal.
///
/// Attach ke GameObject aktif di Canvas (misalnya Canvas itu sendiri).
/// Assign semua referensi UI via Inspector.
/// </summary>
public class ObjectiveWarningUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Root panel warning — disembunyikan saat awal, muncul saat ada objective belum selesai.")]
    [SerializeField] private GameObject warningPanel;

    [Header("Content")]
    [Tooltip("Label daftar objective yang belum terpenuhi.")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Buttons")]
    [Tooltip("Lanjutkan ke konfigurasi simulasi meski objective belum lengkap.")]
    [SerializeField] private Button proceedButton;

    [Tooltip("Tutup dialog dan kembali tanpa membuka konfigurasi.")]
    [SerializeField] private Button backButton;

    // Callback yang dipanggil ketika user memilih Lanjutkan.
    private Action _onProceed;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        AutoWire();
        Hide();

        if (proceedButton != null)
            proceedButton.onClick.AddListener(OnProceedClicked);

        if (backButton != null)
            backButton.onClick.AddListener(Hide);
    }

    /// <summary>
    /// Cari referensi UI secara otomatis berdasarkan nama GameObject,
    /// sehingga tidak perlu assign manual di Inspector.
    /// </summary>
    private void AutoWire()
    {
        if (warningPanel == null)
        {
            Transform t = transform.Find("ObjectiveWarningPanel");
            if (t != null) warningPanel = t.gameObject;
        }

        if (warningPanel == null)
        {
            Debug.LogWarning("[ObjectiveWarningUI] 'ObjectiveWarningPanel' tidak ditemukan sebagai child Canvas.");
            return;
        }

        if (messageText == null)
        {
            Transform msg = warningPanel.transform.Find("Card/Message");
            if (msg != null) messageText = msg.GetComponent<TextMeshProUGUI>();
        }

        if (proceedButton == null)
        {
            Transform btn = warningPanel.transform.Find("Card/BtnRow/BtnLanjutkan");
            if (btn != null) proceedButton = btn.GetComponent<Button>();
        }

        if (backButton == null)
        {
            Transform btn = warningPanel.transform.Find("Card/BtnRow/BtnKembali");
            if (btn != null) backButton = btn.GetComponent<Button>();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tampilkan dialog warning dengan daftar objective yang belum terpenuhi.
    /// <paramref name="onProceed"/> dipanggil jika user memilih "Lanjutkan".
    /// </summary>
    public void Show(List<string> incompleteObjectives, Action onProceed)
    {
        _onProceed = onProceed;

        if (messageText != null)
        {
            if (incompleteObjectives == null || incompleteObjectives.Count == 0)
            {
                messageText.text = "Ada objective yang belum terpenuhi.";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Objective berikut belum terpenuhi:");
                foreach (string obj in incompleteObjectives)
                    sb.AppendLine($"  • {obj}");
                sb.AppendLine("\nApakah kamu ingin tetap melanjutkan?");
                messageText.text = sb.ToString().TrimEnd();
            }
        }

        if (warningPanel != null)
            warningPanel.SetActive(true);
    }

    /// <summary>Sembunyikan dialog warning.</summary>
    public void Hide()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);

        _onProceed = null;
    }

    // ── Handlers ───────────────────────────────────────────────────────────

    private void OnProceedClicked()
    {
        Action callback = _onProceed;
        Hide();
        callback?.Invoke();
    }
}
