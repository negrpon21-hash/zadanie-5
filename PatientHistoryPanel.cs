using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace DoctorMagApp
{
    public class PatientHistoryPanel : UserControl
    {
        private int patientId;
        private string patientName;
        private List<Appointment> allVisits = new();
        private List<Appointment> filtered = new();
        private TextBox txtSearch;
        private ComboBox cmbSort;
        private FlowLayoutPanel flowPanel;
        private Label lblCount;

        public PatientHistoryPanel(int pid, string name)
        {
            patientId = pid; patientName = name;
            BuildUI(); LoadData();
        }

        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(183, 211, 242);

            var pTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(175, 175, 220) };
            txtSearch = new TextBox { Location = new Point(10, 12), Size = new Size(220, 24), Font = new Font("Comic Sans MS", 9f), PlaceholderText = "Поиск..." };
            txtSearch.TextChanged += (s, e) => ApplyFilters();
            cmbSort = new ComboBox { Location = new Point(240, 12), Size = new Size(200, 24), Font = new Font("Comic Sans MS", 9f), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSort.Items.AddRange(new[] { "Дата посещения ↑", "Дата посещения ↓" });
            cmbSort.SelectedIndex = 1;
            cmbSort.SelectedIndexChanged += (s, e) => ApplyFilters();
            pTop.Controls.AddRange(new Control[] { txtSearch, cmbSort });

            flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(183, 211, 242), Padding = new Padding(10) };
            lblCount = new Label { Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Comic Sans MS", 9f), Padding = new Padding(10, 0, 0, 0) };

            this.Controls.Add(flowPanel);
            this.Controls.Add(lblCount);
            this.Controls.Add(pTop);
        }

        private void LoadData()
        {
            allVisits.Clear();
            using var conn = DbHelper.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT a.id, d.full_name as doctor_name, d.specialty, d.shift, d.work_hours, d.photo,
                a.record_date, a.visit_date, a.visit_time, a.visit_type
                FROM Appointments a
                LEFT JOIN Doctors d ON a.doctor_id = d.id
                WHERE a.patient_id = @pid ORDER BY a.visit_date DESC";
            cmd.Parameters.AddWithValue("@pid", patientId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                allVisits.Add(new Appointment
                {
                    Id = r.GetInt32("id"),
                    PatientName = patientName,
                    DoctorName = r.IsDBNull(r.GetOrdinal("doctor_name")) ? "" : r.GetString("doctor_name"),
                    Specialty = r.IsDBNull(r.GetOrdinal("specialty")) ? "" : r.GetString("specialty"),
                    Shift = r.IsDBNull(r.GetOrdinal("shift")) ? "" : r.GetString("shift"),
                    WorkHours = r.IsDBNull(r.GetOrdinal("work_hours")) ? "" : r.GetString("work_hours"),
                    Photo = r.IsDBNull(r.GetOrdinal("photo")) ? "" : r.GetString("photo"),
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
            filtered = new List<Appointment>(allVisits);
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.FindAll(a => (a.DoctorName + a.Specialty + a.VisitType + a.VisitDate.ToShortDateString()).ToLower().Contains(search));
            filtered = cmbSort.SelectedIndex == 1
                ? filtered.OrderByDescending(a => a.VisitDate)
                : filtered.OrderBy(a => a.VisitDate);
            RenderCards();
        }

        private void RenderCards()
        {
            flowPanel.SuspendLayout();
            flowPanel.Controls.Clear();
            foreach (var a in filtered)
            {
                var card = new Panel { Size = new Size(380, 90), BackColor = Color.White, Margin = new Padding(5), BorderStyle = BorderStyle.FixedSingle };
                void Lbl(string text, Point loc, bool bold = false, Color? col = null)
                {
                    var l = new Label { Text = text, Location = loc, Size = new Size(360, 16), Font = new Font("Comic Sans MS", 8f, bold ? FontStyle.Bold : FontStyle.Regular) };
                    if (col.HasValue) l.ForeColor = col.Value;
                    card.Controls.Add(l);
                }
                Lbl(a.DoctorName, new Point(10, 8), bold: true);
                Lbl("Направление: " + a.Specialty, new Point(10, 26));
                Lbl("Тип посещения: " + a.VisitType, new Point(10, 44));
                Lbl("Дата: " + (a.VisitDate != DateTime.MinValue ? a.VisitDate.ToShortDateString() : "-") + "  " + a.VisitTime, new Point(10, 62));
                flowPanel.Controls.Add(card);
            }
            flowPanel.ResumeLayout();
            lblCount.Text = $"  Показано: {filtered.Count} из {allVisits.Count}";
        }
    }
}
