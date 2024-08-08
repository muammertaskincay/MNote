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

namespace newlogin
{
    public partial class RegisterForm : Form
    {

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

        private DatabaseHelper dbHelper = new DatabaseHelper();
        public RegisterForm()
        {
            InitializeComponent();
            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);
            label1.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label2.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label3.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label4.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
        }

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

            // Kullanıcı adının veritabanında olup olmadığını kontrol etme metodu
            public bool CheckIfUsernameExists(string username)
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(1) FROM Users WHERE Username = @username";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
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
        }
        private bool ValidatePassword(string password)
        {
            // En az 8 karakter olmalı
            if (password.Length < 8)
                return false;

            // Büyük harf olmalı
            if (!password.Any(char.IsUpper))
                return false;

            // Küçük harf olmalı
            if (!password.Any(char.IsLower))
                return false;

            // Özel karakter olmalı
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                return false;

            return true;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (!ValidatePassword(password))
            {
                MessageBox.Show("Şifre en az 8 karakter uzunluğunda olmalı, büyük/küçük harf içermeli ve özel karakter içermelidir.");
                return;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Kullanıcı adı ve şifre boş olamaz.");
                return;
            }

            if (dbHelper.CheckIfUsernameExists(username))
            {
                MessageBox.Show("Bu kullanıcı adı zaten alınmış.");
            }
            else
            {
                dbHelper.InsertData(username, password);
                MessageBox.Show("Kayıt başarılı!");
                this.Close();
            }
        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Uygulamayı kapatır
        }
    }
}

