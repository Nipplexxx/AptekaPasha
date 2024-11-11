using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace AptekaPasha
{
    public partial class OrderForm : Form
    {
        private readonly string connectionString = "Data Source=ACER;Initial Catalog=BD_apteka;Integrated Security=True;Encrypt=False";
        private int currentPage = 1;
        private int totalPages = 1;
        private const int recordsPerPage = 10;
        public int CustomerId { get; private set; }
        public List<Tuple<int, int, decimal>> SelectedItems { get; private set; }

        public OrderForm()
        {
            InitializeComponent();
            LoadMedicines(currentPage);
        }

        private void LoadMedicines(int page)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string countQuery = "SELECT COUNT(*) FROM Medicine";
                SqlCommand countCommand = new SqlCommand(countQuery, connection);
                connection.Open();
                int totalRecords = (int)countCommand.ExecuteScalar();
                totalPages = (int)Math.Ceiling((double)totalRecords / recordsPerPage);

                string query = $@"
                    SELECT Id, Name, Price, StockQuantity
                    FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY Id) AS RowNum, Id, Name, Price, StockQuantity
                        FROM Medicine
                    ) AS RowConstrainedResult
                    WHERE RowNum >= {(page - 1) * recordsPerPage + 1} AND RowNum <= {page * recordsPerPage}
                    ORDER BY RowNum";

                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                dataGridViewMedicines.DataSource = dataTable;
                dataGridViewMedicines.Columns["Id"].Visible = false;

                if (!dataGridViewMedicines.Columns.Contains("Select"))
                {
                    dataGridViewMedicines.Columns.Insert(0, new DataGridViewCheckBoxColumn { Name = "Select", HeaderText = "Выбрать" });
                    dataGridViewMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Количество" });
                }

                UpdatePaginationControls();
            }
        }

        private void UpdatePaginationControls()
        {
            btnPrevious.Enabled = currentPage > 1;
            btnNext.Enabled = currentPage < totalPages;
            lblPage.Text = $"Страница {currentPage} из {totalPages}";
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadMedicines(currentPage);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadMedicines(currentPage);
            }
        }

        private void btnSubmitOrder_Click(object sender, EventArgs e)
        {
            string firstName = txtFirstName.Text;
            string lastName = txtLastName.Text;
            DateTime birthDate = dtpBirthDate.Value;
            string phone = txtPhone.Text;
            string email = txtEmail.Text;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Пожалуйста, заполните все данные клиента.");
                return;
            }

            CustomerId = AddCustomer(firstName, lastName, birthDate, phone, email);
            if (CustomerId == -1) return;

            SelectedItems = new List<Tuple<int, int, decimal>>();
            decimal totalAmount = 0;

            foreach (DataGridViewRow row in dataGridViewMedicines.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    int medicineId = Convert.ToInt32(row.Cells["Id"].Value);
                    int quantity = Convert.ToInt32(row.Cells["Quantity"].Value);
                    decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
                    int stock = Convert.ToInt32(row.Cells["StockQuantity"].Value);

                    if (quantity > stock)
                    {
                        MessageBox.Show($"Недостаточно товара {row.Cells["Name"].Value} на складе.");
                        return;
                    }

                    totalAmount += price * quantity;
                    SelectedItems.Add(new Tuple<int, int, decimal>(medicineId, quantity, price));
                }
            }

            if (!SelectedItems.Any())
            {
                MessageBox.Show("Выберите хотя бы один товар.");
                return;
            }

            ProcessOrder(totalAmount);
            MessageBox.Show($"Заказ успешно оформлен! Итоговая сумма: {totalAmount:C}");

            DialogResult = DialogResult.OK;
            Close();
        }

        private int AddCustomer(string firstName, string lastName, DateTime birthDate, string phone, string email)
        {
            string query = "INSERT INTO Customer (FirstName, LastName, DateOfBirth, Phone, Email) OUTPUT INSERTED.Id VALUES (@FirstName, @LastName, @BirthDate, @Phone, @Email)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", firstName);
                        command.Parameters.AddWithValue("@LastName", lastName);
                        command.Parameters.AddWithValue("@BirthDate", birthDate);
                        command.Parameters.AddWithValue("@Phone", phone);
                        command.Parameters.AddWithValue("@Email", email);

                        return (int)command.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при добавлении клиента: " + ex.Message);
                    return -1;
                }
            }
        }

        private void ProcessOrder(decimal totalAmount)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string insertOrderQuery = "INSERT INTO OrderTable (CustomerId, OrderDate, TotalAmount) OUTPUT INSERTED.Id VALUES (@CustomerId, @OrderDate, @TotalAmount)";
                    int orderId;

                    using (SqlCommand orderCommand = new SqlCommand(insertOrderQuery, connection, transaction))
                    {
                        orderCommand.Parameters.AddWithValue("@CustomerId", CustomerId);
                        orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                        orderCommand.Parameters.AddWithValue("@TotalAmount", totalAmount);

                        orderId = (int)orderCommand.ExecuteScalar();
                    }

                    foreach (var item in SelectedItems)
                    {
                        int medicineId = item.Item1;
                        int quantity = item.Item2;
                        decimal unitPrice = item.Item3;

                        string updateStockQuery = "UPDATE Medicine SET StockQuantity = StockQuantity - @Quantity WHERE Id = @Id";
                        using (SqlCommand updateCommand = new SqlCommand(updateStockQuery, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@Quantity", quantity);
                            updateCommand.Parameters.AddWithValue("@Id", medicineId);
                            updateCommand.ExecuteNonQuery();
                        }

                        string insertOrderItemQuery = "INSERT INTO OrderItem (OrderId, MedicineId, Quantity, UnitPrice) VALUES (@OrderId, @MedicineId, @Quantity, @UnitPrice)";
                        using (SqlCommand itemCommand = new SqlCommand(insertOrderItemQuery, connection, transaction))
                        {
                            itemCommand.Parameters.AddWithValue("@OrderId", orderId);
                            itemCommand.Parameters.AddWithValue("@MedicineId", medicineId);
                            itemCommand.Parameters.AddWithValue("@Quantity", quantity);
                            itemCommand.Parameters.AddWithValue("@UnitPrice", unitPrice);
                            itemCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка при оформлении заказа: " + ex.Message);
                }
            }
        }
    }
}
