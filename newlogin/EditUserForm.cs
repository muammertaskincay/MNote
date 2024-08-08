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
    public partial class EditUserForm : Form
    {
        private string connectionString = "Data Source=newlogin.db;Version=3;";

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

        public EditUserForm()
        {
            InitializeComponent();
            LoadUsers();
            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);
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

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (!ValidatePassword(password))
            {
                MessageBox.Show("Şifre en az 8 karakter uzunluğunda olmalı, büyük/küçük harf içermeli ve özel karakter içermelidir.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Kullanıcı adının veritabanında olup olmadığını kontrol edin
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @username";
                    using (SQLiteCommand checkCommand = new SQLiteCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@username", username);
                        int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (userCount > 0)
                        {
                            MessageBox.Show("Bu kullanıcı adı zaten alınmış.");
                            return;
                        }
                    }

                    string query = "INSERT INTO Users (Username, Password, IsAdmin) VALUES (@username, @password, 0)";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", password);
                        command.ExecuteNonQuery();
                    }
                   
                }
                LoadUsers(); // Reload user data
            }
            else
            {
                MessageBox.Show("Kullanıcı adı ve şifre boş olamaz.");
            }
        }

        private void btnDeleteUser_Click(object sender, EventArgs e)
        {
            if (dataGridViewUsers.SelectedRows.Count > 0)
            {
                string username = dataGridViewUsers.SelectedRows[0].Cells["Username"].Value.ToString();

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM Users WHERE Username = @username";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.ExecuteNonQuery();
                    }
                }
                LoadUsers(); // Reload user data
            }
            else
            {
                MessageBox.Show("Silinecek kullanıcıyı seçin.");
            }
        }

        private void btnResetUser_Click(object sender, EventArgs e)
        {
            if (dataGridViewUsers.SelectedRows.Count > 0)
            {
                string username = dataGridViewUsers.SelectedRows[0].Cells["Username"].Value.ToString();
                string newPassword = GenerateRandomPassword(5);

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Users SET Password = @password, IsPasswordReset = 1 WHERE Username = @username";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@password", newPassword);
                        command.Parameters.AddWithValue("@username", username);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Şifre sıfırlandı. Yeni şifre: {newPassword}");
                LoadUsers(); // Reload user data
            }
            else
            {
                MessageBox.Show("Şifresi sıfırlanacak kullanıcıyı seçin.");
            }
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void LoadUsers()
        {
            string query = "SELECT Username, Password, IsAdmin FROM Users";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dataGridViewUsers.DataSource = dt;
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}