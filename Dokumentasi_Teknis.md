# Dokumentasi Teknis
## Simulator Panel Surya 3D (WebGL)

Dibuat untuk mempermudah pemahaman arsitektur proyek bagi tim pengembang dan asisten teknis.

---

### 1. Ikhtisar Proyek (Project Overview)
Proyek ini adalah simulator interaktif 3D berbasis web (dieksport menjadi **WebGL** dengan Unity Engine) yang dirancang untuk mensimulasikan penerapan sistem panel surya pada sebuah bangunan/rumah. Pengguna dapat memilih komponen beban listrik (seperti Kulkas, AC, dsb), meletakkannya di rumah, dan menghitung total beban arus listrik (Watt) secara *real-time*.

---

### 2. Arsitektur Inti Engine
- **Platform/Engine**: Unity 3D (Target Platform: WebGL)
- **Sistem Input**: Menggunakan `EventSystem` modern beralasan desain UI Web, menggantikan metode iterasi *Drag-and-Drop* tradisional dengan sistem **Katalog Dinamis** yang lebih intuitif & anti-bug di browser.
- **Data Management**: Menggunakan `ScriptableObject` (*Asset-based Database*) untuk melacak data kelistrikan yang modular dan ringan (tanpa perlu database eksternal seperti SQL/JSON).

---

### 3. Modul Manajemen Beban (Sistem Perabotan / Furniture Placement)
Ini adalah modul utama yang sudah berhasil diselesaikan (Fase Saat Ini). Sistem kelistrikan ditenagai oleh 4 komponen script utama (ditulis dengan bahasa *C#*):

#### a) `FurnitureData.cs` (Data Model)
Sebuah kelas struktur `ScriptableObject` yang mendefinisikan *Blueprint* dasar setiap perangkat elektronik yang ada.
- **Properti yang disimpan:**
  - `furnitureName` (String): Nama perabotan.
  - `icon` (Sprite): Ikon untuk antarmuka katalog.
  - `prefab3D` (GameObject): Model 3D aktual yang akan muncul di *scene*.
  - `wattConsumption` (float): Tingkat konsumsi daya (misal: 150 Watt).
  - `fixedPosition` & `fixedRotation` (Vector3): Koordinat kemunculan otomatis di dunia 3D sehingga objek selalu *snap* rapi di posisinya.

#### b) `FurnitureManager.cs` (Controller / Otak Utama Kelistrikan)
Manajer terpusat dengan pola *Singleton pattern* (hanya butuh 1 di udara) yang mengatur status siklus hidup fisik barang/elektronik di rumah.
- **Logika Sistem:**
  - Mengelola struktur data *Dictionary* untuk melacak perangkat elektronik apa saja yang sudah diletakkan.
  - **Auto-Calculate:** Menjumlahkan **Total Watt** beban konsumsi secara otomatis melalui *method* `AddFurniture(...)` atau mengurangi saat dipanggil `RemoveFurniture(...)`.
  - Melepas *Event trigger* bernama `OnPowerChanged` ke seluruh sistem jika ada perubahan beban, agar UI bar merespon dengan seketika.
  - Menyertakan rutinitas Coroutine animasi lompat *bounce* kecil setiap objek baru di-*instantiate* (dirender ke dunia 3D).

#### c) UI Controllers: `FurnitureCatalogUI.cs` & `FurnitureCardUI.cs`
Sistem antarmuka pemrograman Visual (Data-Driven UI) yang berjalan di `Canvas` Unity.
- **`FurnitureCatalogUI`**: Root panel. Bertugas membaca antrean `FurnitureData` dan men-*generate* tombol kartu (`FurnitureCardPrefab`) secara otomatis secara vertikal ke dalam komponen `Scroll View`. Ia mengendalikan **Power Bar visual** *(Indikator bar pengukur beban total)* beserta pewarnaan dinamis *(Hijau → Kuning → Merah)* yang membandingkan Total Watt vs Max Watt (Kapasitas Maksimal Rumah).
- **`FurnitureCardUI`**: Script mikrokontroller untuk masing-masing "kartu" perabotan. Ini membaca data spesifik, lalu menampilkan Watt/Teks, sambil mencocokkan status dari `FurnitureManager`. (Cth: Jika komponen belum diletakkan, tombol berwarna hijau "+ Masukan", tapi kalau sudah diletakkan di Scene, state tombol berubah seketika menjadi merah "X Hapus").

---

### 4. Rencana Tahapan Berikutnya (Next Pipelines)
Setelah fondasi arsitektur **Beban Daya (Wattage)** stabil, kode proyek disiapkan untuk integrasi modul logika berikutnya:
1. **Modul Hardware (Panel Surya, Baterai, Inverter)**: Menempatkan komponen energi di sekitar struktur bangunan.
2. **Sistem Wiring (Pengkabelan)**: Mekanisme garis sambungan daya (Drag Poin ke Poin) antara Panel ➔ Inverter ➔ Baterai / Beban Rumah.
3. **Simulasi Slider Waktu (Siang/Malam)**: Manipulator *Skybox Directional Light* yang tersambung dengan logaritma efisiensi sinar UV panel surya per jam untuk mensimulasikan efisiensi penangkapan daya di dunia nyata.

---

*(Dokumen teknis ini menunjang alur komunikasi antara tim developer dan instruktur proyek selama fase pengembangan berjalan)*
