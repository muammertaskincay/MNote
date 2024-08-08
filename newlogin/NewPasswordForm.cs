using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace newlogin
{
    public partial class NewPasswordForm : Form
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


        private string connectionString = "Data Source=newlogin.db;Version=3;";
        private string username;

        public NewPasswordForm(string username)
        {
            InitializeComponent();
            this.username = username;
           
            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);
            label1.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label2.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label3.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
            label4.BackColor = Color.FromArgb(0, 0, 0, 0); // Şeffaf arka plan
        }

      


        private void btnSubmit_Click_1(object sender, EventArgs e)
        {
            string password1 = txtNewPassword1.Text;
            string password2 = txtNewPassword2.Text;

            if (!ValidatePassword(password1))
            {
                MessageBox.Show("Şifre en az 8 karakter uzunluğunda olmalı, büyük/küçük harf içermeli ve özel karakter içermelidir.");
                return;
            }

            if (password1 != password2)
            {
                MessageBox.Show("Şifreler eşleşmiyor. Lütfen tekrar deneyin.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password1) || string.IsNullOrWhiteSpace(password2))
            {
                MessageBox.Show("Şifreler boş olamaz.");
                return;
            }

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Users SET Password = @password, IsPasswordReset = 0 WHERE Username = @username";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@password", password1);
                        command.Parameters.AddWithValue("@username", username);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Şifre başarıyla güncellendi.");
                this.Close(); // Formu kapatabilirsiniz veya uygun bir formu gösterebilirsiniz
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message);
            }
        }

        private bool ValidatePassword(string password)
        {
            // Şifrenin en az 8 karakter uzunluğunda olması, büyük/küçük harf içermesi ve özel karakter içermesini kontrol eder
            if (password.Length < 8)
                return false;

            if (!Regex.IsMatch(password, "[A-Z]")) // Büyük harf
                return false;

            if (!Regex.IsMatch(password, "[a-z]")) // Küçük harf
                return false;

            if (!Regex.IsMatch(password, "[0-9]")) // Rakam
                return false;

            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]")) // Özel karakter
                return false;

            return true;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {
            Application.Exit();

        }
    }
 }
