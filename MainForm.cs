using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace DoctorMagApp
{
    public class MainForm : Form
    {
        private int userId;
        private string userName;
        private string userRole;

        private Panel panelTop;
        private Label lblUserInfo;
        private Button btnLogout;
        private TabControl tabControl;
        private TabPage tabDoctors;
        private TabPage tabAppointments;
        private TabPage tabHistory;

        public MainForm(int uid, string name, string role)
        {
            userId = uid; userName = name; userRole = role;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "ДокторМаг";
            this.ClientSize = new Size(1100, 700);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(183, 211, 242);
            this.Font = new Font("Comic Sans MS", 9f);
            try { this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + @"Resources\icon.ico"); } catch { }

            // Top panel
            panelTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(175, 175, 220) };

            var logoPic = new PictureBox { Location = new Point(10, 5), Size = new Size(45, 45), SizeMode = PictureBoxSizeMode.Zoom };
            try { logoPic.Image = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + @"Resources\logo.png"); } catch { }

            var lblTitle2 = new Label
            {
                Text = "ДокторМаг",
                Font = new Font("Comic Sans MS", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(138, 132, 226),
                Location = new Point(65, 12),
                Size = new Size(200, 30)
            };

            lblUserInfo = new Label
            {
                Text = $"{userRole}  |  {userName}",
                Font = new Font("Comic Sans MS", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(400, 15),
                Size = new Size(580, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnLogout = new Button
            {
                Text = "Выйти",
                Location = new Point(990, 13),
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(138, 132, 226),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Comic Sans MS", 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLogout.Click += (s, e) => { this.Close(); };
            panelTop.Controls.AddRange(new Control[] { logoPic, lblTitle2, lblUserInfo, btnLogout });

            // Tabs
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Comic Sans MS", 9f),
                Padding = new Point(10, 5)
            };

            tabDoctors = new TabPage("Сотрудники");
            tabDoctors.BackColor = Color.FromArgb(183, 211, 242);
            var doctorsCtrl = new DoctorsPanel(userId, userName, userRole);
            doctorsCtrl.Dock = DockStyle.Fill;
            tabDoctors.Controls.Add(doctorsCtrl);

            tabControl.TabPages.Add(tabDoctors);

            if (userRole == "Сотрудник регистратуры" || userRole == "Администратор" || userRole == "Главный врач")
            {
                tabAppointments = new TabPage("Записи");
                tabAppointments.BackColor = Color.FromArgb(183, 211, 242);
                var apptCtrl = new AppointmentsPanel(userId, userName, userRole);
                apptCtrl.Dock = DockStyle.Fill;
                tabAppointments.Controls.Add(apptCtrl);
                tabControl.TabPages.Add(tabAppointments);
            }

            if (userRole == "Клиент")
            {
                tabHistory = new TabPage("Мои посещения");
                tabHistory.BackColor = Color.FromArgb(183, 211, 242);
                var histCtrl = new PatientHistoryPanel(userId, userName);
                histCtrl.Dock = DockStyle.Fill;
                tabHistory.Controls.Add(histCtrl);
                tabControl.TabPages.Add(tabHistory);
            }

            this.Controls.Add(tabControl);
            this.Controls.Add(panelTop);
        }
    }
}
