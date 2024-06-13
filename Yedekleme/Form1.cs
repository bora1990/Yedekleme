﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Web.Services.Description;
using System.Net.Mail;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Windows.Shapes;
using Google.Apis.Upload;
using IniParser;
using IniParser.Model;
using Timer = System.Threading.Timer;
using System.Net;
using System.Windows.Controls;
using Yedekleme.Themes;

using Path = System.IO.Path;
using System.Configuration;
using System.Security.Cryptography;
using MetroFramework.Forms;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Security.Principal;
using DevExpress.XtraEditors;
using DevExpress.Utils;
using DevExpress.Utils.Gesture;




namespace Yedekleme
{
    public partial class Form1 : MetroForm
    {
        private SqlConnection _connection;
        private SqlCommand _command;
        private SqlDataReader _reader;
        string sql = "";
        string connectionstring = "";
        private bool isFirstRun = true;

        private string userId;
        private string pass;

        int isNull = 0;
        int driveBaglanti = 0;

        bool durum = false;

        private string PathToCredentials = AppDomain.CurrentDomain.BaseDirectory + "credentials.json";

        private static string[] Scopes = { DriveService.Scope.Drive };
        private static string appname = "Google Drive Upload";

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenuStrip;



        private readonly DriveService service;

        private BackgroundWorker backgroundWorker;

        public Form1()
        {

            //var saat = kuruluSaat;
            //string[] gunler = item;
            //gunler.

            InitializeComponent();
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);

            this.Load += new EventHandler(Form1_Load);

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true; // İlerleme raporlamasını etkinleştir
            backgroundWorker.DoWork += backgroundWorker_DoWork; // Arka planda yapılacak işi tanımla
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged; // İlerleme değişikliği için olayı tanımla
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted; // İş tamamlandığında yapılacakları tanımla
            backgroundWorker.RunWorkerAsync();

        }

        private void ApplyTheme(Color backColor, Color foreColor)
        {
            this.BackColor = backColor;
            foreach (System.Windows.Forms.Control ctrl in this.Controls)
            {
                ctrl.BackColor = backColor;
                ctrl.ForeColor = foreColor;
            }
        }

        private void OnLightThemeButtonClicked(object sender, EventArgs e)
        {


            ApplyTheme(Themes.Theme.LightBackColor, Themes.Theme.LightForeColor);
        }

        private void OnDarkThemeButtonClicked(object sender, EventArgs e)
        {
            ApplyTheme(Themes.Theme.DarkBackColor, Themes.Theme.DarkForeColor);
        }


        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pictureBox3.Hide();
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }
   
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
          

            DataTable sqlServerInstances = SqlDataSourceEnumerator.Instance.GetDataSources();

            foreach (DataRow row in sqlServerInstances.Rows)
            {
                string serverName = row["ServerName"].ToString();
                string instanceName = row["InstanceName"].ToString();

                if (instanceName != "")
                {
                    // UI bileşenlerine erişim, ana iş parçacığından sağlanmalıdır
                    cmbServerName.Invoke((MethodInvoker)delegate
                    {
                        cmbServerName.Items.Add($@"{serverName}\{instanceName}");
                    });
                }
                else
                {
                    // UI bileşenlerine erişim, ana iş parçacığından sağlanmalıdır
                    cmbServerName.Invoke((MethodInvoker)delegate
                    {
                        cmbServerName.Items.Add($"{serverName}");
                    });
                }
            }

           

        }

    
        private void OnApplicationExit(object sender, EventArgs e)
        {
            // Uygulama kapanırken, isFirstRun değişkenini sıfırla
            isFirstRun = true;
        }

        //private bool IsRunAsAdmin()
        //{
        //    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        //}

        private void Form1_Load(object sender, EventArgs e)
        {
           
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            regkey.SetValue("Yedekleme", Application.ExecutablePath);
            regkey.Close();

            folderIdKaydet.Visible = true;
            timer2.Enabled = false;

            InitializeNotifyIcon();
            InitializeContextMenuStrip();

            string dosyaYolu = AppDomain.CurrentDomain.BaseDirectory + "Ayarlar.ini";

            textBoxLocation.Text = AppDomain.CurrentDomain.BaseDirectory;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            folderIdtxt.Visible = false;


            Thread.Sleep(1000);
            //cmbServerName.Text = System.Net.Dns.GetHostName().ToUpper() + @"\SQLEXPRESS";
            //comboboxDatabase.SelectedIndex = 0;

            pictureBox1.Hide();

            #region PasswordKayıt
            string sifremiz = "KullanıcıAyarları";
            bool bolumSifre = false;
            string[] satirlar = File.ReadAllLines(dosyaYolu);

            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{sifremiz}]"))
                {
                    bolumSifre = true;
                    continue;
                }

                if (bolumSifre)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin

                        if (anahtar == "Sifre")
                        {
                            txtPassword.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }
            }

            #endregion

            #region KullanıcıAdı
            string kullaniciAdı = "KullanıcıAyarları";
            bool bolumKullanici = false;


            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{kullaniciAdı}]"))
                {
                    bolumKullanici = true;
                    continue;
                }

                if (bolumKullanici)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin

                        if (anahtar == "Kullanıcı")
                        {
                            textboxUser.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }
            }

            #endregion

            #region sunucuAdıKayıt

            string sunucuAdı = "SunucuAyarları";
            bool bolum = false;


            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{sunucuAdı}]"))
                {
                    bolum = true;
                    continue;
                }

                if (bolum)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin

                        if (anahtar == "Sunucu")
                        {
                            cmbServerName.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }
            }

            #endregion

            #region FolderIdKayıt

            string hedefBolum = "DosyaAyarları";
            bool hedefBolumde = false;


            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{hedefBolum}]"))
                {
                    hedefBolumde = true;
                    continue;
                }

                if (hedefBolumde)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin

                        if (anahtar == "FolderId")
                        {
                            folderIdtxt.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }




            }
            #endregion

            #region ZamanKayıt
            string hedefBolum1 = "ZamanAyarları";
            bool hedefBolumde1 = false;
            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{hedefBolum1}]"))
                {
                    hedefBolumde1 = true; // hedefBolumde1 değişkenini true olarak ayarlayın
                    continue;
                }

                // Eğer hedef bölümdeysek, değerleri alın
                if (hedefBolumde1)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin					

                        if (anahtar == "KuruluSaat")
                        {
                            textBox1.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }
            }
            #endregion

            #region Gunler Kayıt
            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{hedefBolum1}]"))
                {
                    hedefBolumde1 = true; // hedefBolumde1 değişkenini true olarak ayarlayın
                    continue;
                }

                // Eğer hedef bölümdeysek, değerleri alın
                if (hedefBolumde1)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin					

                        if (anahtar == "Gunler")
                        {
                            textBox2.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }
            }
            #endregion

            #region DosyaYolu
            string dosya = "DosyaYolu";
            bool varMi = false;
            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{dosya}]"))
                {
                    varMi = true; // hedefBolumde1 değişkenini true olarak ayarlayın
                    continue;
                }

                // Eğer hedef bölümdeysek, değerleri alın
                if (varMi)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin					

                        if (anahtar == "Yol")
                        {
                            textBoxLocation.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }
            }


            #endregion

            #region MailKayıt

            string dosya1 = "MailAdresi";
            bool varMi1 = false;
            foreach (string satir in satirlar)
            {
                // Eğer satır boşsa veya yorum satırıysa (örneğin ';' veya '#' ile başlıyorsa), atla
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith(";") || satir.StartsWith("#"))
                    continue;

                // Bölüm başlığına ulaşıldıysa, hedef bölümde olduğumuzu kontrol et
                if (satir.Trim().Equals($"[{dosya1}]"))
                {
                    varMi1 = true; // hedefBolumde1 değişkenini true olarak ayarlayın
                    continue;
                }

                // Eğer hedef bölümdeysek, değerleri alın
                if (varMi1)
                {
                    string[] parcalar = satir.Split('='); // "=" ile ayrılmış ayarları parçalayın

                    if (parcalar.Length == 2) // Doğru ayar uzunluğuna sahipse
                    {
                        string anahtar = parcalar[0].Trim(); // Anahtar kelimeyi alırken boşlukları temizleyin
                        string deger = parcalar[1].Trim(); // Değeri alırken boşlukları temizleyin					

                        if (anahtar == "Mail")
                        {
                            txtEmail.Text = deger;
                            break; // Aradığımız değeri bulduğumuzda döngüyü sonlandırabiliriz.
                        }
                    }
                }
            }

            #endregion



            #region AyarlarıEntegreEtmek
            if (cmbServerName.Text != "" && textboxUser.Text != "" && txtPassword.Text != "" && textBox1.Text != "" && textBox2.Text != "")
            {

                pictureBoxConnect_Click(sender, e);




                List<string> secilenAnahtarlar = new List<string>(); // İşaretlenecek anahtarlar listesi

                foreach (string satir in satirlar)
                {
                    // Satırı işleme
                    string[] parcalar = satir.Split('=');
                    if (parcalar.Length == 2)
                    {
                        // Anahtar ve değeri ayırma
                        string anahtar = parcalar[0].Trim();
                        string deger = parcalar[1].Trim();

                        if (deger == "True")
                        {
                            secilenAnahtarlar.Add(anahtar);
                        }
                    }
                }

                // Eğer hiç True değeri olan anahtar yoksa, uyarı ver
                if (secilenAnahtarlar.Count == 0)
                {
                    Console.WriteLine("True değeri olan bir anahtar bulunamadı!");
                    return; // İşlemi sonlandır
                }

                // CheckBoxList içindeki uygun öğeyi bulma ve işaretleme
                foreach (string anahtar in secilenAnahtarlar)
                {
                    int index = checkedListBox1.Items.IndexOf(anahtar);
                    if (index != -1)
                    {
                        checkedListBox1.SetItemChecked(index, true);
                    }
                }
                

                pictureBoxBackup_Click(pictureBoxBackup, EventArgs.Empty);


            }
            #endregion
            //folderIdKaydet.Enabled = false;
            textBox3.BackColor = Color.White;
            textBox3.ForeColor = Color.Black;
            textBox3.Text = "Copyright © Glopark ";
        }


        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + "program.ico"); // Icon dosyasının adını ve uzantısını buraya yazmalısınız
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            notifyIcon.Visible = true;
        }

        private void InitializeContextMenuStrip()
        {
            contextMenuStrip = new ContextMenuStrip();
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Çıkış");
            exitItem.Click += ExitItem_Click;
            contextMenuStrip.Items.Add(exitItem);
            notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }

       



        //Servera Bağlanma
        private void pictureBoxConnect_Click(object sender, EventArgs e)
        {
            
            try
            {

                if (textboxUser.Text == "" && txtPassword.Text == "")
                {
                    connectionstring = "Data Source=" + cmbServerName.Text.Trim() + ";Integrated Security=True;";
                    _connection = new SqlConnection(connectionstring);
                    _connection.Open();

                    cmbServerName.Enabled = false;
                    pictureBoxConnect.Visible = false;
                    pictureBoxDisconnect.Visible = true;

                    ConnectDisconnectVariation(sender, e);

                }
                else if (textboxUser.Text != "" && txtPassword.Text != "")
                {
                    try
                    {
                        connectionstring = "Data Source=" + cmbServerName.Text.Trim() + ";User Id=" + textboxUser.Text + ";Password=" + txtPassword.Text + ";";
                        _connection = new SqlConnection(connectionstring);
                        _connection.Open();
                        cmbServerName.Enabled = false;
                        ConnectDisconnectVariation(sender, e);


                    }
                    catch (Exception)
                    {
                        pictureBoxConnect.Enabled = true;
                        pictureBoxConnect.Visible = true;
                        pictureBoxDisconnect.Visible = false;
                        pictureBoxDisconnect.Enabled = true;
                    }
                                                   
                }
             
                      
                           
                
                           
                checkedListBox1.Items.Clear();
                sql = "select Name from master.sys.databases d WHERE d.database_id > 4; ";
                _command = new SqlCommand(sql, _connection);
                _reader = _command.ExecuteReader();

                while (_reader.Read())
                {
                    //comboboxDatabase.Items.Add(_reader[0].ToString());
                    checkedListBox1.Items.Add(_reader[0]);
                }
                _connection.Close();

              
            }
            catch (Exception)
            {
                pictureBoxConnect.Visible = true;
                pictureBoxDisconnect.Visible = false;
                MessageBox.Show("Bağlantı hatası","Hata",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }

            
            //BaglanBaglantıKoparButon(sender, e);

        }

        private void ConnectDisconnectVariation(object sender, EventArgs e)
        {
            PictureBox clickedPictureBox = sender as PictureBox; // Tıklanan PictureBox'ı al

            if (clickedPictureBox == pictureBoxConnect)
            {
                pictureBoxConnect.Visible = false;
                pictureBoxDisconnect.Visible = true;
            }
            else if (clickedPictureBox == pictureBoxDisconnect)
            {
                pictureBoxConnect.Visible = true;
                pictureBoxDisconnect.Visible = false;
            }
        }
        //Serverdan Bağlantı Koparma
        private void pictureBoxDisconnect_Click(object sender, EventArgs e)
        {
            cmbServerName.Enabled = true;
            pictureBoxConnect.Enabled = true;
            pictureBoxDisconnect.Enabled = false;

            cmbServerName.Items.Clear();
            DataTable sqlServerInstances = SqlDataSourceEnumerator.Instance.GetDataSources();


            foreach (DataRow row in sqlServerInstances.Rows)
            {
                string serverName = Convert.ToString(row["ServerName"]);
                string instanceName = Convert.ToString(row["InstanceName"]);

                if (instanceName != "")
                {

                    cmbServerName.Items.Add($"{serverName}\\{instanceName}");
                }
                else
                {
                    cmbServerName.Items.Add($"{instanceName}");
                }
            }

            cmbServerName.Enabled = true;

            textBoxLocation.Clear();

            MessageBox.Show("Sunucuyla bağlantı kesildi.");

            //textBoxLocation.Text = Application.StartupPath;

            checkedListBox1.Items.Clear();
            textBoxLocation.Enabled = true;
            cmbServerName.Enabled = true;

            PictureBox clickedPictureBox = sender as PictureBox; // Tıklanan PictureBox'ı al

            if (clickedPictureBox == pictureBoxDisconnect)
            {
                pictureBoxDisconnect.Visible = false;
                pictureBoxConnect.Visible = true;
            }


        }

        //Folder Açma
        public void pictureBoxBrowse_Click(object sender, EventArgs e)
        {


            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxLocation.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        //Ayarları Kaydet / Drivesız
        public void pictureBoxBackup_Click(object sender, EventArgs e)
        {

            AyarlariKaydet();

            string dosyaYolu = AppDomain.CurrentDomain.BaseDirectory + "Ayarlar.ini";

            string kuruluSaat = textBox1.Text;
            string iniIcerik = "[ZamanAyarları]\r\n";
            iniIcerik += "KuruluSaat=" + kuruluSaat + "\r\n";
            iniIcerik += "Gunler=" + textBox2.Text;



            try
            {
                using (StreamWriter dosya = new StreamWriter(dosyaYolu))
                {
                    dosya.WriteLine(iniIcerik);
                    for (int i = 0; i < checkedListBox1.Items.Count; i++)
                    {

                        // Her bir seçeneğin adını ve durumunu alın
                        var seçenek = checkedListBox1.Items[i].ToString();
                        var seçildiMi = checkedListBox1.GetItemChecked(i);

                        // INI dosyasına seçeneği ekleyin
                        dosya.WriteLine($"{seçenek}={seçildiMi}");
                    }
                }


            }
            catch (Exception ex)
            {
                // Diğer hatalar için burası çalışır
                MessageBox.Show("Beklenmeyen bir hata oluştu: " + ex.Message);
            }
            // .ini dosyasına veri ekleme işlevini çağırın



            AddDataToIniFile(dosyaYolu, "SunucuAyarları", "Sunucu", cmbServerName.Text);

            AddDataToIniFile(dosyaYolu, "KullanıcıAyarları", "Kullanıcı", textboxUser.Text);

            AddDataToIniFile(dosyaYolu, "SifreAyarları", "Sifre", txtPassword.Text);

            AddDataToIniFile(dosyaYolu, "DosyaAyarları", "FolderId", folderIdtxt.Text);

            AddDataToIniFile(dosyaYolu, "DosyaYolu", "Yol", textBoxLocation.Text);

            AddDataToIniFile(dosyaYolu, "MailAdresi", "Mail", txtEmail.Text);


            folderIdKaydet.Enabled = true;


            textBox3.Text = "Tüm ayarlar başarıyla kaydedilmiştir.";

        }

        private void checkBoxTumu_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTumu.Checked)
            {
                for (int item = 0; item <= checkedListBox1.Items.Count - 1; item++)
                {
                    checkedListBox1.SetItemChecked(item, true);
                }
            }
            else
            {
                for (int item = 0; item <= checkedListBox1.Items.Count - 1; item++)
                {
                    checkedListBox1.SetItemChecked(item, false);
                }
            }
        }

        //Plan Formunu Açma
        private void pictureBox2_Click(object sender, EventArgs e)
        {        
            Plan plan = new Plan();
            plan.ShowDialog();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            string[] turkishDays = { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };
            int dayOfWeekIndex = (int)DateTime.Now.DayOfWeek;

            // Eğer gün Pazar ise (0), indeksi 6 olarak ayarlayın
            // Aksi takdirde, indeksi 1 azaltarak doğru günü bulun
            int adjustedIndex = (dayOfWeekIndex == 0) ? 6 : dayOfWeekIndex - 1;

            string turkishDay = turkishDays[adjustedIndex];
       

            if (isNull == 0)
            {
                if (DateTime.Now.ToString("HH:mm") == textBox1.Text && textBox2.Text.Split(',').Contains(turkishDay))
                {
               

                    timer2.Enabled = true;

                    string dosyaYolu = AppDomain.CurrentDomain.BaseDirectory + "Ayarlar.ini";

                    Dictionary<string, Dictionary<string, string>> iniIcerik = new Dictionary<string, Dictionary<string, string>>();
                    string currentSection = "";


                    Thread.Sleep(2000);
                    // INI dosyasını satır satır oku
                    using (StreamReader dosya = new StreamReader(dosyaYolu))
                    {
                        string satir;

                        while ((satir = dosya.ReadLine()) != null)
                        {
                            satir = satir.Trim();

                            // Boş satırları atla
                            if (string.IsNullOrEmpty(satir))
                                continue;

                            // Bölüm başlığını kontrol et
                            if (satir.StartsWith("[") && satir.EndsWith("]"))
                            {
                                currentSection = satir.Substring(1, satir.Length - 2);
                                iniIcerik[currentSection] = new Dictionary<string, string>();
                                continue;
                            }

                            // Anahtar-değer çiftlerini ayır
                            string[] parcalar = satir.Split('=');
                            if (parcalar.Length == 2 && !string.IsNullOrEmpty(currentSection))
                            {
                                string anahtar = parcalar[0].Trim();
                                string deger = parcalar[1].Trim();
                                iniIcerik[currentSection][anahtar] = deger;
                            }
                        }
                    }
                    textBox1.Text = iniIcerik["ZamanAyarları"]["KuruluSaat"];
                    textBox2.Text = iniIcerik["ZamanAyarları"]["Gunler"];


                  
                        try
                        {
                            _connection = new SqlConnection(connectionstring);

                            _connection.Open();

                            if (checkedListBox1.Items.Count != 0)
                            {
                                foreach (object databasechecked in checkedListBox1.CheckedItems)
                                {
                                    string folder;
                                    folder = textBoxLocation + @"\" + Convert.ToString(databasechecked) + ".bak";


                                    sql = $@"BACKUP DATABASE {Convert.ToString(databasechecked)} TO DISK ='{textBoxLocation.Text.Trim()}\{Convert.ToString(databasechecked)} -{DateTime.Now.ToString("dddd, dd MMMM yyyy HH-mm-ss")}.bak'";

                                    _command = new SqlCommand(sql, _connection);
                                    _command.ExecuteNonQuery();


                                }
                                _connection.Close();
                                _connection.Dispose();


                                string files = textBoxLocation.Text;

                                string[] array1 = Directory.GetFiles(files, "*.bak");
                                foreach (string name in array1)
                                {
                                    string filename = System.IO.Path.GetFileName(name);
                                    string sZipFile = $@"{files}\{filename}.zip";
                                    using (FileStream _flStream = File.Open(sZipFile, FileMode.Create))
                                    {
                                        GZipStream obj = new GZipStream(_flStream, CompressionMode.Compress);
                                        byte[] bt = File.ReadAllBytes(name);
                                        obj.Write(bt, 0, bt.Length);
                                        obj.Close();
                                        obj.Dispose();

                                    }
                                }

                                string srcDir = textBoxLocation.Text;
                                string[] bakList = Directory.GetFiles(srcDir, "*.bak");

                                if (Directory.Exists(srcDir))
                                {
                                    foreach (string f in bakList)
                                    {
                                        File.Delete(f);
                                    }
                                }

                                          
                                timer1.Stop();
                                isNull = 1;

                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Location", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    
                }
            }

            if (isNull == 1)
            {
                if (folderIdtxt.Text != "")
                {
                    textBox3.BackColor = Color.White;
                    textBox3.ForeColor = Color.Black;

                    textBox3.Text = "Dosyalar Google Drive'a Gönderiliyor...";
                    Thread.Sleep(30000);
                    timer2.Interval = 45000;
                    timer2.Start();
                  
                }
                else
                {
                    textBox3.BackColor = Color.White;
                    textBox3.ForeColor = Color.Black;
                    textBox3.Text = "Copyright © Glopark ";
                    timer2.Stop();
                    Thread.Sleep(60000);
                    timer1.Start();
                    isNull = 0;
                }
            }
        }

        //Hemen Yedek Al
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                _connection = new SqlConnection(connectionstring);

                _connection.Open();
                //sql = @" BACKUP DATABASE " + comboboxDatabase.Text + " TO DISK ='" + textBoxLocation.Text.Trim() + "\\" +  comboboxDatabase.Text + "-" + DateTime.Now.ToString("dddd,dd MMMM yyyy HH-mm-ss") + ".bak'" ;
                if (checkedListBox1.Items.Count != 0)
                {
                    foreach (object databasechecked in checkedListBox1.CheckedItems)
                    {
                        string folder;
                        folder = textBoxLocation + @"\" + Convert.ToString(databasechecked) + ".bak";


                        sql = $@"BACKUP DATABASE {Convert.ToString(databasechecked)} TO DISK ='{textBoxLocation.Text.Trim()}\{Convert.ToString(databasechecked)} -{DateTime.Now.ToString("dddd, dd MMMM yyyy HH-mm-ss")}.bak'";

                        _command = new SqlCommand(sql, _connection);
                        _command.ExecuteNonQuery();


                    }
                    _connection.Close();
                    _connection.Dispose();


                    string files = textBoxLocation.Text;



                    string[] array1 = Directory.GetFiles(files, "*.bak");
                    foreach (string name in array1)
                    {
                        string filename = System.IO.Path.GetFileName(name);
                        string sZipFile = $@"{files}\{filename}.zip";
                        using (FileStream _flStream = File.Open(sZipFile, FileMode.Create))
                        {
                            GZipStream obj = new GZipStream(_flStream, CompressionMode.Compress);
                            byte[] bt = File.ReadAllBytes(name);
                            obj.Write(bt, 0, bt.Length);
                            obj.Close();
                            obj.Dispose();

                        }
                    }

                    string srcDir = textBoxLocation.Text;
                    string[] bakList = Directory.GetFiles(srcDir, "*.bak");

                    if (Directory.Exists(srcDir))
                    {
                        foreach (string f in bakList)
                        {
                            File.Delete(f);
                        }

                        MessageBox.Show("Başarıyla Yedek Alınmıştır.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    //_command = new SqlCommand(sql, _connection);
                    //_command.ExecuteNonQuery();
                    //_connection.Close();
                    //_connection.Dispose();

                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Location", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        bool durum1 = false;
        private async Task UploadFilesToDrive(string folderId)
        {


            // Google Drive'a erişim için kimlik doğrulaması yapılıyor
            var tokenStorage = new FileDataStore(folder: "token.json", fullPath: true);

            UserCredential credential;
            using (var stream = new FileStream(PathToCredentials, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { DriveService.ScopeConstants.Drive },
                    "user",
                    CancellationToken.None,
                    tokenStorage);
            }

            // Drive servisi oluşturuluyor
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });

            // Yüklemek istediğiniz dosyaların bulunduğu klasör
            string applicationFolder = textBoxLocation.Text;

            // Klasördeki tüm ".zip" dosyalarını al
            string[] zipFiles = Directory.GetFiles(applicationFolder, "*.zip");

            foreach (string zipFile in zipFiles)
            {
                // Dosyanın son değiştirilme zamanı kontrol ediliyor
                FileInfo fileInfo = new FileInfo(zipFile);
                TimeSpan timeSinceLastModified = DateTime.Now - fileInfo.LastWriteTime;

                // Son değiştirilme zamanı 2 dakikadan az olan dosyalar seçiliyor
                if (timeSinceLastModified.TotalMinutes <= 5)
                {
                    durum1 = true;
                   
                    // Dosyanın adı alınıyor
                    string fileName = System.IO.Path.GetFileName(zipFile);

                    // Dosya metadata oluşturuluyor
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File
                    {
                        Name = fileName,
                        Parents = new List<string> { folderId } // Dosyanın yükleneceği klasör ID'si
                    };

                    if (durum1 == true)
                    {
                    // Dosya yükleme işlemi başlatılıyor
                    using (var stream = new FileStream(zipFile, FileMode.Open))
                    {
                        var request = service.Files.Create(fileMetadata, stream, "application/zip");
                        request.Fields = "id";

                        // Dosya yükleme işlemi gerçekleştiriliyor
                        await request.UploadAsync();
                           
                    }

                    }

                    // Dosya yükleme işlemi tamamlandıktan sonra e-posta gönderiliyor
                }

            }
       
        }

        //Elle Drive'a Yedek Al
        private async void pictureBox1_Click(object sender, EventArgs e)
        {
            string folderId = folderIdtxt.Text;
            await UploadFilesToDrive(folderId);
        }

        //Credential Tanıtma Google Console Bağlanma
        private async void button2_Click(object sender, EventArgs e)
        {
            driveBaglanti = 1;
            DriveAccessFolder();
            //DriveAccessFolder();

            var tokenStorage = new FileDataStore(folder: "token.json", fullPath: true);

            UserCredential credential;
            using (var stream = new FileStream(PathToCredentials, FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = appname,
            });
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = "mimeType='application/vnd.google-apps.folder'";

            IList<Google.Apis.Drive.v3.Data.File> folders = listRequest.Execute().Files;

            CheckForIllegalCrossThreadCalls = false;

            foreach (var item in folders)
            {

                if (comboBox1.Text == item.Name)
                {
                    folderIdtxt.Text = item.Id;
                }

            }
        }

        static void AddDataToIniFile(string filePath, string sectionName, string key, string value)
        {

            // .ini dosyasını satır satır okuyun
            string[] lines = File.ReadAllLines(filePath);

            // Eğer bölüm yoksa, yeni bir bölüm ekleyin
            bool sectionFound = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("[" + sectionName + "]"))
                {
                    sectionFound = true;
                    break;
                }
            }
            if (!sectionFound)
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine("[" + sectionName + "]");
                }
            }

            // Anahtar ve değeri ekleyin
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(key + "=" + value);
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

            string folderId = folderIdtxt.Text;
            UploadFilesToDrive(folderId);
            Thread.Sleep(18000);
            textBox3.Text = "";
            textBox3.BackColor = Color.White;
            textBox3.ForeColor = Color.Black;
            textBox3.Text = "Copyright © Glopark ";
            isNull = 0;
            
            string mailTo = txtEmail.Text;
            string emailPattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
            Regex regex = new Regex(emailPattern);
            if (mailTo != null)
            {
                if (regex.IsMatch(mailTo))
                {
                    SendEmail(mailTo);
                }

            }
            timer2.Stop();
            timer1.Start();
        }
        /// <summary>
        /// Tüm Ayarları Kaydededer.
        /// </summary>
        private void AyarlariKaydet()
        {
            string dosyaYolu = AppDomain.CurrentDomain.BaseDirectory + "Ayarlar.ini"; // INI dosyasının yolu  

            Dictionary<string, Dictionary<string, string>> iniIcerik = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";

            // INI dosyasını satır satır oku
            using (StreamReader dosya = new StreamReader(dosyaYolu))
            {
                string satir;

                while ((satir = dosya.ReadLine()) != null)
                {
                    satir = satir.Trim();

                    // Boş satırları atla
                    if (string.IsNullOrEmpty(satir))
                        continue;

                    // Bölüm başlığını kontrol et
                    if (satir.StartsWith("[") && satir.EndsWith("]"))
                    {
                        currentSection = satir.Substring(1, satir.Length - 2);
                        iniIcerik[currentSection] = new Dictionary<string, string>();
                        continue;
                    }

                    // Anahtar-değer çiftlerini ayır
                    string[] parcalar = satir.Split('=');
                    if (parcalar.Length == 2 && !string.IsNullOrEmpty(currentSection))
                    {
                        string anahtar = parcalar[0].Trim();
                        string deger = parcalar[1].Trim();
                        iniIcerik[currentSection][anahtar] = deger;
                    }
                }
            }
            textBox1.Text = iniIcerik["ZamanAyarları"]["KuruluSaat"];
            textBox2.Text = iniIcerik["ZamanAyarları"]["Gunler"];

            //DateTime.Now.ToString("HH:mm") == textBox1.Text && DateTime.Now.DayOfWeek.ToString() == textBox2.Text
            //MessageBox.Show("Beklenmeyen bir hata oluştu: " + textBox2.Text.Split(',')[0] + "\n" + "day of week: " + textBox2.Text.Split(',').Contains(DateTime.Now.DayOfWeek.ToString()));
            string[] turkishDays = { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };
            int dayOfWeekIndex = (int)DateTime.Now.DayOfWeek;

            // Eğer gün Pazar ise (0), indeksi 6 olarak ayarlayın
            // Aksi takdirde, indeksi 1 azaltarak doğru günü bulun
            int adjustedIndex = (dayOfWeekIndex == 0) ? 6 : dayOfWeekIndex - 1;

            string turkishDay = turkishDays[adjustedIndex];
          
            // Pazar gününü doğru şekilde ele almak için indeksi ayarlayın

            if (DateTime.Now.ToString("HH:mm") == textBox1.Text && textBox2.Text.Split(',').Contains(turkishDay))
            {

                try
                {
                    _connection = new SqlConnection(connectionstring);

                    _connection.Open();
                    //sql = @" BACKUP DATABASE " + comboboxDatabase.Text + " TO DISK ='" + textBoxLocation.Text.Trim() + "\\" +  comboboxDatabase.Text + "-" + DateTime.Now.ToString("dddd,dd MMMM yyyy HH-mm-ss") + ".bak'" ;
                    if (checkedListBox1.Items.Count != 0)
                    {
                        foreach (object databasechecked in checkedListBox1.CheckedItems)
                        {
                            string folder;
                            folder = textBoxLocation + @"\" + Convert.ToString(databasechecked) + ".bak";


                            sql = $@"BACKUP DATABASE {Convert.ToString(databasechecked)} TO DISK ='{textBoxLocation.Text.Trim()}\{Convert.ToString(databasechecked)} -{DateTime.Now.ToString("dddd, dd MMMM yyyy HH-mm-ss")}.bak'";

                            _command = new SqlCommand(sql, _connection);
                            _command.ExecuteNonQuery();


                        }
                        _connection.Close();
                        _connection.Dispose();


                        string files = textBoxLocation.Text;



                        string[] array1 = Directory.GetFiles(files, "*.bak");
                        foreach (string name in array1)
                        {
                            string filename = System.IO.Path.GetFileName(name);
                            string sZipFile = $@"{files}\{filename}.zip";
                            using (FileStream _flStream = File.Open(sZipFile, FileMode.Create))
                            {
                                GZipStream obj = new GZipStream(_flStream, CompressionMode.Compress);
                                byte[] bt = File.ReadAllBytes(name);
                                obj.Write(bt, 0, bt.Length);
                                obj.Close();
                                obj.Dispose();

                            }
                        }

                        string srcDir = textBoxLocation.Text;
                        string[] bakList = Directory.GetFiles(srcDir, "*.bak");

                        if (Directory.Exists(srcDir))
                        {
                            foreach (string f in bakList)
                            {
                                File.Delete(f);
                            }
                        }

                        //_command = new SqlCommand(sql, _connection);
                        //_command.ExecuteNonQuery();
                        //_connection.Close();
                        //_connection.Dispose();

                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Location", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        /// <summary>
        /// Mail Yollama Butonu
        /// </summary>
        /// <param name="recipient">Hangi Maile Gideceğini Gösterir.</param>
        private void SendEmail(string recipient)
        {


            string senderEmail = "sqlserverbackup.glopark@gmail.com";
            string senderPassword = "ysohnngeanvklfeo";
            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587;

            SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
            smtpClient.EnableSsl = true;

            MailMessage mail = new MailMessage(senderEmail, recipient);
            mail.Subject = "Yedekleme İşlemi Hk.";

            var zaman = DateTime.Now.ToString("dddd dd/MM/yyyy hh:mm");
            mail.Body = $"{zaman} tarihinde yedekleme işlemi başarıyla gerçekleşmiştir.";


            smtpClient.Send(mail);
        }

        private void btnDeleteOldBackups_Click(object sender, EventArgs e)
        {


         
        }

        private void DeleteOldBackups(string directoryPath, int days)
        {
            
        
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
                if (!dirInfo.Exists)
                {
                    MessageBox.Show("Yedek Dosyası Mevcut Değil.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DateTime thresholdDate = DateTime.Now.AddDays(-days);

                var filesToDelete = dirInfo.GetFiles()
                                           .Where(file => file.CreationTime < thresholdDate)
                                           .ToList();

                foreach (var file in filesToDelete)
                {
                    file.Delete();
                }

                MessageBox.Show($"{filesToDelete.Count} eski yedek dosyalar silindi.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            }
        }



        /// <summary>
        /// Google Drivedaki klasörlere erişimi sağlar.
        /// </summary>
        private async void DriveAccessFolder()
        {
            var tokenStorage = new FileDataStore(folder: "token.json", fullPath: true);

            UserCredential credential;
            using (var stream = new FileStream(PathToCredentials, FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential
            });

            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = "mimeType='application/vnd.google-apps.folder'";

            IList<Google.Apis.Drive.v3.Data.File> folders = listRequest.Execute().Files;

            CheckForIllegalCrossThreadCalls = false;

            comboBox1.Items.Clear();

            foreach (var driveFiles in folders)
            {
                comboBox1.Items.Add(driveFiles.Name);
            }


        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tokenStorage = new FileDataStore(folder: "token.json", fullPath: true);

            UserCredential credential;
            using (var stream = new FileStream(PathToCredentials, FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }


        }

        private void btnDriveDisconnect_Click(object sender, EventArgs e)
        {
            
            // Uygulamanın çalıştığı dizini al
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Token klasörünün yolunu oluştur
            string tokenFolderPath = Path.Combine(appDirectory, "token.json");

            try
            {
                // Eğer dosya mevcutsa, sil
                if (Directory.Exists(tokenFolderPath))
                {
                    Directory.Delete(tokenFolderPath, true);
                   
                             
                    folderIdtxt.Text = "";
                    MessageBox.Show("Drive bağlantısı kesilmiştir.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    driveBaglanti = 0;
                }
                else
                {
                    MessageBox.Show("Drive zaten bağlı değil!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Drive bağlantısı kesilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void folderIdKaydet_Click(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Lütfen Drive Klasörü Seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else {
                if (driveBaglanti == 1) { 
            button2.PerformClick();

            MessageBox.Show("Kayıt başarıyla yapıldı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Drive bağlantısı yapınız.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void metroSetDefaultButton1_Click(object sender, EventArgs e)
        {
            OnDarkThemeButtonClicked(sender, e);

        }

        private void metroSetDefaultButton2_Click(object sender, EventArgs e)
        {
            OnLightThemeButtonClicked(sender, e);
        }

        private void button3_Click(object sender, EventArgs e)
        {
        


            string dosyaYeri = textBoxLocation.Text;

            string backupDirectory = dosyaYeri; // Yedekleme dizininizin yolu

            if (string.IsNullOrWhiteSpace(mskGunSil.Text) )
            {
                MessageBox.Show("Geçerli bir gün sayısı giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            int kacGun = Convert.ToInt32(mskGunSil.Text);



            DialogResult result = MessageBox.Show($"{kacGun} önceki tüm kayıtlar silinecektir. Emin misiniz?", "Uyarı", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

           if (result == DialogResult.OK)
                DeleteOldBackups(backupDirectory, kacGun);
            
        }
    }
}

