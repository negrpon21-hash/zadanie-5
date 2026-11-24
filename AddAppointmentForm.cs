using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace DoctorMagApp
{
    public class AddAppointmentForm : Form
    {
        private Doctor doctor;
        private int staffId;
        private string staffName;

        private Label lblStaff, lblRecDate, lblPatient, lblSpec, lblVtype, lblVisitDate, lblVisitTime;
        private TextBox txtStaff, txtRecDate, txtPolicy, txtPatient, txtSpec, txtVtype;
        private DateTimePicker dtpDate, dtpTime;
        private Button btnSave, btnBack;

        public AddAppointmentForm(Doctor doc, int sid, string sname)
        {
            doctor = doc; staffId = sid; staffName = sname;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "ДокторМаг - Добавление записи";
            this.ClientSize = new Size(500, 460);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(183, 211, 242);
            this.Font = new Font("Comic Sans MS", 9f);
            try { this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + @"Resources\icon.ico"); } catch { }

            int y = 20; int lx = 20; int fx = 200; int fw = 270;

            void AddRow(string label, Control ctrl)
            {
                this.Controls.Add(new Label { Text = label, Location = new Point(lx, y + 3), AutoSize = true, Font = new Font("Comic Sans MS", 9f) });
                ctrl.Location = new Point(fx, y);
                ctrl.Size = new Size(fw, 24);
                ctrl.Font = new Font("Comic Sans MS", 9f);
                this.Controls.Add(ctrl);
                y += 38;
            }

            txtStaff = new TextBox { Text = staffName, ReadOnly = true, BackColor = Color.AliceBlue };
            AddRow("Оформляет запись:", txtStaff);

            txtRecDate = new TextBox { Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), ReadOnly = true, BackColor = Color.AliceBlue };
            AddRow("Дата записи:", txtRecDate);

            var pnlPolicy = new Panel { Size = new Size(fw, 24) };
            var tbPolicy = new TextBox { Size = new Size(170, 24), Font = new Font("Comic Sans MS", 9f), PlaceholderText = "Номер полиса ОМС" };
            var btnFind = new Button { Text = "Найти", Location = new Point(178, 0), Size = new Size(90, 24), BackColor = Color.FromArgb(138, 132, 226), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            txtPatient = new TextBox { ReadOnly = true, BackColor = Color.AliceBlue };
            int patientId = 0;

            btnFind.Click += (s, e) =>
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, full_name FROM Patients WHERE policy_number=@p LIMIT 1";
                cmd.Parameters.AddWithValue("@p", tbPolicy.Text.Trim());
                using var r = cmd.ExecuteReader();
                if (r.Read()) { patientId = r.GetInt32("id"); txtPatient.Text = r.GetString("full_name"); }
                else MessageBox.Show("Пациент не найден", "Поиск", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            pnlPolicy.Controls.AddRange(new Control[] { tbPolicy, btnFind });
            this.Controls.Add(new Label { Text = "Полис ОМС:", Location = new Point(lx, y + 3), AutoSize = true, Font = new Font("Comic Sans MS", 9f) });
            pnlPolicy.Location = new Point(fx, y); pnlPolicy.Font = new Font("Comic Sans MS", 9f);
            this.Controls.Add(pnlPolicy); y += 38;

            txtPatient = new TextBox { ReadOnly = true, BackColor = Color.AliceBlue };
            AddRow("Пациент:", txtPatient);

            txtSpec = new TextBox { Text = doctor.Specialty, ReadOnly = true, BackColor = Color.AliceBlue };
            AddRow("Направление:", txtSpec);

            var cmbVtype = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbVtype.Items.AddRange(new[] { "Первичное", "Повторное", "Срочное" });
            cmbVtype.SelectedIndex = 0;
            AddRow("Тип посещения:", cmbVtype);

            dtpDate = new DateTimePicker { Format = DateTimePickerFormat.Short, MinDate = DateTime.Today };
            AddRow("Дата посещения:", dtpDate);

            dtpTime = new DateTimePicker { Format = DateTimePickerFormat.Time, ShowUpDown = true };
            dtpTime.Value = DateTime.Today.AddHours(8);
            AddRow("Время посещения:", dtpTime);

            btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(200, y), Size = new Size(130, 34),
                BackColor = Color.FromArgb(138, 132, 226), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Comic Sans MS", 10f, FontStyle.Bold)
            };
            btnSave.Click += (s, e) =>
            {
                if (patientId == 0) { MessageBox.Show("Укажите пациента по полису ОМС", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                // Validate time vs work hours
                if (!string.IsNullOrEmpty(doctor.WorkHours))
                {
                    var parts = doctor.WorkHours.Split('-');
                    if (parts.Length == 2 && TimeSpan.TryParse(parts[0], out var start) && TimeSpan.TryParse(parts[1], out var end))
                    {
                        var selTime = dtpTime.Value.TimeOfDay;
                        if (selTime < start || selTime > end)
                        {
                            MessageBox.Show($"Время посещения должно быть в рамках работы сотрудника: {doctor.WorkHours}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }

                using var conn = DbHelper.GetConnection();
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Appointments (patient_id, doctor_id, created_by_id, record_date, visit_date, visit_time, visit_type) VALUES (@pid,@did,@cid,@rd,@vd,@vt,@vtype)";
                cmd.Parameters.AddWithValue("@pid", patientId);
                cmd.Parameters.AddWithValue("@did", doctor.Id);
                cmd.Parameters.AddWithValue("@cid", staffId > 0 ? staffId : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@rd", DateTime.Now);
                cmd.Parameters.AddWithValue("@vd", dtpDate.Value.Date);
                cmd.Parameters.AddWithValue("@vt", dtpTime.Value.ToString("HH:mm:ss"));
                cmd.Parameters.AddWithValue("@vtype", cmbVtype.SelectedItem.ToString());
                cmd.ExecuteNonQuery();
                MessageBox.Show("Запись добавлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            btnBack = new Button
            {
                Text = "Назад",
                Location = new Point(345, y), Size = new Size(90, 34),
                BackColor = Color.FromArgb(175, 175, 220), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Comic Sans MS", 9f)
            };
            btnBack.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { btnSave, btnBack });
        }
    }
}
