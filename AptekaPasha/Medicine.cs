﻿using System;
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
        private int currentPage = 1; // Текущая страница
        private int totalPages = 1;  // Общее количество страниц
        private const int recordsPerPage = 10; // Количество записей на странице

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
            string countQuery;

            // Определение запроса в зависимости от таблицы
            if (tableName == "OrderTable")
            {
                query = $@"
                SELECT o.Id, o.OrderDate, c.FirstName + ' ' + c.LastName AS CustomerName, o.TotalAmount
                FROM OrderTable o
                JOIN Customer c ON o.CustomerId = c.Id
                ORDER BY o.Id
                OFFSET {(currentPage - 1) * recordsPerPage} ROWS FETCH NEXT {recordsPerPage} ROWS ONLY";

                countQuery = "SELECT COUNT(*) FROM OrderTable";
            }
            else if (tableName == "OrderItem")
            {
                query = $@"
                SELECT oi.Id, oi.OrderId, m.Name AS MedicineName, oi.Quantity, oi.UnitPrice
                FROM OrderItem oi
                JOIN Medicine m ON oi.MedicineId = m.Id
                ORDER BY oi.Id
                OFFSET {(currentPage - 1) * recordsPerPage} ROWS FETCH NEXT {recordsPerPage} ROWS ONLY";

                countQuery = "SELECT COUNT(*) FROM OrderItem";
            }
            else if (tableName == "Check")
            {
                query = $@"
                SELECT c.FirstName + ' ' + c.LastName AS CustomerName, m.Name AS MedicineName, oi.Quantity, o.OrderDate, oi.UnitPrice, (oi.Quantity * oi.UnitPrice) AS TotalPrice
                FROM OrderTable o
                JOIN Customer c ON o.CustomerId = c.Id
                JOIN OrderItem oi ON o.Id = oi.OrderId
                JOIN Medicine m ON oi.MedicineId = m.Id
                ORDER BY o.Id
                OFFSET {(currentPage - 1) * recordsPerPage} ROWS FETCH NEXT {recordsPerPage} ROWS ONLY";

                countQuery = "SELECT COUNT(*) FROM OrderItem"; // Обновите это, если нужно получить общее количество для другого запроса
            }
            else
            {
                query = $@"
                SELECT * FROM {tableName}
                ORDER BY Id
                OFFSET {(currentPage - 1) * recordsPerPage} ROWS FETCH NEXT {recordsPerPage} ROWS ONLY";

                countQuery = $"SELECT COUNT(*) FROM {tableName}";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Получение общего количества записей
                    SqlCommand countCommand = new SqlCommand(countQuery, connection);
                    int totalRecords = (int)countCommand.ExecuteScalar();
                    totalPages = (int)Math.Ceiling(totalRecords / (double)recordsPerPage);

                    // Загрузка данных для текущей страницы
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridViewMedicine.DataSource = dataTable;

                    // Скрытие колонки Id, если она существует
                    if (dataGridViewMedicine.Columns.Contains("Id"))
                    {
                        dataGridViewMedicine.Columns["Id"].Visible = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading data: " + ex.Message);
                }
            }

            UpdatePaginationControls();
        }

        private void UpdatePaginationControls()
        {
            btnPrevious.Enabled = currentPage > 1;
            btnNext.Enabled = currentPage < totalPages;
            lblPage.Text = $"Page {currentPage} from {totalPages}";
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadData(currentTableName);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadData(currentTableName);
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

        // Операции с данными (например, добавление, редактирование, удаление)
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
                    MessageBox.Show("Error while performing the operation: " + ex.Message);
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
                MessageBox.Show("Select the entry to edit.");
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
                MessageBox.Show("Select the record to delete.");
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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnPlaceOrder_Click(object sender, EventArgs e)
        {
            using (OrderForm orderForm = new OrderForm())
            {
                if (orderForm.ShowDialog() == DialogResult.OK)
                {
                    int customerId = orderForm.CustomerId;
                    List<Tuple<int, int, decimal>> selectedItems = orderForm.SelectedItems;

                    AddOrder(customerId, selectedItems);
                }
            }
        }

        private void AddOrder(int customerId, List<Tuple<int, int, decimal>> items)
        {
            string orderQuery = "INSERT INTO OrderTable (CustomerId, OrderDate, TotalAmount) " +
                                "OUTPUT INSERTED.Id VALUES (@CustomerId, @OrderDate, @TotalAmount)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        // Проверка на существование заказа для клиента (например, по дате)
                        string checkOrderQuery = "SELECT COUNT(*) FROM OrderTable WHERE CustomerId = @CustomerId AND OrderDate = @OrderDate";
                        SqlCommand checkOrderCommand = new SqlCommand(checkOrderQuery, connection, transaction);
                        checkOrderCommand.Parameters.AddWithValue("@CustomerId", customerId);
                        checkOrderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now.Date); // Проверка только по дате (можно уточнить)

                        int existingOrders = (int)checkOrderCommand.ExecuteScalar();

                        if (existingOrders > 0)
                        {
                            MessageBox.Show("Заказ для данного клиента уже существует на сегодня.");
                            return; // Не создаем заказ
                        }

                        // Создаем новый заказ
                        SqlCommand orderCommand = new SqlCommand(orderQuery, connection, transaction);
                        orderCommand.Parameters.AddWithValue("@CustomerId", customerId);
                        orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);

                        decimal totalAmount = 0;
                        foreach (var item in items)
                        {
                            int medicineId = item.Item1;
                            int quantity = item.Item2;
                            decimal unitPrice = item.Item3;

                            totalAmount += unitPrice * quantity;
                        }

                        orderCommand.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        int orderId = (int)orderCommand.ExecuteScalar();

                        foreach (var item in items)
                        {
                            int medicineId = item.Item1;
                            int quantity = item.Item2;
                            decimal unitPrice = item.Item3;

                            // Проверка на существование товара в заказе
                            string checkOrderItemQuery = "SELECT COUNT(*) FROM OrderItem WHERE OrderId = @OrderId AND MedicineId = @MedicineId";
                            SqlCommand checkOrderItemCommand = new SqlCommand(checkOrderItemQuery, connection, transaction);
                            checkOrderItemCommand.Parameters.AddWithValue("@OrderId", orderId);
                            checkOrderItemCommand.Parameters.AddWithValue("@MedicineId", medicineId);

                            int existingOrderItems = (int)checkOrderItemCommand.ExecuteScalar();

                            if (existingOrderItems > 0)
                            {
                                MessageBox.Show($"Product {medicineId} has already been added to this order.");
                                continue; // Пропустить этот товар, если он уже есть в заказе
                            }

                            // Добавляем товар в заказ
                            SqlCommand orderItemCommand = new SqlCommand("INSERT INTO OrderItem (OrderId, MedicineId, Quantity, UnitPrice) VALUES (@OrderId, @MedicineId, @Quantity, @UnitPrice)", connection, transaction);
                            orderItemCommand.Parameters.AddWithValue("@OrderId", orderId);
                            orderItemCommand.Parameters.AddWithValue("@MedicineId", medicineId);
                            orderItemCommand.Parameters.AddWithValue("@Quantity", quantity);
                            orderItemCommand.Parameters.AddWithValue("@UnitPrice", unitPrice);

                            orderItemCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        MessageBox.Show("The order has been successfully placed!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error when placing an order: " + ex.Message);
                }
            }
        }
    }
}