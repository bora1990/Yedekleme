using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Yedekleme
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            //// Uygulamayı yönetici olarak başlatma kontrolü
            //if (!IsRunAsAdmin())
            //{
            //    // Eğer yönetici olarak başlatılmadıysa, uygulamayı yeniden yönetici olarak başlat
            //    RestartAsAdmin();
            //    // Uygulamayı kapat
            //    return;
            //}

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        //Uygulamanın yönetici olarak başlatılıp başlatılmadığını kontrol eden metot
        //private static bool IsRunAsAdmin()
        //{
        //    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        //}

        //Uygulamayı yönetici olarak yeniden başlatan metot
        //private static void RestartAsAdmin()
        //{
        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = Application.ExecutablePath,
        //        UseShellExecute = true,
        //        Verb = "runas" // Bu, uygulamanın yönetici olarak başlatılmasını sağlar
        //    };

        //    try
        //    {
        //        Process.Start(startInfo);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Uygulama yönetici olarak başlatılamadı: " + ex.Message);
        //    }
        //}
    }
}

