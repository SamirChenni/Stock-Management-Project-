using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Stock_Management
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlConnection sqlConnection;
        //SqlDataAdapter sqlDataAdapter;
        SqlCommand sqlCommand;
        private string password, username;
        public MainWindow()
        {
            InitializeComponent();
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            sqlConnection = new SqlConnection(connectionString);
            Clear();
        }



        private void textUsername_MouseDown(object sender, MouseButtonEventArgs e)
        {
            userBox.Focus();
        }

        private void userBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!string.IsNullOrEmpty(userBox.Text) || userBox.Text.Length > 0)
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

        private void loginButton_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void loginButton_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            
            password = passwordBox.Password;
            username = userBox.Text;
            sqlConnection.Open();
            try
            {
                if(password != GetPasswordFromDB() || username != GetUsernameFromDB())
                {
                    password = "";
                    username = "";
                    MessageBox.Show("Either your username or password is incorrect!", "Error",MessageBoxButton.OK, MessageBoxImage.Information);
                    sqlConnection.Close();
                    return;
                }
                else
                {
                    SecondaryWindow secondaryWindow = new SecondaryWindow();
                    secondaryWindow.Show();
                    sqlConnection.Close();
                    this.Close();
                    
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            
        }

        private string GetPasswordFromDB()
        {
            string password = "";
            string query = "select Password from Admin where Id=@Id";
            sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Id", 1);
            object result = sqlCommand.ExecuteScalar();
            if(result != null)
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

        private void Clear()
        {

            try
            {
                userBox.Text = "";
                passwordBox.Password = "";
                userBox.Focus();
            }
            catch (Exception e)
            {

                MessageBox.Show(e.ToString());
            }
        }


        private void toggleShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password != "")
            {
                if (passwordBox.PasswordChar == '\0')
                {
                    passwordBox.PasswordChar = '•'; // Change '•' to the character you want to show for the password
                }
                else
                {
                    passwordBox.PasswordChar = '\0';
                }
            }
        }

    }
}
