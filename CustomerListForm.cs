using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Text;

namespace BravoProject
{
    public partial class CustomerListForm : Form
    {
        static CancellationTokenSource c_ts = new CancellationTokenSource();
        private static string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=BravoProject;Integrated Security=True;MultipleActiveResultSets=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private int totalRows = -1;
        private int currentIndex = 1;
        private int numberOfPages = 0;
        private int rowsPerPage = 30;
        private BindingSource bindingSource = new BindingSource();
        private SqlConnection connection = new SqlConnection(connectionString);
        public CustomerListForm()
        {
            InitializeComponent();
            //Setting up the DataGridView
            customerList.AllowUserToAddRows = false;
            customerList.AllowUserToDeleteRows = false;
            customerList.AllowUserToResizeRows = true;
            customerList.EditMode = DataGridViewEditMode.EditProgrammatically; 

            connection.Open();
            //Run in the first time to gen DB - serving for testing
            /*for (int i = 1; i <= 1500; i++)
            {
                string userCode = $"Code{i}";
                string name = $"User{i}";
                string addr = $"Addr{i}";
                var cmd = $"INSERT INTO DbUser (MaKH, TenKH, DiaChiKH) VALUES ('{userCode}', '{name}', '{addr}')";
                SqlCommand sqlCommand = new SqlCommand(cmd, connection);
                sqlCommand.ExecuteNonQuery();
            }*/
        }
        //Generate query string
        private string QueryString()
        {
            return $"select Id[Id], MaKH[Mã KH], TenKH[Tên KH], DiaChiKH[Địa chỉ KH] " +
                $"from DbUser ORDER BY (SELECT null) OFFSET {(currentIndex - 1) * rowsPerPage} ROWS FETCH NEXT {rowsPerPage} ROWS ONLY";
        }
        //Get data from the query & load into datagridview
        private async Task GetData(string selectCmd)
        {
            try
            {
                SqlCommand command = new SqlCommand(selectCmd, connection);
                DataTable dataTable = new DataTable
                {
                    Locale = CultureInfo.InvariantCulture
                };
                SqlDataReader reader = await command.ExecuteReaderAsync(c_ts.Token);
                dataTable.Load(reader);
                bindingSource.DataSource = dataTable;
            }
            catch (Exception e)
            {
                if(e is TaskCanceledException || e is SqlException)
                {
                    // c_ts.Dispose();
                    Console.WriteLine(e.Message); 
                    return;
                }
                MessageBox.Show(e.ToString());
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            customerList.DataSource = bindingSource;
            //Init query, serving for getting the number of rows in the DB.
            string query = "select id from DbUser";
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            SqlCommand cmd = new SqlCommand(query, connection);
            var reader = cmd.ExecuteReader();
            DataTable tempDt = new DataTable
            {
                Locale = CultureInfo.InvariantCulture
            };
            tempDt.Load(reader);
            totalRows = tempDt.Rows.Count;
            numberOfPages = (int)Math.Ceiling(totalRows * 1.0 / rowsPerPage);
            await GetData(QueryString());
            pageIndex.Text = $"Trang 1 / {numberOfPages}";
            prevBtn.Enabled = false;
            if (numberOfPages < 2) nextBtn.Enabled = false;

            connection.Close();
        }

        //Get the next page
        private async void Next_Click(object sender, EventArgs e)
        {
            //Cancel current running task and execute new task
            c_ts.Cancel();
            c_ts = new CancellationTokenSource();
            //Task
            currentIndex++;
            if (!prevBtn.Enabled) prevBtn.Enabled = true;
            if (currentIndex >= numberOfPages) nextBtn.Enabled = false;
            pageIndex.Text = $"Trang {currentIndex} / {numberOfPages}";
            await GetData(QueryString());
        }

        //Get the previous page
        private async void prevBtn_Click(object sender, EventArgs e)
        {
            //Cancel current running task and execute new task
            c_ts.Cancel();
            c_ts = new CancellationTokenSource();
            //Task
            currentIndex--;
            if (!nextBtn.Enabled) nextBtn.Enabled = true;
            if (currentIndex < 2) prevBtn.Enabled = false;
            pageIndex.Text = $"Trang {currentIndex} / {numberOfPages}";
            await GetData(QueryString());
        }
    }
}