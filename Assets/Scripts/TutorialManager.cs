using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tombol Bantuan Universal")]
    public Button universalHelpButton;

    [System.Serializable]
    public class TutorialStep
    {
        public string tutorialID;
        public GameObject tutorialObject;
    }

    [Header("Daftar GameObject Tutorial")]
    public List<TutorialStep> tutorials = new List<TutorialStep>();

    private string currentActiveTutorialID = "Intro";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (universalHelpButton != null)
            universalHelpButton.onClick.AddListener(ShowCurrentTutorial);

        CloseAllTutorials();

        // Saat game mulai, otomatis cek dan munculkan tutorial Intro 
        // (tapi hanya jika ini pertama kalinya main)
        CheckAndAutoShow("Intro");
    }

    /// <summary>
    /// Fungsi Pintar untuk tombol Toggle (buka/tutup menu).
    /// </summary>
    public void ToggleTutorialContext(string sectionID)
    {
        if (currentActiveTutorialID == sectionID)
        {
            // Pemain sedang menutup menu, balikkan ke Intro.
            currentActiveTutorialID = "Intro";
            // Kita tidak perlu auto-show Intro saat menu ditutup biar tidak mengganggu.
        }
        else
        {
            // Pemain baru membuka menu ini.
            currentActiveTutorialID = sectionID;

            // Cek apakah tutorial menu ini belum pernah dilihat, jika belum: munculkan!
            CheckAndAutoShow(sectionID);
        }

        Debug.Log($"[Tutorial] Konteks tutorial beralih ke: {currentActiveTutorialID}");
    }

    /// <summary>
    /// Cek ke memori sistem. Kalau baru pertama kali, otomatis munculkan.
    /// </summary>
    private void CheckAndAutoShow(string id)
    {
        // Mengecek memori dengan nama "AutoTutorial_NamaID". 
        // Kalau nilainya 0 (belum ada), berarti ini pertama kalinya!
        if (PlayerPrefs.GetInt("AutoTutorial_" + id, 0) == 0)
        {
            // 1. Catat di memori bahwa pemain sudah melihatnya, jadi besok-besok tidak muncul otomatis lagi
            PlayerPrefs.SetInt("AutoTutorial_" + id, 1);
            PlayerPrefs.Save();

            // 2. Munculkan layarnya
            ShowCurrentTutorial();
        }
    }

    /// <summary>
    /// Fungsi ini dipakai oleh tombol Help (?). 
    /// Memaksa tutorial muncul, tidak peduli sudah pernah dilihat atau belum.
    /// </summary>
    public void ShowCurrentTutorial()
    {
        CloseAllTutorials();

        TutorialStep step = tutorials.Find(t => t.tutorialID == currentActiveTutorialID);

        if (step != null && step.tutorialObject != null)
        {
            step.tutorialObject.SetActive(true);
        }
    }

    public void CloseAllTutorials()
    {
        foreach (var step in tutorials)
        {
            if (step.tutorialObject != null)
            {
                step.tutorialObject.SetActive(false);
            }
        }
    }

    // ?? FITUR RAHASIA DEVELOPER ??????????????????????????????????????????????

    [ContextMenu("Reset Semua Auto-Tutorial")]
    public void ResetTutorialMemory()
    {
        // Fitur untuk menghapus ingatan PlayerPrefs saat kamu sedang testing di Unity
        PlayerPrefs.DeleteAll();
        Debug.Log("Memori tutorial telah di-reset! Semua tutorial akan muncul pop-up otomatis lagi.");
    }
}