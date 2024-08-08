using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using System.Net.Http;

namespace newlogin
{
    public partial class Form1 : Form
    {

        private async void CheckForUpdates()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = await client.GetStringAsync("https://muammertaskincay.github.io/update-checker/update_info.json");

                    dynamic updateInfo = JsonConvert.DeserializeObject(json);

                    Version latestVersion = new Version(updateInfo.latestVersion.ToString());
                    Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    if (currentVersion < latestVersion)
                    {
                        DialogResult result = MessageBox.Show($"Yeni bir sürüm mevcut: {latestVersion}. Güncellemek ister misiniz?\n\n{updateInfo.releaseNotes}", "Güncelleme Mevcut", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = updateInfo.downloadUrl.ToString(),
                                UseShellExecute = true
                            });
                            Application.Exit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güncellemeleri kontrol ederken bir hata oluştu: {ex.ToString()}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private bool dragging = false;
        private Point offset;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                offset = new Point(e.X, e.Y);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point newPoint = this.PointToScreen(e.Location);
                this.Location = new Point(newPoint.X - offset.X, newPoint.Y - offset.Y);
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;
            }
        }

       
        public Form1()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);
            label1.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label2.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label3.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label4.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Uygulama kapanırken işlemler yapabilirsiniz
            Application.Exit(); // Uygulamayı tamamen kapatır
        }

        private DatabaseHelper dbHelper = new DatabaseHelper();

        public class DatabaseHelper
        {
            // connectionString'i sınıf düzeyinde tanımlayın
            private string connectionString = "Data Source=newlogin.db;Version=3;";

            // Veritabanına bağlanma metodu
            public void ConnectToDatabase()
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        Console.WriteLine("Veritabanına bağlandı!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Bağlantı hatası: " + ex.Message);
                    }
                }
            }

            // Veri ekleme metodu
            public void InsertData(string username, string password)
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO Users (Username, Password) VALUES (@username, @password)";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", password);
                       
                        command.ExecuteNonQuery();
                    }
                }
            }
            // Kullanıcı girişini doğrulayan metod
            public bool ValidateLogin(string username, string password)
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(1) FROM Users WHERE Username = @username AND Password = @password";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", password);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count == 1;
                    }
                }
            }

            public bool CheckIfAdmin(string username)
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT IsAdmin FROM Users WHERE Username = @username";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        object result = command.ExecuteScalar();

                        if (result == DBNull.Value)
                        {
                            // NULL değeri varsa varsayılan olarak false döndürün
                            return false;
                        }
                        else
                        {
                            // TRUE/FALSE değerini dönüştür
                            return Convert.ToBoolean(result);
                        }
                    }
                }
            }
            public bool IsPasswordReset(string username)
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT IsPasswordReset FROM Users WHERE Username = @username";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        object result = command.ExecuteScalar();
                        if (result == DBNull.Value)
                        {
                            return false;
                        }
                        else
                        {
                            return Convert.ToBoolean(result);
                        }
                    }
                }
            }
        }
        
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (dbHelper.ValidateLogin(username, password))
            {
                bool isPasswordReset = dbHelper.IsPasswordReset(username);

                if (isPasswordReset)
                {
                    // Şifre sıfırlama formunu aç
                    NewPasswordForm newPasswordForm = new NewPasswordForm(username);
                    newPasswordForm.ShowDialog();
                }
                else
                {
                    bool isAdmin = dbHelper.CheckIfAdmin(username);

                    if (isAdmin)
                    {
                        AdminPanelForm adminPanel = new AdminPanelForm();
                        adminPanel.Show();
                        this.Hide();
                    }
                    else
                    {
                        int userId = GetUserId(username); // Kullanıcı ID'sini al
                        NoteForm noteForm = new NoteForm(userId);
                        noteForm.Show();
                        this.Hide();
                        MessageBox.Show("Giriş Başarılı!");
                    }
                }
            }
            else
            {
                MessageBox.Show("Geçersiz kullanıcı adı veya şifre.");
            }
        }

        private int GetUserId(string username)
        {
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=newlogin.db;Version=3;"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id FROM Users WHERE Username = @username";
                command.Parameters.AddWithValue("@username", username);
                object result = command.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception("Kullanıcı bulunamadı."); // Kullanıcı ID'si bulunamazsa bir hata fırlat
                }
            }
        }

        private void btnRegister_Click_1(object sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            registerForm.ShowDialog();
           
        }

        private void label3_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Uygulamayı kapatır
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForUpdates();
        }
    }
}
    

