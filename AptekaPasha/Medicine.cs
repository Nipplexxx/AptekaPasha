using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AptekaPasha
{
    public partial class Medicine : Form
    {
        private readonly string connectionString = "Data Source=ACER;Initial Catalog=BD_apteka;Integrated Security=True;Encrypt=False";
        public Medicine()
        {
            InitializeComponent();
        }

        private void Medicine_Load(object sender, EventArgs e)
        {
            this.Opacity = 1;
            LoadMedicineData();  // загрузка данных

            dataGridViewMedicine.Dock = DockStyle.Fill;

            dataGridViewMedicine.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // вскрыл колонку Id
            if (dataGridViewMedicine.Columns.Contains("Id"))
            {
                dataGridViewMedicine.Columns["Id"].Visible = false;
            }

            flowLayoutPanelButtons.Dock = DockStyle.Bottom;
            flowLayoutPanelButtons.FlowDirection = FlowDirection.LeftToRight;
            flowLayoutPanelButtons.AutoSize = true;
            flowLayoutPanelButtons.HorizontalScroll.Visible = false;
            flowLayoutPanelButtons.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        // загрузка данных
        private void LoadMedicineData()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT * FROM Medicine";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridViewMedicine.DataSource = dataTable;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
                }
            }
        }

        // добавление новой записи
        private void btnAdd_Click_1(object sender, EventArgs e)
        {
            if (dataGridViewMedicine.CurrentRow == null)
            {
                MessageBox.Show("Выберите запись для добавления.");
                return;
            }

            string name = Convert.ToString(dataGridViewMedicine.CurrentRow.Cells["Name"].Value);
            string description = Convert.ToString(dataGridViewMedicine.CurrentRow.Cells["Description"].Value);
            decimal price = Convert.ToDecimal(dataGridViewMedicine.CurrentRow.Cells["Price"].Value);
            int stockQuantity = Convert.ToInt32(dataGridViewMedicine.CurrentRow.Cells["StockQuantity"].Value);
            DateTime expiryDate = Convert.ToDateTime(dataGridViewMedicine.CurrentRow.Cells["ExpiryDate"].Value);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "INSERT INTO Medicine (Name, Description, Price, StockQuantity, ExpiryDate) VALUES (@Name, @Description, @Price, @StockQuantity, @ExpiryDate)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@StockQuantity", stockQuantity);
                        command.Parameters.AddWithValue("@ExpiryDate", expiryDate);

                        command.ExecuteNonQuery();
                    }
                    LoadMedicineData(); // перезагрузка
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при добавлении записи: " + ex.Message);
                }
            }
        }

        // редактирование записи
        private void btnEdit_Click_1(object sender, EventArgs e)
        {
            if (dataGridViewMedicine.CurrentRow == null)
            {
                MessageBox.Show("Выберите запись для редактирования.");
                return;
            }

            int id = Convert.ToInt32(dataGridViewMedicine.CurrentRow.Cells["Id"].Value);
            string name = Convert.ToString(dataGridViewMedicine.CurrentRow.Cells["Name"].Value);
            string description = Convert.ToString(dataGridViewMedicine.CurrentRow.Cells["Description"].Value);
            decimal price = Convert.ToDecimal(dataGridViewMedicine.CurrentRow.Cells["Price"].Value);
            int stockQuantity = Convert.ToInt32(dataGridViewMedicine.CurrentRow.Cells["StockQuantity"].Value);
            DateTime expiryDate = Convert.ToDateTime(dataGridViewMedicine.CurrentRow.Cells["ExpiryDate"].Value);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE Medicine SET Name = @Name, Description = @Description, Price = @Price, StockQuantity = @StockQuantity, ExpiryDate = @ExpiryDate WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@StockQuantity", stockQuantity);
                        command.Parameters.AddWithValue("@ExpiryDate", expiryDate);

                        command.ExecuteNonQuery();
                    }
                    LoadMedicineData(); // нужно больше перезагрузок
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при редактировании записи: " + ex.Message);
                }
            }
        }

        // удаление записи
        private void btnDelete_Click_1(object sender, EventArgs e)
        {
            if (dataGridViewMedicine.CurrentRow == null)
            {
                MessageBox.Show("Выберите запись для удаления.");
                return;
            }

            int id = Convert.ToInt32(dataGridViewMedicine.CurrentRow.Cells["Id"].Value);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "DELETE FROM Medicine WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                    LoadMedicineData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении записи: " + ex.Message);
                }
            }
        }

        // обновление данных
        private void btnLoadData_Click_1(object sender, EventArgs e)
        {
            LoadMedicineData();
        }

        private void medicineToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
