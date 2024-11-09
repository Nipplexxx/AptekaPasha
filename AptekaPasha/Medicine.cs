using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace AptekaPasha
{
    public partial class Medicine : Form
    {
        private readonly string connectionString = "Data Source=ACER;Initial Catalog=BD_apteka;Integrated Security=True;Encrypt=False";
        private string currentTableName;

        public Medicine()
        {
            InitializeComponent();
        }

        private void Medicine_Load(object sender, EventArgs e)
        {
            this.Opacity = 1;
            LoadData("Medicine");  // загрузка данных по умолчанию

            dataGridViewMedicine.Dock = DockStyle.Fill;
            dataGridViewMedicine.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            flowLayoutPanelButtons.Dock = DockStyle.Bottom;
            flowLayoutPanelButtons.FlowDirection = FlowDirection.LeftToRight;
            flowLayoutPanelButtons.AutoSize = true;
            flowLayoutPanelButtons.HorizontalScroll.Visible = false;
            flowLayoutPanelButtons.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        private void LoadData(string tableName)
        {
            currentTableName = tableName;
            string query;

            // Определение запроса в зависимости от таблицы
            if (tableName == "OrderTable")
            {
                // JOIN для замены CustomerId на имя клиента
                query = @"
            SELECT o.Id, o.OrderDate, c.FirstName + ' ' + c.LastName AS CustomerName, o.TotalAmount
            FROM OrderTable o
            JOIN Customer c ON o.CustomerId = c.Id";
            }
            else if (tableName == "OrderItem")
            {
                // JOIN для замены MedicineId на название медикамента
                query = @"
            SELECT oi.Id, oi.OrderId, m.Name AS MedicineName, oi.Quantity, oi.UnitPrice
            FROM OrderItem oi
            JOIN Medicine m ON oi.MedicineId = m.Id";
            }
            else if (tableName == "Check")
            {
                // Новый запрос для отображения данных чека
                query = @"
            SELECT 
                c.FirstName + ' ' + c.LastName AS CustomerName,
                m.Name AS MedicineName,
                oi.Quantity,
                o.OrderDate,
                oi.UnitPrice,
                (oi.Quantity * oi.UnitPrice) AS TotalPrice
            FROM OrderTable o
            JOIN Customer c ON o.CustomerId = c.Id
            JOIN OrderItem oi ON o.Id = oi.OrderId
            JOIN Medicine m ON oi.MedicineId = m.Id";
            }
            else
            {
                // Для других таблиц - простой запрос
                query = $"SELECT * FROM {tableName}";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridViewMedicine.DataSource = dataTable;

                    // скрытие колонки Id, если она существует
                    if (dataGridViewMedicine.Columns.Contains("Id"))
                    {
                        dataGridViewMedicine.Columns["Id"].Visible = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
                }
            }
        }

        // Добавление пункта меню для отображения чека
        private void CheckToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadData("Check");
        }



        // Обработчики для пунктов меню
        private void medicineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadData("Medicine");
        }

        private void customerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadData("Customer");
        }

        private void orderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadData("OrderTable");
        }

        private void orderItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadData("OrderItem");
        }

        private void ExecuteQuery(string query, SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        command.ExecuteNonQuery();
                    }
                    LoadData(currentTableName); // перезагрузка данных
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при выполнении операции: " + ex.Message);
                }
            }
        }

        // добавление новой записи
        private void btnAdd_Click_1(object sender, EventArgs e)
        {
            var columns = dataGridViewMedicine.Columns;
            var values = new List<string>();
            var parameters = new List<SqlParameter>();

            for (int i = 1; i < columns.Count; i++) // Пропускаем Id
            {
                string columnName = columns[i].Name;
                object value = dataGridViewMedicine.CurrentRow.Cells[columnName].Value;
                values.Add($"@{columnName}");
                parameters.Add(new SqlParameter($"@{columnName}", value ?? DBNull.Value));
            }

            string query = $"INSERT INTO {currentTableName} ({string.Join(", ", columns.Cast<DataGridViewColumn>().Skip(1).Select(c => c.Name))}) VALUES ({string.Join(", ", values)})";
            ExecuteQuery(query, parameters.ToArray());
        }

        // редактирование записи
        private void btnEdit_Click_1(object sender, EventArgs e)
        {
            if (dataGridViewMedicine.CurrentRow == null || !dataGridViewMedicine.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите запись для редактирования.");
                return;
            }

            int id = Convert.ToInt32(dataGridViewMedicine.CurrentRow.Cells["Id"].Value);
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };
            var setClause = new List<string>();

            for (int i = 1; i < dataGridViewMedicine.Columns.Count; i++) // Пропускаем Id
            {
                string columnName = dataGridViewMedicine.Columns[i].Name;
                object value = dataGridViewMedicine.CurrentRow.Cells[columnName].Value;
                setClause.Add($"{columnName} = @{columnName}");
                parameters.Add(new SqlParameter($"@{columnName}", value ?? DBNull.Value));
            }

            string query = $"UPDATE {currentTableName} SET {string.Join(", ", setClause)} WHERE Id = @Id";
            ExecuteQuery(query, parameters.ToArray());
        }

        // удаление записи
        private void btnDelete_Click_1(object sender, EventArgs e)
        {
            if (dataGridViewMedicine.CurrentRow == null || !dataGridViewMedicine.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите запись для удаления.");
                return;
            }

            int id = Convert.ToInt32(dataGridViewMedicine.CurrentRow.Cells["Id"].Value);
            string query = $"DELETE FROM {currentTableName} WHERE Id = @Id";
            ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@Id", id) });
        }

        // обновление данных
        private void btnLoadData_Click_1(object sender, EventArgs e)
        {
            LoadData(currentTableName);
        }
    }
}