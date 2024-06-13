using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Yedekleme
{
    public partial class Plan : Form
    {
       
        public Plan()
        {
            InitializeComponent();
        }
  

        public void Plan_Load(object sender, EventArgs e)
        {
            //checkedListBox1.Items.Add("Monday");
            //checkedListBox1.Items.Add("Tuesday");
            //checkedListBox1.Items.Add("Wednesday");
            //checkedListBox1.Items.Add("Thursday");
            //checkedListBox1.Items.Add("Friday");
            //checkedListBox1.Items.Add("Saturday");
            //checkedListBox1.Items.Add("Sunday");

            //
            string[] turkishDays = { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };

            for (int i = 0; i < 7; i++)
            {
                checkedListBox1.Items.Add(turkishDays[i]);
            }

            maskedTextBox1.Text = Convert.ToString(DateTime.Now.TimeOfDay);


        }

     
        private void checkBoxTumu1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTumu1.Checked)
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

        private void button1_Click(object sender, EventArgs e)
        {
            


            var suankiSaat = DateTime.Now.ToString("HH:mm");
            var kuruluSaat = maskedTextBox1.Text.ToString();
            List<string> gunler = new List<string>();
            foreach (var item in checkedListBox1.CheckedItems)
            {
                gunler.Add(item.ToString());

            }

			string dosyaYolu = AppDomain.CurrentDomain.BaseDirectory + "Ayarlar.ini" ; 

            string saat=kuruluSaat;
			string iniIcerik = "[ZamanAyarları]\r\n";		
			iniIcerik += "KuruluSaat=" + kuruluSaat + "\r\n";
			iniIcerik += "Gunler=" + string.Join(",", gunler) + "\r\n";

			try
			{
				using (StreamWriter dosya = new StreamWriter(dosyaYolu))
				{
					dosya.WriteLine(iniIcerik);
				}
				
			}
	
			catch (Exception ex)
			{
				// Diğer hatalar için burası çalışır
				MessageBox.Show("Beklenmeyen bir hata oluştu: " + ex.Message);
			}

			

			this.Close();
        }

	}
}
