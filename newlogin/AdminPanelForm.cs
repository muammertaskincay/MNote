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

    public partial class AdminPanelForm : Form
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


        public AdminPanelForm()
            {
                InitializeComponent();
            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);
            this.FormClosing += new FormClosingEventHandler(Form_FormClosing);
            // dataGridView1 kontrolünü form üzerinde oluşturup, bu formun Load olayında veri yükleyin
            LoadDataFromDatabase();
            }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Uygulama kapanırken işlemler yapabilirsiniz
            Application.Exit(); // Uygulamayı tamamen kapatır
        }

        private void LoadDataFromDatabase()
            {
                string query = "SELECT Username, Password, IsAdmin FROM Users";
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Debugging: DataTable'da veri olup olmadığını kontrol edin
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Veritabanında veri bulunamadı.");
                    }

                    // Verileri dataGridView1 kontrolüne yükleyin
                    dataGridView1.DataSource = dt;
                }
            }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            EditUserForm editUserForm = new EditUserForm();
            editUserForm.ShowDialog();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
       
    }

