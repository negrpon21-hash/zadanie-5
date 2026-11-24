using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace DoctorMagApp
{
    public class DoctorsPanel : UserControl
    {
        private int userId; private string userName; private string userRole;
        private List<Doctor> allDoctors = new();
        private List<Doctor> filtered = new();

        private TextBox txtSearch;
        private ComboBox cmbStatus, cmbVisitType, cmbSpecialty, cmbShift, cmbSort;
        private FlowLayoutPanel flowPanel;
        private Label lblCount;
        private Button btnAddAppt;

        public DoctorsPanel(int uid, string name, string role) { userId = uid; userName = name; userRole = role; BuildUI(); LoadData(); }

        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(183, 211, 242);

            var pTop = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.FromArgb(175, 175, 220), Padding = new Padding(5) };

            txtSearch = new TextBox { Location = new Point(10, 10), Size = new Size(220, 24), Font = new Font("Comic Sans MS", 9f), PlaceholderText = "Поиск..." };
            txtSearch.TextChanged += (s, e) => ApplyFilters();

            cmbStatus = MakeCombo(new Point(240, 10), 160, new[] { "Сбросить фильтрацию", "Присутствует", "В отпуске", "В командировке" });
            cmbVisitType = MakeCombo(new Point(410, 10), 180, new[] { "Сбросить фильтрацию", "По полису ОМС", "По направлению", "Срочное", "Платное" });
            cmbSpecialty = MakeCombo(new Point(600, 10), 200, new[] { "Сбросить фильтрацию" });
            cmbShift = MakeCombo(new Point(810, 10), 160, new[] { "Сбросить фильтрацию", "1 Смена", "2 Смена" });
            cmbSort = MakeCombo(new Point(10, 45), 350, new[] { "Без сортировки", "ФИО ↑", "ФИО ↓", "Направление ↑", "Направление ↓" });

            if (userRole != "Гость" && userRole != "Клиент")
            {
                btnAddAppt = new Button
                {
                    Text = "Добавить запись",
                    Location = new Point(850, 45), Size = new Size(140, 28),
                    BackColor = Color.FromArgb(138, 132, 226), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Comic Sans MS", 9f, FontStyle.Bold)
                };
                btnAddAppt.Click += BtnAddAppt_Click;
                pTop.Controls.Add(btnAddAppt);
            }

            pTop.Controls.AddRange(new Control[] { txtSearch, cmbStatus, cmbVisitType, cmbSpecialty, cmbShift, cmbSort });

            flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(183, 211, 242),
                Padding = new Padding(10)
            };

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
            allDoctors.Clear();
            using var conn = DbHelper.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, full_name, specialty, shift, work_hours, visit_type, status, photo FROM Doctors ORDER BY full_name";
            using var r = cmd.ExecuteReader();
            var specs = new HashSet<string>();
            while (r.Read())
            {
                var d = new Doctor
                {
                    Id = r.GetInt32("id"),
                    FullName = r.GetString("full_name"),
                    Specialty = r.IsDBNull(r.GetOrdinal("specialty")) ? "" : r.GetString("specialty"),
                    Shift = r.IsDBNull(r.GetOrdinal("shift")) ? "" : r.GetString("shift"),
                    WorkHours = r.IsDBNull(r.GetOrdinal("work_hours")) ? "" : r.GetString("work_hours"),
                    VisitType = r.IsDBNull(r.GetOrdinal("visit_type")) ? "" : r.GetString("visit_type"),
                    Status = r.IsDBNull(r.GetOrdinal("status")) ? "" : r.GetString("status"),
                    Photo = r.IsDBNull(r.GetOrdinal("photo")) ? "" : r.GetString("photo")
                };
                allDoctors.Add(d);
                if (!string.IsNullOrEmpty(d.Specialty)) specs.Add(d.Specialty);
            }
            foreach (var s in specs) if (!cmbSpecialty.Items.Contains(s)) cmbSpecialty.Items.Add(s);
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string search = txtSearch.Text.ToLower();
            string status = cmbStatus.SelectedIndex > 0 ? cmbStatus.SelectedItem.ToString() : "";
            string vtype = cmbVisitType.SelectedIndex > 0 ? cmbVisitType.SelectedItem.ToString() : "";
            string spec = cmbSpecialty.SelectedIndex > 0 ? cmbSpecialty.SelectedItem.ToString() : "";
            string shift = cmbShift.SelectedIndex > 0 ? cmbShift.SelectedItem.ToString() : "";
            string sort = cmbSort.SelectedItem?.ToString() ?? "";

            filtered = new List<Doctor>(allDoctors);
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.FindAll(d => (d.FullName + d.Specialty + d.Shift + d.WorkHours + d.VisitType + d.Status).ToLower().Contains(search));
            if (!string.IsNullOrEmpty(status)) filtered = filtered.FindAll(d => d.Status == status);
            if (!string.IsNullOrEmpty(vtype)) filtered = filtered.FindAll(d => d.VisitType == vtype);
            if (!string.IsNullOrEmpty(spec)) filtered = filtered.FindAll(d => d.Specialty == spec);
            if (!string.IsNullOrEmpty(shift)) filtered = filtered.FindAll(d => d.Shift == shift);

            filtered = sort switch
            {
                "ФИО ↑" => new List<Doctor>(filtered).OrderBy(d => d.FullName).ToList(),
                "ФИО ↓" => new List<Doctor>(filtered).OrderByDescending(d => d.FullName).ToList(),
                "Направление ↑" => new List<Doctor>(filtered).OrderBy(d => d.Specialty).ToList(),
                "Направление ↓" => new List<Doctor>(filtered).OrderByDescending(d => d.Specialty).ToList(),
                _ => filtered
            };

            RenderCards();
        }

        private void RenderCards()
        {
            flowPanel.SuspendLayout();
            flowPanel.Controls.Clear();
            string imgDir = AppDomain.CurrentDomain.BaseDirectory + "Images\\";

            foreach (var d in filtered)
            {
                bool absent = d.Status != "Присутствует";
                var card = new Panel
                {
                    Size = new Size(380, 95),
                    BackColor = absent ? Color.LightGray : Color.White,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = d
                };

                var pic = new PictureBox { Location = new Point(5, 5), Size = new Size(70, 80), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.LightGray };
                try
                {
                    string photoPath = string.IsNullOrEmpty(d.Photo) ? "" : imgDir + d.Photo;
                    if (File.Exists(photoPath))
                        pic.Image = Image.FromFile(photoPath);
                    else
                        pic.Image = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + @"Resources\picture.png");
                }
                catch { }

                var lblName = new Label { Text = d.FullName, Font = new Font("Comic Sans MS", 9f, FontStyle.Bold), Location = new Point(85, 5), Size = new Size(285, 18) };
                var lblShift = new Label { Text = "Смена: " + d.Shift, Font = new Font("Comic Sans MS", 8f), Location = new Point(85, 23), Size = new Size(285, 15), ForeColor = Color.DimGray };
                var lblHours = new Label { Text = "Режим: " + d.WorkHours, Font = new Font("Comic Sans MS", 8f), Location = new Point(85, 38), Size = new Size(285, 15), ForeColor = Color.DimGray };
                var lblSpec = new Label { Text = "Направление: " + d.Specialty, Font = new Font("Comic Sans MS", 8f), Location = new Point(85, 53), Size = new Size(285, 15) };
                var lblVtype = new Label { Text = "Тип: " + d.VisitType, Font = new Font("Comic Sans MS", 8f), Location = new Point(85, 68), Size = new Size(180, 15) };
                var lblStatus = new Label { Text = d.Status, Font = new Font("Comic Sans MS", 8f, FontStyle.Bold), Location = new Point(270, 68), Size = new Size(100, 15), ForeColor = absent ? Color.OrangeRed : Color.Green };

                card.Controls.AddRange(new Control[] { pic, lblName, lblShift, lblHours, lblSpec, lblVtype, lblStatus });

                if (!absent && (userRole == "Сотрудник регистратуры" || userRole == "Администратор" || userRole == "Главный врач"))
                {
                    card.Cursor = Cursors.Hand;
                    card.Click += (s, e) => OpenAddAppointment((Doctor)((Panel)s).Tag);
                    foreach (Control c in card.Controls) c.Click += (s, e) => OpenAddAppointment((Doctor)card.Tag);
                }

                flowPanel.Controls.Add(card);
            }
            flowPanel.ResumeLayout();
            lblCount.Text = $"  Показано: {filtered.Count} из {allDoctors.Count}";
        }

        private void BtnAddAppt_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Выберите сотрудника из списка для добавления записи.", "Подсказка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenAddAppointment(Doctor d)
        {
            var form = new AddAppointmentForm(d, userId, userName);
            form.ShowDialog();
        }
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string Shift { get; set; } = "";
        public string WorkHours { get; set; } = "";
        public string VisitType { get; set; } = "";
        public string Status { get; set; } = "";
        public string Photo { get; set; } = "";
    }

    static class ListExtensions
    {
        public static List<T> OrderBy<T, TKey>(this List<T> list, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var r = new List<T>(list);
            r.Sort((a, b) => keySelector(a).CompareTo(keySelector(b)));
            return r;
        }
        public static List<T> OrderByDescending<T, TKey>(this List<T> list, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var r = new List<T>(list);
            r.Sort((a, b) => keySelector(b).CompareTo(keySelector(a)));
            return r;
        }
    }
}
