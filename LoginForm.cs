using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace DoctorMagApp
{
    public class LoginForm : Form
    {
        private PictureBox logoPic;
        private Label lblTitle;
        private Label lblLogin;
        private TextBox txtLogin;
        private Label lblPassword;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnGuest;
        private Label lblUserInfo;

        public LoginForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "ДокторМаг - Вход";
            this.ClientSize = new Size(480, 380);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(183, 211, 242);
            this.Font = new Font("Comic Sans MS", 9f);
            try { this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + @"Resources\icon.ico"); } catch { }

            logoPic = new PictureBox { Location = new Point(20, 20), Size = new Size(60, 60), SizeMode = PictureBoxSizeMode.Zoom };
            try { logoPic.Image = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + @"Resources\logo.png"); } catch { }

            lblTitle = new Label
            {
                Text = "ДокторМаг",
                Font = new Font("Comic Sans MS", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(138, 132, 226),
                Location = new Point(100, 30),
                Size = new Size(360, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblLogin = new Label { Text = "Логин:", Location = new Point(80, 110), AutoSize = true, Font = new Font("Comic Sans MS", 10f) };
            txtLogin = new TextBox { Location = new Point(80, 135), Size = new Size(320, 26), Font = new Font("Comic Sans MS", 10f) };

            lblPassword = new Label { Text = "Пароль:", Location = new Point(80, 175), AutoSize = true, Font = new Font("Comic Sans MS", 10f) };
            txtPassword = new TextBox { Location = new Point(80, 200), Size = new Size(320, 26), Font = new Font("Comic Sans MS", 10f), UseSystemPasswordChar = true };

            btnLogin = new Button
            {
                Text = "Войти",
                Location = new Point(80, 250),
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(138, 132, 226),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Comic Sans MS", 10f, FontStyle.Bold)
            };
            btnLogin.Click += BtnLogin_Click;

            btnGuest = new Button
            {
                Text = "Войти как гость",
                Location = new Point(250, 250),
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(175, 175, 220),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Comic Sans MS", 10f, FontStyle.Bold)
            };
            btnGuest.Click += BtnGuest_Click;

            lblUserInfo = new Label { Location = new Point(80, 310), Size = new Size(320, 20), ForeColor = Color.Red, TextAlign = ContentAlignment.MiddleCenter };

            this.Controls.AddRange(new Control[] { logoPic, lblTitle, lblLogin, txtLogin, lblPassword, txtPassword, btnLogin, btnGuest, lblUserInfo });
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string pass = txtPassword.Text.Trim();
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
            {
                lblUserInfo.Text = "Заполните все поля";
                return;
            }

            using var conn = DbHelper.GetConnection();
            try
            {
                conn.Open();
                // Try staff login
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, full_name, role FROM Users WHERE login=@l AND password_hash=@p LIMIT 1";
                cmd.Parameters.AddWithValue("@l", login);
                cmd.Parameters.AddWithValue("@p", pass);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    int uid = r.GetInt32("id");
                    string name = r.GetString("full_name");
                    string role = r.GetString("role");
                    r.Close();
                    this.Hide();
                    var mf = new MainForm(uid, name, role);
                    mf.ShowDialog();
                    this.Show();
                    return;
                }
                r.Close();

                // Try patient login: логин = фамилия, пароль = полис ОМС
                var cmd2 = conn.CreateCommand();
                cmd2.CommandText = "SELECT id, full_name FROM Patients WHERE full_name LIKE @l AND policy_number=@p LIMIT 1";
                cmd2.Parameters.AddWithValue("@l", login + "%");
                cmd2.Parameters.AddWithValue("@p", pass);
                using var r2 = cmd2.ExecuteReader();
                if (r2.Read())
                {
                    int pid = r2.GetInt32("id");
                    string name = r2.GetString("full_name");
                    r2.Close();
                    this.Hide();
                    var mf = new MainForm(pid, name, "Клиент");
                    mf.ShowDialog();
                    this.Show();
                    return;
                }

                lblUserInfo.Text = "Неверный логин или пароль";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuest_Click(object sender, EventArgs e)
        {
            this.Hide();
            var mf = new MainForm(0, "Гость", "Гость");
            mf.ShowDialog();
            this.Show();
        }
    }
}
