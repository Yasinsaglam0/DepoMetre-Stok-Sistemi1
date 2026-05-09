using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace proje
{
    public partial class Form1 : Form
    {
        // Eski satırı sil, bunu yapıştır:
        SqlConnection baglanti = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='C:\Users\LENOVO\source\repos\proje\proje\Database1.mdf';Integrated Security=True");
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) { Listele(); IstatistikGuncelle(); }
         


        void Listele()
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM URUNLER", baglanti);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dataGridView1.DataSource = dt;
                baglanti.Close();
                KritikKontrol();
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                baglanti.Open();
                SqlCommand komut = new SqlCommand("INSERT INTO URUNLER (urun_ad, kategori, stok_miktari, birim, kritik_esik) VALUES (@p1, @p2, @p3, @p4, @p5)", baglanti);
                komut.Parameters.AddWithValue("@p1", textBox1.Text);
                komut.Parameters.AddWithValue("@p2", comboBox1.Text);
                komut.Parameters.AddWithValue("@p3", numericUpDown1.Value);
                komut.Parameters.AddWithValue("@p4", comboBox2.Text);
                komut.Parameters.AddWithValue("@p5", numericUpDown2.Value);
                komut.ExecuteNonQuery();
                baglanti.Close();
                MessageBox.Show("Ürün Kaydedildi!");
                Listele();
                IstatistikGuncelle();
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); baglanti.Close(); }
        }
        void KritikKontrol()
        {
            foreach (DataGridViewRow satir in dataGridView1.Rows)
            {
                if (satir.Cells["stok_miktari"].Value != null && satir.Cells["kritik_esik"].Value != null)
                {
                    decimal stok = Convert.ToDecimal(satir.Cells["stok_miktari"].Value);
                    decimal esik = Convert.ToDecimal(satir.Cells["kritik_esik"].Value);
                    if (stok <= esik)
                    {
                        satir.DefaultCellStyle.BackColor = Color.Red;
                        satir.DefaultCellStyle.ForeColor = Color.White;
                    }
                }
            }
        }
        void IstatistikGuncelle()
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                // 1. Toplam kaç çeşit ürün var?
                SqlCommand cmd1 = new SqlCommand("SELECT COUNT(*) FROM URUNLER", baglanti);
                int toplamUrun = (int)cmd1.ExecuteScalar();
                label7.Text = "Toplam Çeşit: " + toplamUrun;

                // 2. Kaç ürün kritik seviyede?
                SqlCommand cmd2 = new SqlCommand("SELECT COUNT(*) FROM URUNLER WHERE stok_miktari <= kritik_esik", baglanti);
                int kritikUrun = (int)cmd2.ExecuteScalar();
                label8.Text = "Acil Sipariş: " + kritikUrun;

                // Kritik ürün varsa yazıyı kırmızı yapıp dikkat çekelim
                if (kritikUrun > 0) label8.ForeColor = Color.Red;
                else label8.ForeColor = Color.Green;

                // 3. Toplam kaç birim ürün var?
                // 3. Toplam birim bazlı stok miktarları
                SqlCommand cmd3 = new SqlCommand("SELECT birim, SUM(stok_miktari) FROM URUNLER GROUP BY birim", baglanti);
                SqlDataReader dr = cmd3.ExecuteReader();

                string stokOzeti = "";
                while (dr.Read())
                {
                    // Örn: "25 Kg / 500 Adet / 40 Litre" gibi bir yazı oluşturur
                    stokOzeti += dr[1].ToString() + " " + dr[0].ToString() + " / ";
                }
                dr.Close();

                // Sonundaki fazla " / " işaretini temizleyip yazdıralım
                if (stokOzeti.Length > 3)
                    label9.Text = "Stok Dağılımı: " + stokOzeti.Substring(0, stokOzeti.Length - 3);
                else
                    label9.Text = "Stok Dağılımı: 0";

                baglanti.Close();
            }
            catch { baglanti.Close(); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // DataGridView'da seçili bir satır var mı kontrol et
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    // Seçili satırdaki urun_id'yi alıyoruz
                    int seciliId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["urun_id"].Value);
                    string urunAd = dataGridView1.SelectedRows[0].Cells["urun_ad"].Value.ToString();

                    // Kullanıcıya onay soralım (Yanlışlıkla silmemek için)
                    DialogResult onay = MessageBox.Show(urunAd + " adlı ürünü silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (onay == DialogResult.Yes)
                    {
                        baglanti.Open();
                        SqlCommand silKomutu = new SqlCommand("DELETE FROM URUNLER WHERE urun_id = @id", baglanti);
                        silKomutu.Parameters.AddWithValue("@id", seciliId);
                        silKomutu.ExecuteNonQuery();
                        baglanti.Close();

                        MessageBox.Show("Ürün başarıyla silindi.");
                        Listele(); // Listeyi tazele ki silinen ürün gitsin
                        IstatistikGuncelle();
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen silmek istediğiniz ürünün satırına (en soluna) tıklayarak seçin.");
                }
            }
            catch (Exception ex)
            {
                if (baglanti.State == ConnectionState.Open) baglanti.Close();
                MessageBox.Show("Silme hatası: " + ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    int seciliId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["urun_id"].Value);

                    baglanti.Open();
                    // SQL'e "Git bu ID'li ürünün bilgilerini kutucuklardaki yeni değerlerle değiştir" diyoruz.
                    SqlCommand guncelleKomutu = new SqlCommand("UPDATE URUNLER SET urun_ad=@p1, kategori=@p2, stok_miktari=@p3, birim=@p4, kritik_esik=@p5 WHERE urun_id=@id", baglanti);

                    guncelleKomutu.Parameters.AddWithValue("@p1", textBox1.Text);
                    guncelleKomutu.Parameters.AddWithValue("@p2", comboBox1.Text);
                    guncelleKomutu.Parameters.AddWithValue("@p3", numericUpDown1.Value);
                    guncelleKomutu.Parameters.AddWithValue("@p4", comboBox2.Text);
                    guncelleKomutu.Parameters.AddWithValue("@p5", numericUpDown2.Value);
                    guncelleKomutu.Parameters.AddWithValue("@id", seciliId);

                    guncelleKomutu.ExecuteNonQuery();
                    baglanti.Close();

                    MessageBox.Show("Stok bilgileri başarıyla güncellendi!");
                    Listele(); // Tabloyu tazele
                    IstatistikGuncelle();
                }
                else
                {
                    MessageBox.Show("Lütfen güncellemek istediğiniz ürünün satırını seçin.");
                }
            }
            catch (Exception ex)
            {
                if (baglanti.State == ConnectionState.Open) baglanti.Close();
                MessageBox.Show("Güncelleme hatası: " + ex.Message);
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            textBox1.Text = dataGridView1.CurrentRow.Cells["urun_ad"].Value.ToString();
            comboBox1.Text = dataGridView1.CurrentRow.Cells["kategori"].Value.ToString();
            numericUpDown1.Value = Convert.ToDecimal(dataGridView1.CurrentRow.Cells["stok_miktari"].Value);
            comboBox2.Text = dataGridView1.CurrentRow.Cells["birim"].Value.ToString();
            numericUpDown2.Value = Convert.ToDecimal(dataGridView1.CurrentRow.Cells["kritik_esik"].Value);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                // SQL'deki LIKE komutu ile içinde yazdığımız harfler geçen ürünleri arıyoruz
                // % işareti "başına veya sonuna ne gelirse gelsin" demektir
                string sorgu = "SELECT * FROM URUNLER WHERE urun_ad LIKE @arama";

                SqlDataAdapter da = new SqlDataAdapter(sorgu, baglanti);
                da.SelectCommand.Parameters.AddWithValue("@arama", "%" + textBox2.Text + "%");

                DataTable dt = new DataTable();
                da.Fill(dt);
                dataGridView1.DataSource = dt;

                baglanti.Close();

                // Tablo her süzüldüğünde renkleri (kritik stok) tekrar kontrol etmeliyiz
                KritikKontrol();
            }
            catch (Exception ex)
            {
                if (baglanti.State == ConnectionState.Open) baglanti.Close();
                MessageBox.Show("Arama hatası: " + ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (baglanti.State == ConnectionState.Closed) baglanti.Open();

                // Sihirli sorgumuz burada: stok_miktari, kritik_esik'ten küçük veya eşit olanları seç
                string sorgu = "SELECT * FROM URUNLER WHERE stok_miktari <= kritik_esik";

                SqlDataAdapter da = new SqlDataAdapter(sorgu, baglanti);
                DataTable dt = new DataTable();
                da.Fill(dt);

                // Eğer hiç eksik ürün yoksa kullanıcıya bilgi verelim
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Harika! Şu an stoğu bitmek üzere olan bir ürün bulunmuyor.", "Stok Durumu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Listele(); // Tüm listeyi geri getir
                }
                else
                {
                    dataGridView1.DataSource = dt;
                    KritikKontrol(); // Gelenleri yine kırmızı boyayalım ki vurgulu olsun
                    MessageBox.Show(dt.Rows.Count + " adet ürün kritik seviyede! Sipariş vermeniz gerekebilir.", "Eksik Listesi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                baglanti.Close();
            }
            catch (Exception ex)
            {
                if (baglanti.State == ConnectionState.Open) baglanti.Close();
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
