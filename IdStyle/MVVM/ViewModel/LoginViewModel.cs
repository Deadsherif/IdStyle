using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IdStyle.MVVM.View;
using IdStyle.MVVM.ViewModel;

namespace IdStyle.MVVM.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        private string email;

        public string Email
        {
            get { return email; }
            set { email = value; OnPropertyChanged(nameof(Email)); }
        }
        private string password;

        public string Password
        {
            get { return password; }
            set { password = value; OnPropertyChanged(nameof(Password)); }
        }


        private string loginResponse;

        public string LoginResponse
        {
            get { return loginResponse; }
            set { loginResponse = value; OnPropertyChanged(nameof(LoginResponse)); }
        }


        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login);
        }
        public MainWindow MainWindow { get; set; }


        private void Login(object parameter)
        {
           
            ////Authenticate the user
            //if (AuthenticateUser(Email, Password))
            //{
            //   // Assuming the parent MainWindow has a method to navigate to MainPage

            //   MainWindow?.NavigateToMainPage(); // Navigate to MainPage after successful login
            //}
            //else
            //{
            //    // Show some error message or feedback
            //}
        }

        //private bool AuthenticateUser(string email, string password)
        //{
        //    string domain = "https://app.mimar.tech";
        //    string endpoint = $"{domain}/api/login";
        //    RestClient restClient = new RestClient();
        //    var request = new RestRequest(endpoint, Method.Post);
        //    // Add any headers, parameters, or body content as needed
        //    request.AddHeader("Content-Type", "application/json");
        //    request.AddJsonBody(new 
        //    {
        //        email = $"{email}",
        //        password = $"{password}",
        //    });
        //    var response = restClient.Execute(request);
        //    if (response.IsSuccessful)
        //    {
        //        LoginResponse = response.Content;
        //        return true;
        //    }
        //    else { 
        //        LoginResponse = "Invailed Email or Password";
        //        return false;
        //    }

        //}
    }
}
