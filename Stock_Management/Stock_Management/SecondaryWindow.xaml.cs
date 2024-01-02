using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Stock_Management
{
    /// <summary>
    /// Interaction logic for SecondaryWindow.xaml
    /// </summary>
    public partial class SecondaryWindow : Window
    {
        SqlConnection sqlConnection;
        SqlDataAdapter sqlDataAdapter;
        SqlCommand sqlCommand;

        private int ProdId = 0;
        private int clid = 0;
        private int totalCost = 0; 
        private string CombinedText = "";
        private DateTime dateTime = DateTime.Now;
        private string username, password;
        public SecondaryWindow()
        {
            InitializeComponent();
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            sqlConnection = new SqlConnection(connectionString);
            ShowCategories();
            GetData();
            ShowClient();
        }

        #region Billing
        private void FilterButton_Click(Object sender, RoutedEventArgs e)
        {
            try
            {
                if(txtFilter.Text.Trim() == "" || txtFilter.Text == null)
                {
                    ShowProdInfo();
                }
                else
                {
                    Filter();
                }
                
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void AddToBill_Click(Object sender, RoutedEventArgs e)
        {
            try
            {

                if(ClNametxtBox.Text.Trim() == "" || ClNametxtBox.Text == null)
                {
                    MessageBox.Show("Please enter the client name!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClNametxtBox.Focus();
                    return;
                }
                else if(PhonetxtBox.Text.Trim() == "" || PhonetxtBox.Text == null)
                {
                    MessageBox.Show("Please enter the phone number!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    PhonetxtBox.Focus();
                    return;
                }
                else if(ProdNametxtBox.Text.Trim() == "" || ProdNametxtBox.Text == null)
                {
                    MessageBox.Show("Please choose the product from the list on the right or just enter its name!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    ProdNametxtBox.Focus();
                    return;
                }
                else if(QuanttxtBox.Text.Trim() == "" || QuanttxtBox.Text == null)
                {
                    MessageBox.Show("Please choose the quantity!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    QuanttxtBox.Focus();
                    return;
                }
                else
                {
                    // Get the Product Id to use it later in the next query
                    int ProductId = 0;
                    int ClientId = 0;
                    string query = "select ProductId from Product where ProductName = @ProdName";
                    sqlCommand = new SqlCommand(query , sqlConnection);
                    sqlConnection.Open();
                    sqlCommand.Parameters.AddWithValue("@ProdName", ProdNametxtBox.Text);
                    object result = sqlCommand.ExecuteScalar(); 
                    if(result != null)
                    {
                        ProductId = Convert.ToInt32(result);
                    }
                    else
                    {
                        MessageBox.Show("Product unavailable!!!","Information",MessageBoxButton.OK,MessageBoxImage.Information); 
                        return;
                    }

                    //Get the client id
                    query = "select Id from Client where ClientName = @ClientName";
                    sqlCommand = new SqlCommand(query, sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@ClientName", ClNametxtBox.Text);
                    result = sqlCommand.ExecuteScalar();
                    if(result != null)
                    {
                        ClientId = Convert.ToInt32(result);
                    }
                    else
                    {
                        MessageBox.Show("Client doesn't exist , or He has not been added yet!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    if(GetQuantity(ProductId) < Convert.ToInt32(QuanttxtBox.Text))
                    {
                        MessageBox.Show("The quantity you choosed is bigger than the total Quantity!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else
                    {
                        //insert in ClientProd table
                        query = "insert into ClientProd (ClientId , ProductId , QuantityClient) values (@ClientId , @ProductId , @QuantityClient)";
                        sqlCommand = new SqlCommand(query, sqlConnection);
                        sqlCommand.Parameters.AddWithValue("@ClientId", ClientId);
                        sqlCommand.Parameters.AddWithValue("@ProductId", ProductId);
                        sqlCommand.Parameters.AddWithValue("@QuantityClient", QuanttxtBox.Text);
                        sqlCommand.ExecuteNonQuery();


                        //Update the quantity of product (it will decrease)
                        query = "update Product set Quantity = Quantity - @QuantityClient where ProductId = @ProductId";
                        sqlCommand = new SqlCommand(query, sqlConnection);
                        sqlCommand.Parameters.AddWithValue("@ProductId", ProductId);
                        sqlCommand.Parameters.AddWithValue("@QuantityClient", QuanttxtBox.Text);
                        sqlCommand.ExecuteNonQuery();

                        PrintBill(ClNametxtBox.Text , ProdNametxtBox.Text , QuanttxtBox.Text);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
                GetData();
            }
        }

        private void ShowClient()
        {
            string query = "select Id , ClientName , Date , Phone from Client ";
            sqlDataAdapter = new SqlDataAdapter(query, sqlConnection);
            using(sqlDataAdapter)
            {
                DataTable clientTable = new DataTable();
                sqlDataAdapter.Fill(clientTable);
                if(clientTable.Rows.Count > 0 || clientTable != null)
                {
                    dgClients.ItemsSource = clientTable.DefaultView;
                }
                else
                {
                    dgClients.ItemsSource = null;
                }
            }

        }

        private void ShowClientDetails(int id)
        {
            string query = "select ProductName , QuantityClient from Product inner join ClientProd on Product.ProductId = ClientProd.ProductId" +
                " where ClientProd.ClientId = @Id";
            sqlCommand = new SqlCommand(query, sqlConnection);
            sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            using (sqlDataAdapter)
            {
                sqlCommand.Parameters.AddWithValue("@Id", id);
                
                DataTable prodClientInfo = new DataTable();
                sqlDataAdapter.Fill(prodClientInfo);

                ClientInfo.ItemsSource = prodClientInfo.DefaultView;

            }
        }

        private void Filter()
        {
            string query = "select ProductName,Quantity from Product" +
                " where ProductName like @ProductName";
            sqlCommand = new SqlCommand(query, sqlConnection);
            sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            using (sqlDataAdapter)
            {
                sqlCommand.Parameters.AddWithValue("@ProductName", "%" + txtFilter.Text + "%");
                DataTable dt = new DataTable();
                sqlDataAdapter.Fill(dt);
                ProductsInfo.ItemsSource = dt.DefaultView;
            }

        }

        #endregion


        #region SelectedCellsChanged
        private void dg_Products_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                DataGrid dgr = (DataGrid)sender;  //Create a Datagrid selected cell changed event. to delete
                DataRowView dataRow = dgr.CurrentItem as DataRowView;  //Select the DataRowView to identify the selected row records.
                if (dataRow != null)
                {
                    if (dg_Products.Items.Count > 0)
                    {
                        if (dgr.SelectedCells.Count > 0)
                        {
                            ProdId = Int32.Parse(dataRow["ProductId"].ToString());
                            ProdTB.Text = dataRow["ProductName"].ToString();
                            QuantTB.Text = dataRow["Quantity"].ToString();
                            PriceTB.Text = dataRow["Price"].ToString();
                            CatCB.Text = dataRow["CategoryName"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void dgClients_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                DataGrid dgr = (DataGrid)sender;  //Create a Datagrid selected cell changed event. to delete
                DataRowView dataRow = dgr.CurrentItem as DataRowView;  //Select the DataRowView to identify the selected row records.
                if (dataRow != null)
                {
                    if (dg_Products.Items.Count > 0)
                    {
                        if (dgr.SelectedCells.Count > 0)
                        {
                            clid = Int32.Parse(dataRow["Id"].ToString());
                            ShowClientDetails(clid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        #endregion

        #region Client
        private void AddClient_Click(object sender , RoutedEventArgs e)
        {
            try
            {
                if (ClNametxtBox.Text.Trim() == "" || ClNametxtBox.Text == null)
                {
                    MessageBox.Show("Please enter the client name!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (PhonetxtBox.Text.Trim() == "" || PhonetxtBox.Text == null)
                {
                    MessageBox.Show("Please enter the phone number!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                   string query = "insert into Client (ClientName , Phone , Date)" +
                       " values (@ClientName , @Phone , @Date)";
                    sqlCommand = new SqlCommand(query, sqlConnection);
                    sqlConnection.Open();
                    sqlCommand.Parameters.AddWithValue("@ClientName", ClNametxtBox.Text);
                    sqlCommand.Parameters.AddWithValue("@Phone", PhonetxtBox.Text);
                    sqlCommand.Parameters.AddWithValue("@Date", dateTime);
                    sqlCommand.ExecuteNonQuery();
                    MessageBox.Show("Client has been added successfully!","Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
                ShowClient();
            }
        }
        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(clid > 0 )
                {
                    if(MessageBox.Show("Are you sure you want to delete this client","Information",MessageBoxButton.YesNo,MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        string query = "delete from Client where Id = @clientID";
                        sqlCommand = new SqlCommand(query, sqlConnection);
                        sqlConnection.Open();
                        sqlCommand.Parameters.AddWithValue("@clientID", clid);
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                clid = 0;
                sqlConnection.Close();
                ShowClient();
            }
        }
        #endregion

        #region Products and Categories 
        private void AddCatButton_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                if(CatTB.Text == null || CatTB.Text.Trim() == "")
                {
                    MessageBox.Show("If you want to add category please choose a name first to do that!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    CatTB.Focus();
                    return;
                }
                else
                {
                    string query = "insert into Category values (@Name)";
                    sqlCommand = new SqlCommand(query, sqlConnection);
                    sqlConnection.Open();
                    sqlCommand.Parameters.AddWithValue("@Name", CatTB.Text);
                    sqlCommand.ExecuteScalar();
                }
                
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
                ShowCategories();
            }
        }

        private void DeleteCatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CatList.SelectedValue == null )
                {
                    MessageBox.Show("If you want to delete category please choose one of them from the list above!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    CatTB.Focus();
                    return;
                }
                else
                {
                    string query = "delete from Category where Id = @Id";
                    sqlCommand = new SqlCommand(query, sqlConnection);
                    sqlConnection.Open();
                    sqlCommand.Parameters.AddWithValue("@Id", CatList.SelectedValue);
                    sqlCommand.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
                ShowCategories();
                GetData();
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProdTB.Text == null || ProdTB.Text.Trim() == "")
                {
                    MessageBox.Show("Please enter the product name!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    ProdTB.Focus();
                    return;
                }
                else if (QuantTB.Text == null || QuantTB.Text.Trim() == "")
                {
                    MessageBox.Show("Please enter the quantity !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    QuantTB.Focus();
                    return;
                }
                else if (PriceTB.Text == null || PriceTB.Text.Trim() == "")
                {
                    MessageBox.Show("Please enter the price !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    PriceTB.Focus();
                    return;
                }
                else if (CatCB.SelectedValue == null)
                {
                    MessageBox.Show("Please choose category!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    CatCB.Focus();
                    return;
                }
                else
                {
                    string query = "insert into Product values (@ProductName,@Quantity,@Price,@CategoryId)";
                    sqlCommand = new SqlCommand(query, sqlConnection);
                    sqlConnection.Open();
                    sqlCommand.Parameters.AddWithValue("@ProductName", ProdTB.Text);
                    sqlCommand.Parameters.AddWithValue("@Quantity", QuantTB.Text);
                    sqlCommand.Parameters.AddWithValue("@Price", PriceTB.Text);
                    sqlCommand.Parameters.AddWithValue("@CategoryId", CatCB.SelectedValue);
                    sqlCommand.ExecuteScalar();
                    MessageBox.Show("Product has been added successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    Clear();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                
                sqlConnection.Close();
                GetData();
            }
        }

        private void DeleteProdButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(ProdId > 0)
                {
                    if (MessageBox.Show("Are you sure you want to delete this product?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string query = "delete from Product where ProductId = @Id";
                        sqlCommand = new SqlCommand(query, sqlConnection);
                        sqlConnection.Open();
                        sqlCommand.Parameters.AddWithValue("@Id", ProdId);
                        sqlCommand.ExecuteScalar();
                        MessageBox.Show("Product has been deleted successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        Clear();
                    }
                }
                

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
                GetData();
                
            }
        }

        private void UpdateProdButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(ProdId > 0)
                {
                    if(MessageBox.Show("Are you sure you want to update this product?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string query = "update Product set ProductName = @ProductName , Quantity = @Quantity , Price = @Price , CategoryId=@CategoryId where ProductId = @Id";
                        sqlCommand = new SqlCommand(query, sqlConnection);
                        sqlConnection.Open();
                        sqlCommand.Parameters.AddWithValue("@Id", ProdId);
                        sqlCommand.Parameters.AddWithValue("@ProductName", ProdTB.Text);
                        sqlCommand.Parameters.AddWithValue("@Quantity", QuantTB.Text);
                        sqlCommand.Parameters.AddWithValue("@Price", PriceTB.Text);
                        sqlCommand.Parameters.AddWithValue("@CategoryId", CatCB.SelectedValue);
                        sqlCommand.ExecuteScalar();
                        MessageBox.Show("Product has been updated successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        Clear();
                    }
                    
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
                GetData();
            }
        }
        private void ShowCategories()
        {
            
            try
            {
                sqlDataAdapter = new SqlDataAdapter("select * from Category", sqlConnection);
                using (sqlDataAdapter)
                {
                    DataTable categoryTable = new DataTable();
                    sqlDataAdapter.Fill(categoryTable);

                    CatList.DisplayMemberPath = "CategoryName";
                    CatList.SelectedValuePath = "Id";
                    CatList.ItemsSource = categoryTable.DefaultView;
                    CatCB.DisplayMemberPath = "CategoryName";
                    CatCB.SelectedValuePath = "Id";
                    CatCB.ItemsSource = categoryTable.DefaultView;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }


        private void GetData()
        {
            string query = "select ProductId , ProductName , Quantity , Price , CategoryName from Product inner join Category on Product.CategoryId = Category.Id";
            sqlDataAdapter = new SqlDataAdapter(query, sqlConnection);
            using (sqlDataAdapter)
            {
                DataTable productTable = new DataTable();
                sqlDataAdapter.Fill(productTable);

                if (productTable.Rows.Count > 0 || productTable != null)
                {
                    dg_Products.ItemsSource = productTable.DefaultView;
                }
                else
                {
                    dg_Products.ItemsSource = null;
                }
            }
            ShowProdInfo();
            Alarm();
        }

        private void ShowProdInfo()
        {
            string query = "select ProductName,Quantity from Product ";
            sqlDataAdapter = new SqlDataAdapter(query, sqlConnection);
            using (sqlDataAdapter)
            {
                DataTable prodInfo = new DataTable();
                sqlDataAdapter.Fill(prodInfo);

                ProductsInfo.ItemsSource = prodInfo.DefaultView;

            }
        }
        #endregion

        #region Clear Buttons
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClNametxtBox.Text = string.Empty;
            ProdNametxtBox.Text = string.Empty;
            PhonetxtBox.Text = string.Empty;
            QuanttxtBox.Text = string.Empty;
            txtFilter.Text = string.Empty;
            ShowProdInfo();
        }
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }
        public void Clear()
        {
            ProdId = 0;
            ProdTB.Text = string.Empty;
            QuantTB.Text = string.Empty;
            PriceTB.Text = string.Empty;
            CatTB.Text = string.Empty;
            CatCB.SelectedValue = null;
            CatTB.Text = string.Empty;
        }
        #endregion

        #region Cursor stuff
        private void loginButton_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void QuantTB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ProdTB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void loginButton_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        #endregion

        #region SelectionChanged
        private void ProductsInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                DataRowView selectedRow = (DataRowView)ProductsInfo.SelectedItem;
                string query = "select ProductName from Product where ProductName = @ProductName";
                sqlCommand = new SqlCommand(query, sqlConnection);
                sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                using (sqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@ProductName", selectedRow.Row.ItemArray[0].ToString());
                    DataTable dt = new DataTable();
                    sqlDataAdapter.Fill(dt);
                    ProdNametxtBox.Text = dt.Rows[0]["ProductName"].ToString();
                }
            }
            catch (Exception ex)
            {

                //MessageBox.Show(ex.ToString());
            }

        }
        #endregion

        #region  Alarm and GetQuantity

        private void Alarm()
        {
            int[] ArrayQuantities;
            var quantities = new List<int>();
            string query = "select Quantity from Product";
            sqlCommand = new SqlCommand(query, sqlConnection);
            sqlConnection.Open();
            using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
            {
                // Check if the reader has rows before trying to read
                if (sqlDataReader.HasRows)
                {

                    while (sqlDataReader.Read())
                    {
                        int quantity = sqlDataReader.GetInt32(0);
                        quantities.Add(quantity);
                    }

                }
            }
            ArrayQuantities = quantities.ToArray();
            foreach (int item in ArrayQuantities)
            {
                if (item < 20)
                {
                    query = "select ProductName from Product where Quantity = @Quantity";
                    sqlCommand = new SqlCommand(query, sqlConnection);

                    sqlCommand.Parameters.AddWithValue("@Quantity", item);
                    object result = sqlCommand.ExecuteScalar();
                    if (result != null)
                    {
                        MessageBox.Show(result.ToString() + " Has low quantity , less than 20!!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        sqlConnection.Close();
                        return;
                    }

                }
                else
                {
                    sqlConnection.Close();
                    return;
                }
            }

        }

        private int GetQuantity(int Id)
        {
            int quantity = 0;
            string query = "select Quantity from Product where ProductId = @Id";
            sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Id", Id);
            object result = sqlCommand.ExecuteScalar();
            if(result != null)
            {
                 quantity = Convert.ToInt32(result);
            }
            return quantity;
        }
        #endregion

        private void PrintBill(string ClientName , string Product , string Quantity)
        {
            int price = 0;
            sqlCommand = new SqlCommand("select Price from Product where ProductName = @Product", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Product", Product);
            object result = sqlCommand.ExecuteScalar();
            if(result != null)
            {
                price = Convert.ToInt32(result);
            }
            if(CombinedText != "")
            {
                totalCost += price * Int32.Parse(Quantity);
                CombinedText = $" Product:{Product} - Quantity:{Quantity} - Total:{price * Int32.Parse(Quantity)},00 da";
                billingTxtBlock.Text += CombinedText + Environment.NewLine;
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ScrollableHeight);
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.ScrollableWidth);
                Result.Text = "Total: " + totalCost.ToString() + ",00 DA";
            }
            else
            {
                totalCost += price * Int32.Parse(Quantity);
                CombinedText = $"***********The Bill الفاتورة*************\n" +
                    $" Name:{ClientName} - Purchased products:\n" +
                    $" Product:{Product} - Quantity:{Quantity} - Total:{price*Int32.Parse(Quantity)},00 da";
                billingTxtBlock.Text += CombinedText + Environment.NewLine;
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ScrollableHeight);
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.ScrollableWidth);
                Result.Text ="Total: "+totalCost.ToString()+",00 DA";
            }
        }
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if(printDialog.ShowDialog() == true)
            {
                billingTxtBlock.Text += Result.Text+"        Date:"+ dateTime + Environment.NewLine;
                FlowDocument flowDocument = new FlowDocument(new Paragraph(new Run(billingTxtBlock.Text)));
                flowDocument.Name = "FlowDoc";
                IDocumentPaginatorSource idpSource = flowDocument;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Printing TextBlock Content");
            }
            billingTxtBlock.Text = "";
            totalCost = 0;
            Result.Text = "";
            CombinedText = "";
        }



        #region Login staff

        private void textUsername_MouseDown(object sender, MouseButtonEventArgs e)
        {
            userBox.Focus();
        }

        private void userBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(userBox.Text) || userBox.Text.Length > 0)
            {
                textUsername.Visibility = Visibility.Collapsed;
            }
            else
            {
                textUsername.Visibility = Visibility.Visible;
            }

        }

        private void textPassword_MouseDown(object sender, MouseButtonEventArgs e)
        {
            passwordBox.Focus();
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(passwordBox.Password) || passwordBox.Password.Length > 0)
            {
                textPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                textPassword.Visibility = Visibility.Visible;
            }
        }
        private void newUsernameTxtBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            newUserBox.Focus();
        }

        private void newUserBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(newUserBox.Text) || newUserBox.Text.Length > 0)
            {
                newUsernameTxt.Visibility = Visibility.Collapsed;
            }
            else
            {
                newUsernameTxt.Visibility = Visibility.Visible;
            }
        }

        private void newTextPassword_MouseDown(object sender, MouseButtonEventArgs e)
        {
            newPasswordBox.Focus();
        }

        private void newPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(newPasswordBox.Password) || newPasswordBox.Password.Length > 0)
            {
                newTextPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                newTextPassword.Visibility = Visibility.Visible;
            }
        }
        #endregion



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (userBox.Text.Trim() == "" || userBox.Text == null)
                {
                    MessageBox.Show("Please enter the actual username !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    userBox.Focus();
                    return;
                }
                else if (newUserBox.Text.Trim() == "" || newUserBox.Text == null)
                {
                    MessageBox.Show("Please enter the new username !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    newUserBox.Focus();
                    return;
                }
                else
                {
                    username = userBox.Text;
                    sqlConnection.Open();
                    if (username != GetUsernameFromDB())
                    {
                        MessageBox.Show("Your username is wrong!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else
                    {
                        if (MessageBox.Show("Are you sure you want to change your username!", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            String query = "update Admin set Username = @Username where Id = @Id";
                            sqlCommand = new SqlCommand(query, sqlConnection);
                            sqlCommand.Parameters.AddWithValue("@Username", newUserBox.Text);
                            sqlCommand.Parameters.AddWithValue("@Id", 1);
                            sqlCommand.ExecuteNonQuery();
                            MessageBox.Show("Username has been updated successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            
        }
        private void SavePWButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (passwordBox.Password.Trim() == "" || passwordBox.Password == null)
                {
                    MessageBox.Show("Please enter the actual password !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    passwordBox.Focus();
                    return;
                }
                else if (newPasswordBox.Password.Trim() == "" || newPasswordBox.Password == null)
                {
                    MessageBox.Show("Please enter the new password !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    newPasswordBox.Focus();
                    return;
                }
                else
                {
                    password = passwordBox.Password;
                    sqlConnection.Open();
                    if (password != GetPasswordFromDB())
                    {
                        MessageBox.Show("Your password is wrong!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else
                    {
                        if (MessageBox.Show("Are you sure you want to change your password!", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            String query = "update Admin set Password = @Password where Id = @Id";
                            sqlCommand = new SqlCommand(query, sqlConnection);
                            sqlCommand.Parameters.AddWithValue("@Password", newPasswordBox.Password);
                            sqlCommand.Parameters.AddWithValue("@Id", 1);
                            sqlCommand.ExecuteNonQuery();
                            MessageBox.Show("Password has been updated successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
            
        }
        private void ClearButton_Click_1(object sender, RoutedEventArgs e)
        {
            userBox.Text = String.Empty;
            textUsername.Visibility = Visibility.Visible;
            newUserBox.Text = String.Empty;
            newUsernameTxt.Visibility = Visibility.Visible;
            passwordBox.Password = String.Empty;
            textPassword.Visibility = Visibility.Visible;
            newPasswordBox.Password = String.Empty;
            newTextPassword.Visibility = Visibility.Visible;
            userBox.Focus();
        }

        private string GetPasswordFromDB()
        {
            string password = "";
            string query = "select Password from Admin where Id=@Id";
            sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Id", 1);
            object result = sqlCommand.ExecuteScalar();
            if (result != null)
            {
                password = result.ToString();
            }
            return password;
        }

        private string GetUsernameFromDB()
        {
            string username = "";
            string query = "select Username from Admin where Id=@Id";
            sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Id", 1);
            object result = sqlCommand.ExecuteScalar();
            if (result != null)
            {
                username = result.ToString();
            }
            return username;
        }

        
    }
}
