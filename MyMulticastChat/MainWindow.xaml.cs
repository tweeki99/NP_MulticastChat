using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyMulticastChat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Chat chat;

        public MainWindow()
        {
            InitializeComponent();
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^ЁёА-яa-zA-Z0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SigninButtonClick(object sender, RoutedEventArgs e)
        {
            if (userNameTextBox.Text.Trim() == string.Empty)
                return;

            var userName = userNameTextBox.Text.Trim();
            userName = userName.Replace(" ", "_");
            userNameTextBox.Text = userName;
            ElementsMode(true);

            chat = new Chat(this, userName);
            Thread listenThread = new Thread(new ThreadStart(chat.Listen));
            listenThread.IsBackground = true;
            listenThread.Start();
            Thread.Sleep(5);
            chat.ThisUserConnection();
        }

        private void ElementsMode(bool isConnected)
        {
            if (isConnected)
                label1.Content = "Ваше имя";
            else
            {
                label1.Content = "Введите имя";
                userNameTextBox.Text = string.Empty;
                onlineUsersListBox.Items.Clear();
            }

            allMessageTextBox.Text = string.Empty;
            messageTextBox.Text = string.Empty;

            userNameTextBox.IsReadOnly = isConnected;
            signinButton.IsEnabled = !isConnected;
            signoutButton.IsEnabled = isConnected;
            allMessageTextBox.IsEnabled = isConnected;
            messageTextBox.IsEnabled = isConnected;
            sendButton.IsEnabled = isConnected;

        }

        private void SignoutButtonClick(object sender, RoutedEventArgs e)
        {
            ElementsMode(false);
            chat.ThisUserDisconnection();
        }

        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            if (messageTextBox.Text.Trim() != string.Empty)
            {
                chat.SendMessageToTheAllUsers(messageTextBox.Text);
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(chat != null)
            chat.ThisUserDisconnection();
        }
    }
}
