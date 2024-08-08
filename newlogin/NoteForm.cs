using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;



namespace newlogin
{
    public partial class NoteForm : Form
    {
        // Kullanıcı ID'sini alacak yapıcı metod
        public NoteForm(int userId)
        {
            InitializeComponent();
            this.MouseDown += new MouseEventHandler(NoteForm_MouseDown);
            this.MouseMove += new MouseEventHandler(NoteForm_MouseMove);
            this.MouseUp += new MouseEventHandler(NoteForm_MouseUp);
            _userId = userId; // Kullanıcı ID'sini sakla
            LoadNotes(); // Notları yükle
            pictureBox3.Click += pictureBox3_Click; // Olay işleyicisini bağlayın
            txtContent.Visible = false; // txtContent başlangıçta görünmez
                                       
            // txtTitle tıklama olayını bağla
            this.txtTitle.Click += new System.EventHandler(this.txtTitle_TextChanged);
        }

        private bool dragging = false;
        private Point offset;

        private void NoteForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                offset = new Point(e.X, e.Y);
            }
        }

        private void NoteForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point newPoint = this.PointToScreen(e.Location);
                this.Location = new Point(newPoint.X - offset.X, newPoint.Y - offset.Y);
            }
        }

        private void NoteForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;
            }
        }

        private int _userId; // Kullanıcı ID'sini saklayacak değişken
        private string connectionString = "Data Source=newlogin.db;Version=3;"; // Veritabanı bağlantı dizesi


        private void LoadNotes()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Title, Font, Color, FilePath FROM Notes WHERE UserId = @UserId";
                command.Parameters.AddWithValue("@UserId", _userId);
                using (var reader = command.ExecuteReader())
                {
                    listBoxNotes.Items.Clear(); // Önceki notları temizle
                    while (reader.Read())
                    {
                        var note = new NoteItem
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            FilePath = reader.IsDBNull(4) ? null : reader.GetString(4)
                        };

                        // Dosyadan not içeriğini oku
                        if (!string.IsNullOrEmpty(note.FilePath) && File.Exists(note.FilePath))
                        {
                            note.Content = File.ReadAllText(note.FilePath);
                        }

                        listBoxNotes.Items.Add(note); // NoteItem nesnesini listeye ekle
                    }
                }

               

            }

            // Varsayılan font ayarlarını belirtin
            txtContent.Font = new Font("Arial", 12, FontStyle.Regular);
            txtContent.ForeColor = Color.Black;
        }







        private void listBoxNotes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxNotes.SelectedItem is NoteItem selectedNote)
            {
                txtTitle.Text = selectedNote.Title;
                txtContent.Text = selectedNote.Content;
                selectedNoteId = selectedNote.Id;

                // Dosyadan not içeriğini oku
                if (!string.IsNullOrEmpty(selectedNote.FilePath) && File.Exists(selectedNote.FilePath))
                {
                    txtContent.Text = File.ReadAllText(selectedNote.FilePath);
                }
            }
        }


        


        private void NoteForm_Load(object sender, EventArgs e)
        {
            
        }

        private int? selectedNoteId = null; // Seçilen notun ID'sini saklamak için

        // Not sınıfı
        public class NoteItem
        {
            public int Id { get; set; }
            public string Title { get; set; }

            public string Content { get; set; }
            public string FilePath { get; set; }

            public override string ToString()
            {
                return Title; // ListBox'ta görünecek olan
            }
        }

        private void açToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxNotes.Visible = !listBoxNotes.Visible;
            editToolStripMenuItem.Visible = listBoxNotes.Visible;
            deleteToolStripMenuItem.Visible = listBoxNotes.Visible;
            txtSearch.Visible = listBoxNotes.Visible;  
            label1.Visible = listBoxNotes.Visible;
        }

        private void kaydetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text;
            string content = txtContent.Text;
            string font = txtContent.Font.FontFamily.Name + "," + txtContent.Font.Size + "," + (int)txtContent.Font.Style;
            string color = txtContent.ForeColor.ToArgb().ToString();

            bool isUnique;

            if (selectedNoteId.HasValue)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Title FROM Notes WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", selectedNoteId.Value);
                    string currentTitle = (string)command.ExecuteScalar();

                    if (title == currentTitle)
                    {
                        isUnique = true;
                    }
                    else
                    {
                        isUnique = IsTitleUnique(title, selectedNoteId);
                    }
                }
            }
            else
            {
                isUnique = IsTitleUnique(title);
            }

            if (isUnique)
            {
                if (selectedNoteId.HasValue)
                {
                    var result = MessageBox.Show("Bu notu mevcut notun üzerine mi kaydetmek istersiniz? (Evet: Güncelle, Hayır: Yeni Not)", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        using (var connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            var command = connection.CreateCommand();
                            command.CommandText = "UPDATE Notes SET Title = @Title, Content = @Content, Font = @Font, Color = @Color, FilePath = @FilePath WHERE Id = @Id";
                            command.Parameters.AddWithValue("@Title", title);
                            command.Parameters.AddWithValue("@Content", content);
                            command.Parameters.AddWithValue("@Font", font);
                            command.Parameters.AddWithValue("@Color", color);
                            string filePath = SaveNoteToDesktop(title, content);
                            command.Parameters.AddWithValue("@FilePath", filePath);
                            command.Parameters.AddWithValue("@Id", selectedNoteId.Value);
                            command.ExecuteNonQuery();
                        }

                        LoadNotes();
                        selectedNoteId = null;
                        txtTitle.Clear();
                        txtContent.Clear();
                    }
                    else if (result == DialogResult.No)
                    {
                        if (IsTitleUnique(title))
                        {
                            using (var connection = new SQLiteConnection(connectionString))
                            {
                                connection.Open();
                                var command = connection.CreateCommand();
                                command.CommandText = "INSERT INTO Notes (UserId, Title, Content, Font, Color, FilePath) VALUES (@UserId, @Title, @Content, @Font, @Color, @FilePath)";
                                command.Parameters.AddWithValue("@UserId", _userId);
                                command.Parameters.AddWithValue("@Title", title);
                                command.Parameters.AddWithValue("@Content", content);
                                command.Parameters.AddWithValue("@Font", font);
                                command.Parameters.AddWithValue("@Color", color);
                                string filePath = SaveNoteToDesktop(title, content);
                                command.Parameters.AddWithValue("@FilePath", filePath);
                                command.ExecuteNonQuery();
                            }

                            LoadNotes();
                            selectedNoteId = null;
                            txtTitle.Clear();
                            txtContent.Clear();
                        }
                        else
                        {
                            MessageBox.Show("Aynı başlıkta bir not zaten mevcut. Lütfen farklı bir başlık seçin.",
                                "Başlık Uyarısı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "INSERT INTO Notes (UserId, Title, Content, Font, Color, FilePath) VALUES (@UserId, @Title, @Content, @Font, @Color, @FilePath)";
                        command.Parameters.AddWithValue("@UserId", _userId);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Content", content);
                        command.Parameters.AddWithValue("@Font", font);
                        command.Parameters.AddWithValue("@Color", color);
                        string filePath = SaveNoteToDesktop(title, content);
                        command.Parameters.AddWithValue("@FilePath", filePath);
                        command.ExecuteNonQuery();
                    }

                    // Masaüstüne kaydet
                    SaveNoteToDesktop(title, content);

                    LoadNotes();
                    selectedNoteId = null;
                    txtTitle.Clear();
                    txtContent.Clear();
                }
            }
            else
            {
                MessageBox.Show("Aynı başlıkta bir not zaten mevcut. Lütfen farklı bir başlık seçin.", "Başlık Uyarısı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string SaveNoteToDesktop(string title, string content)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, title + ".txt");

            File.WriteAllText(filePath, content);
            return filePath;
        }

        

      



        private bool IsTitleUnique(string title, int? excludeNoteId = null)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Notes WHERE UserId = @UserId AND Title = @Title AND (@ExcludeId IS NULL OR Id != @ExcludeId)";
                command.Parameters.AddWithValue("@UserId", _userId);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@ExcludeId", excludeNoteId.HasValue ? (object)excludeNoteId.Value : DBNull.Value);
                int count = Convert.ToInt32(command.ExecuteScalar());
                return count == 0;
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBoxNotes.SelectedItem is NoteItem selectedNote)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Id, Title, Content, Font, Color FROM Notes WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", selectedNote.Id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtTitle.Text = reader.GetString(1); // Başlık
                            txtContent.Text = reader.GetString(2); // İçerik
                            selectedNoteId = reader.GetInt32(0); // Not ID'sini sakla

                            // Font ve renk ayarlarını yükle
                            string font = reader.GetString(3);
                            string color = reader.GetString(4);
                            if (!string.IsNullOrEmpty(font))
                            {
                                var fontParts = font.Split(',');
                                FontStyle fontStyle;
                                // Eğer FontStyle düzgün okunamazsa, varsayılan olarak Regular kullan
                                if (Enum.TryParse<FontStyle>(fontParts[2], out fontStyle))
                                {
                                    txtContent.Font = new Font(fontParts[0], float.Parse(fontParts[1]), fontStyle);
                                }
                                else
                                {
                                    txtContent.Font = new Font(fontParts[0], float.Parse(fontParts[1]), FontStyle.Regular);
                                }
                            }
                            if (!string.IsNullOrEmpty(color))
                            {
                                txtContent.ForeColor = Color.FromArgb(int.Parse(color));
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen düzenlemek için bir not seçin.");
            }
        }






        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBoxNotes.SelectedItem is NoteItem selectedNote)
            {
                var result = MessageBox.Show("Bu notu silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM Notes WHERE Id = @Id";
                        command.Parameters.AddWithValue("@Id", selectedNote.Id);
                        command.ExecuteNonQuery();
                    }

                    LoadNotes(); // Notları tekrar yükle
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir not seçin.");
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Arama terimini al
            string searchTerm = txtSearch.Text.ToLower();

            // Listeyi temizle
            listBoxNotes.Items.Clear();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Title FROM Notes WHERE UserId = @UserId AND Title LIKE @SearchTerm";
                command.Parameters.AddWithValue("@UserId", _userId);
                command.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var note = new NoteItem
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1)
                        };
                        listBoxNotes.Items.Add(note); // NoteItem nesnesini listeye ekle
                    }
                }
            }
        }

        private void yazıFontuToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            FontDialog fontDialog = new FontDialog
            {
                ShowEffects = false, // Stil efektlerini gizle
                Font = txtContent.Font // Mevcut fontu göster
            };

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                // Sadece Bold, Italic ve Regular stillerine izin ver
                FontStyle selectedStyle = fontDialog.Font.Style & (FontStyle.Bold | FontStyle.Italic | FontStyle.Regular);
                txtContent.Font = new Font(fontDialog.Font.FontFamily, fontDialog.Font.Size, selectedStyle);
            }
        }

        private void yazıRengiToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                txtContent.ForeColor = colorDialog.Color;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Form'un tam ekran olup olmadığını izlemek için bir değişken
        private bool isFullScreen = false;

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (!isFullScreen)
            {
                // Form'u tam ekran yap
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                isFullScreen = true; // Tam ekran modunu takip etmek için değişkeni güncelle
            }
            else
            {
                // Form'u normal boyuta döndür, ama BorderStyle'ı değiştirme
                this.WindowState = FormWindowState.Normal;
                isFullScreen = false; // Tam ekran modunu takip etmek için değişkeni güncelle
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized; // Formu simge durumuna küçült
        }

        private void txtTitle_TextChanged(object sender, EventArgs e)
        {
            txtContent.Visible = true; // txtTitle tıklandığında txtContent'i görünür yap
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            listBoxNotes.Visible = !listBoxNotes.Visible;
            editToolStripMenuItem.Visible = listBoxNotes.Visible;
            deleteToolStripMenuItem.Visible = listBoxNotes.Visible;
            txtSearch.Visible = listBoxNotes.Visible;
            label1.Visible = listBoxNotes.Visible;
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text;
            string content = txtContent.Text;
            string font = txtContent.Font.FontFamily.Name + "," + txtContent.Font.Size + "," + (int)txtContent.Font.Style;
            string color = txtContent.ForeColor.ToArgb().ToString();

            bool isUnique;

            if (selectedNoteId.HasValue)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Title FROM Notes WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", selectedNoteId.Value);
                    string currentTitle = (string)command.ExecuteScalar();

                    if (title == currentTitle)
                    {
                        isUnique = true;
                    }
                    else
                    {
                        isUnique = IsTitleUnique(title, selectedNoteId);
                    }
                }
            }
            else
            {
                isUnique = IsTitleUnique(title);
            }

            if (isUnique)
            {
                if (selectedNoteId.HasValue)
                {
                    var result = MessageBox.Show("Bu notu mevcut notun üzerine mi kaydetmek istersiniz? (Evet: Güncelle, Hayır: Yeni Not)", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        using (var connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            var command = connection.CreateCommand();
                            command.CommandText = "UPDATE Notes SET Title = @Title, Content = @Content, Font = @Font, Color = @Color, FilePath = @FilePath WHERE Id = @Id";
                            command.Parameters.AddWithValue("@Title", title);
                            command.Parameters.AddWithValue("@Content", content);
                            command.Parameters.AddWithValue("@Font", font);
                            command.Parameters.AddWithValue("@Color", color);
                            string filePath = SaveNoteToDesktop(title, content);
                            command.Parameters.AddWithValue("@FilePath", filePath);
                            command.Parameters.AddWithValue("@Id", selectedNoteId.Value);
                            command.ExecuteNonQuery();
                        }

                        LoadNotes();
                        selectedNoteId = null;
                        txtTitle.Clear();
                        txtContent.Clear();
                    }
                    else if (result == DialogResult.No)
                    {
                        if (IsTitleUnique(title))
                        {
                            using (var connection = new SQLiteConnection(connectionString))
                            {
                                connection.Open();
                                var command = connection.CreateCommand();
                                command.CommandText = "INSERT INTO Notes (UserId, Title, Content, Font, Color, FilePath) VALUES (@UserId, @Title, @Content, @Font, @Color, @FilePath)";
                                command.Parameters.AddWithValue("@UserId", _userId);
                                command.Parameters.AddWithValue("@Title", title);
                                command.Parameters.AddWithValue("@Content", content);
                                command.Parameters.AddWithValue("@Font", font);
                                command.Parameters.AddWithValue("@Color", color);
                                string filePath = SaveNoteToDesktop(title, content);
                                command.Parameters.AddWithValue("@FilePath", filePath);
                                command.ExecuteNonQuery();
                            }

                            LoadNotes();
                            selectedNoteId = null;
                            txtTitle.Clear();
                            txtContent.Clear();
                        }
                        else
                        {
                            MessageBox.Show("Aynı başlıkta bir not zaten mevcut. Lütfen farklı bir başlık seçin.",
                                "Başlık Uyarısı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "INSERT INTO Notes (UserId, Title, Content, Font, Color, FilePath) VALUES (@UserId, @Title, @Content, @Font, @Color, @FilePath)";
                        command.Parameters.AddWithValue("@UserId", _userId);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Content", content);
                        command.Parameters.AddWithValue("@Font", font);
                        command.Parameters.AddWithValue("@Color", color);
                        string filePath = SaveNoteToDesktop(title, content);
                        command.Parameters.AddWithValue("@FilePath", filePath);
                        command.ExecuteNonQuery();
                    }

                    // Masaüstüne kaydet
                    SaveNoteToDesktop(title, content);

                    LoadNotes();
                    selectedNoteId = null;
                    txtTitle.Clear();
                    txtContent.Clear();
                }
            }
            else
            {
                MessageBox.Show("Aynı başlıkta bir not zaten mevcut. Lütfen farklı bir başlık seçin.", "Başlık Uyarısı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            if (listBoxNotes.SelectedItem is NoteItem selectedNote)
            {
                var result = MessageBox.Show("Bu notu silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM Notes WHERE Id = @Id";
                        command.Parameters.AddWithValue("@Id", selectedNote.Id);
                        command.ExecuteNonQuery();
                    }

                    // Notu masaüstünden de sil
                    if (!string.IsNullOrEmpty(selectedNote.FilePath) && File.Exists(selectedNote.FilePath))
                    {
                        File.Delete(selectedNote.FilePath);
                    }

                    LoadNotes(); // Notları tekrar yükle
                    selectedNoteId = null; // Seçilen not ID'sini sıfırla
                    txtTitle.Clear(); // Başlık kutusunu temizle
                    txtContent.Clear(); // İçerik kutusunu temizle
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir not seçin.");
            }
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            if (listBoxNotes.SelectedItem is NoteItem selectedNote)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Id, Title, Content, Font, Color FROM Notes WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", selectedNote.Id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtTitle.Text = reader.GetString(1); // Başlık
                            txtContent.Text = reader.GetString(2); // İçerik
                            selectedNoteId = reader.GetInt32(0); // Not ID'sini sakla

                            // Font ve renk ayarlarını yükle
                            string font = reader.GetString(3);
                            string color = reader.GetString(4);
                            if (!string.IsNullOrEmpty(font))
                            {
                                var fontParts = font.Split(',');
                                FontStyle fontStyle;
                                // Eğer FontStyle düzgün okunamazsa, varsayılan olarak Regular kullan
                                if (Enum.TryParse<FontStyle>(fontParts[2], out fontStyle))
                                {
                                    txtContent.Font = new Font(fontParts[0], float.Parse(fontParts[1]), fontStyle);
                                }
                                else
                                {
                                    txtContent.Font = new Font(fontParts[0], float.Parse(fontParts[1]), FontStyle.Regular);
                                }
                            }
                            if (!string.IsNullOrEmpty(color))
                            {
                                txtContent.ForeColor = Color.FromArgb(int.Parse(color));
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen düzenlemek için bir not seçin.");
            }
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {

            FontDialog fontDialog = new FontDialog
            {
                ShowEffects = false, // Stil efektlerini gizle
                Font = txtContent.Font // Mevcut fontu göster
            };

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                // Sadece Bold, Italic ve Regular stillerine izin ver
                FontStyle selectedStyle = fontDialog.Font.Style & (FontStyle.Bold | FontStyle.Italic | FontStyle.Regular);
                txtContent.Font = new Font(fontDialog.Font.FontFamily, fontDialog.Font.Size, selectedStyle);
            }
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                txtContent.ForeColor = colorDialog.Color;
            }
        }
    }
}


