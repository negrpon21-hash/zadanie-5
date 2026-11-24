using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace DoctorMagApp
{
    public class AppointmentsPanel : UserControl
    {
        private int userId; private string userName; private string userRole;
        private List<Appointment> allAppts = new();
        private List<Appointment> filtered = new();

        private TextBox txtSearch;
        private ComboBox cmbVisitType, cmbSort;
        private FlowLayoutPanel flowPanel;
        private Label lblCount;
        private Button btnAdd;

        public AppointmentsPanel(int uid, string name, string role) { userId = uid; userName = name; userRole = role; BuildUI(); LoadData(); }

        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(183, 211, 242);

            var pTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(175, 175, 220), Padding = new Padding(5) };

            txtSearch = new TextBox { Location = new Point(10, 12), Size = new Size(220, 24), Font = new Font("Comic Sans MS", 9f), PlaceholderText = "Поиск..." };
            txtSearch.TextChanged += (s, e) => ApplyFilters();

            cmbVisitType = MakeCombo(new Point(240, 12), 200, new[] { "Сбросить фильтрацию", "Первичное", "Повторное", "Срочное", "По полису ОМС", "По направлению", "Платное" });
            cmbSort = MakeCombo(new Point(450, 12), 250, new[] { "Дата записи ↑", "Дата записи ↓" });

            btnAdd = new Button
            {
                Text = "+ Добавить запись",
                Location = new Point(850, 12), Size = new Size(150, 28),
                BackColor = Color.FromArgb(138, 132, 226), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Comic Sans MS", 9f, FontStyle.Bold)
            };
            btnAdd.Click += (s, e) => MessageBox.Show("Выберите сотрудника на вкладке 'Сотрудники' для добавления записи.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);

            pTop.Controls.AddRange(new Control[] { txtSearch, cmbVisitType, cmbSort, btnAdd });

            flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(183, 211, 242), Padding = new Padding(10) };
            lblCount = new Label { Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Comic Sans MS", 9f), Padding = new Padding(10, 0, 0, 0) };

            this.Controls.Add(flowPanel);
            this.Controls.Add(lblCount);
            this.Controls.Add(pTop);
        }

        private ComboBox MakeCombo(Point loc, int width, string[] items)
        {
            var cb = new ComboBox { Location = loc, Size = new Size(width, 24), Font = new Font("Comic Sans MS", 9f), DropDownStyle = ComboBoxStyle.DropDownList };
            cb.Items.AddRange(items);
            cb.SelectedIndex = 0;
            cb.SelectedIndexChanged += (s, e) => ApplyFilters();
            return cb;
        }

        private void LoadData()
        {
            allAppts.Clear();
            using var conn = DbHelper.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT a.id, p.full_name as patient_name, d.full_name as doctor_name, 
                d.specialty, d.shift, d.work_hours, d.photo, d.status as doc_status,
                a.record_date, a.visit_date, a.visit_time, a.visit_type
                FROM Appointments a
                LEFT JOIN Patients p ON a.patient_id = p.id
                LEFT JOIN Doctors d ON a.doctor_id = d.id
                ORDER BY a.visit_date DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                allAppts.Add(new Appointment
                {
                    Id = r.GetInt32("id"),
                    PatientName = r.IsDBNull(r.GetOrdinal("patient_name")) ? "" : r.GetString("patient_name"),
                    DoctorName = r.IsDBNull(r.GetOrdinal("doctor_name")) ? "" : r.GetString("doctor_name"),
                    Specialty = r.IsDBNull(r.GetOrdinal("specialty")) ? "" : r.GetString("specialty"),
                    Shift = r.IsDBNull(r.GetOrdinal("shift")) ? "" : r.GetString("shift"),
                    WorkHours = r.IsDBNull(r.GetOrdinal("work_hours")) ? "" : r.GetString("work_hours"),
                    Photo = r.IsDBNull(r.GetOrdinal("photo")) ? "" : r.GetString("photo"),
                    RecordDate = r.IsDBNull(r.GetOrdinal("record_date")) ? DateTime.MinValue : r.GetDateTime("record_date"),
                    VisitDate = r.IsDBNull(r.GetOrdinal("visit_date")) ? DateTime.MinValue : r.GetDateTime("visit_date"),
                    VisitTime = r.IsDBNull(r.GetOrdinal("visit_time")) ? "" : r.GetValue(r.GetOrdinal("visit_time")).ToString() ?? "",
                    VisitType = r.IsDBNull(r.GetOrdinal("visit_type")) ? "" : r.GetString("visit_type")
                });
            }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string search = txtSearch.Text.ToLower();
            string vtype = cmbVisitType.SelectedIndex > 0 ? cmbVisitType.SelectedItem.ToString() : "";
            string sort = cmbSort.SelectedItem?.ToString() ?? "";

            filtered = new List<Appointment>(allAppts);
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.FindAll(a => (a.PatientName + a.DoctorName + a.Specialty + a.VisitType + a.VisitDate.ToShortDateString()).ToLower().Contains(search));
            if (!string.IsNullOrEmpty(vtype))
                filtered = filtered.FindAll(a => a.VisitType.Contains(vtype, StringComparison.OrdinalIgnoreCase));

            filtered = sort == "Дата записи ↓"
                ? filtered.OrderByDescending(a => a.VisitDate)
                : filtered.OrderBy(a => a.VisitDate);

            RenderCards();
        }

        private void RenderCards()
        {
            flowPanel.SuspendLayout();
            flowPanel.Controls.Clear();
            string imgDir = AppDomain.CurrentDomain.BaseDirectory + "Images\\";

            foreach (var a in filtered)
            {
                var card = new Panel
                {
                    Size = new Size(380, 115),
                    BackColor = Color.White,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = a,
                    Cursor = Cursors.Hand
                };

                var pic = new PictureBox { Location = new Point(5, 5), Size = new Size(70, 100), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.LightGray };
                try
                {
                    string pp = string.IsNullOrEmpty(a.Photo) ? "" : imgDir + a.Photo;
                    pic.Image = File.Exists(pp) ? Image.FromFile(pp) : Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + @"Resources\picture.png");
                }
                catch { }

                void Lbl(string text, Point loc, int w = 290, bool bold = false, Color? col = null)
                {
                    var l = new Label { Text = text, Location = loc, Size = new Size(w, 16), Font = new Font("Comic Sans MS", 8f, bold ? FontStyle.Bold : FontStyle.Regular) };
                    if (col.HasValue) l.ForeColor = col.Value;
                    card.Controls.Add(l);
                }

                Lbl(a.DoctorName, new Point(85, 5), bold: true);
                Lbl("Смена: " + a.Shift, new Point(85, 21), col: Color.DimGray);
                Lbl("Режим: " + a.WorkHours, new Point(85, 37), col: Color.DimGray);
                Lbl("Направление: " + a.Specialty, new Point(85, 53));
                Lbl("Посетитель: " + a.PatientName, new Point(85, 69));
                Lbl("Тип: " + a.VisitType, new Point(85, 85));
                Lbl("Дата/время: " + (a.VisitDate != DateTime.MinValue ? a.VisitDate.ToShortDateString() : "-") + " " + a.VisitTime, new Point(200, 85));

                card.Controls.Add(pic);
                card.Click += (s, e) => OpenRepeat((Appointment)((Panel)s).Tag);
                foreach (Control c in card.Controls) c.Click += (s2, e2) => OpenRepeat((Appointment)card.Tag);

                flowPanel.Controls.Add(card);
            }
            flowPanel.ResumeLayout();
            lblCount.Text = $"  Показано: {filtered.Count} из {allAppts.Count}";
        }

        private void OpenRepeat(Appointment a)
        {
            // Получаем врача для повторной записи
            using var conn = DbHelper.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, full_name, specialty, shift, work_hours, visit_type, status, photo FROM Doctors WHERE id=(SELECT doctor_id FROM Appointments WHERE id=@id)";
            cmd.Parameters.AddWithValue("@id", a.Id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                var d = new Doctor
                {
                    Id = r.GetInt32("id"), FullName = r.GetString("full_name"),
                    Specialty = r.IsDBNull(r.GetOrdinal("specialty")) ? "" : r.GetString("specialty"),
                    Shift = r.IsDBNull(r.GetOrdinal("shift")) ? "" : r.GetString("shift"),
                    WorkHours = r.IsDBNull(r.GetOrdinal("work_hours")) ? "" : r.GetString("work_hours"),
                    VisitType = r.IsDBNull(r.GetOrdinal("visit_type")) ? "" : r.GetString("visit_type"),
                    Status = r.IsDBNull(r.GetOrdinal("status")) ? "" : r.GetString("status"),
                    Photo = r.IsDBNull(r.GetOrdinal("photo")) ? "" : r.GetString("photo")
                };
                r.Close();
                if (d.Status != "Присутствует") { MessageBox.Show("Сотрудник временно отсутствует, запись невозможна.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                new AddAppointmentForm(d, userId, userName).ShowDialog();
                LoadData();
            }
        }
    }

    public class Appointment
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string Shift { get; set; } = "";
        public string WorkHours { get; set; } = "";
        public string Photo { get; set; } = "";
        public DateTime RecordDate { get; set; }
        public DateTime VisitDate { get; set; }
        public string VisitTime { get; set; } = "";
        public string VisitType { get; set; } = "";
    }
}
