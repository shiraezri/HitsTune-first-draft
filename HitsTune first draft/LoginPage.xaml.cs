using System;
using System.Text.RegularExpressions;
using HitsTune_first_draft.Services;
using Microsoft.Maui.Controls;

namespace HitsTune_first_draft
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;
        private bool _isRegisterMode = false;

        public LoginPage()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text?.Trim();
            string playerName = PlayerNameEntry.Text?.Trim();

            // 1. בדיקת שדות ריקים בסיסיים
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowStatus("נא למלא את כל השדות", true);
                return;
            }

            // 2. בדיקת תקינות אימייל
            if (!IsValidEmail(email))
            {
                ShowStatus("נא להזין כתובת אימייל תקינה", true);
                return;
            }

            // 3. הגבלות מיוחדות רק בזמן הרשמה
            if (_isRegisterMode)
            {
                // בדיקת שם שחקן
                if (string.IsNullOrEmpty(playerName) || playerName.Length < 2)
                {
                    ShowStatus("שם שחקן חייב להכיל לפחות 2 תווים", true);
                    return;
                }

                if (playerName.Length > 15)
                {
                    ShowStatus("שם שחקן לא יכול להיות ארוך מ-15 תווים", true);
                    return;
                }

                // בדיקת סיסמה - אורך מינימלי למענה על דרישות Firebase
                if (password.Length < 6)
                {
                    ShowStatus("הסיסמה חייבת להכיל לפחות 6 תווים", true);
                    return;
                }

                // בדיקה שהסיסמה מכילה לפחות ספרה אחת
                if (!Regex.IsMatch(password, @"\d"))
                {
                    ShowStatus("הסיסמה חייבת להכיל לפחות ספרה אחת", true);
                    return;
                }
            }

            SetLoading(true);

            bool success;
            if (_isRegisterMode)
            {
                success = await _authService.RegisterAsync(email, password, playerName);
            }
            else
            {
                success = await _authService.LoginAsync(email, password, playerName);
            }

            SetLoading(false);

            if (success)
            {
                ShowStatus("התחברת בהצלחה!", false);
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                ShowStatus(_isRegisterMode ? "הרשמה נכשלה. ייתכן שהאימייל כבר קיים במערכת." : "התחברות נכשלה. אימייל או סיסמה שגויים.", true);
            }
        }

        private void OnToggleModeClicked(object sender, EventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;

            if (_isRegisterMode)
            {
                TitleLabel.Text = "הרשמה לחשבון חדש";
                PlayerNameBorder.IsVisible = true;
                SubmitButton.Text = "הירשם";
                ToggleModeText.Text = "כבר יש לך חשבון?";
                ToggleButton.Text = "התחבר";
            }
            else
            {
                TitleLabel.Text = "התחברות לחשבון";
                PlayerNameBorder.IsVisible = false;
                SubmitButton.Text = "התחבר";
                ToggleModeText.Text = "אין לך חשבון?";
                ToggleButton.Text = "הירשם עכשיו";
            }

            StatusLabel.IsVisible = false;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = isError ? Microsoft.Maui.Graphics.Colors.Red : Microsoft.Maui.Graphics.Colors.Green;
            StatusLabel.IsVisible = true;
        }

        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.IsVisible = isLoading;
            LoadingIndicator.IsRunning = isLoading;
            SubmitButton.IsEnabled = !isLoading;
        }
    }
}