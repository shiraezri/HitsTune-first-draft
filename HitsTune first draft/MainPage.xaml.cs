using System;
using HitsTune_first_draft.Services;
using Microsoft.Maui.Controls;

namespace HitsTune_first_draft
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // טעינת שם השחקן שהתחבר מתוך AuthService
            string playerName = AuthService.CurrentPlayerName;
            if (!string.IsNullOrEmpty(playerName))
            {
                WelcomeLabel.Text = $"שלום, {playerName}! 👋";
            }
            else
            {
                WelcomeLabel.Text = "שלום, שחקן! 👋";
            }
        }

        private async void OnStartGameClicked(object sender, EventArgs e)
        {
            // מעבר למסך המשחק
            await Shell.Current.GoToAsync("//GamePage");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // מעבר חזרה למסך ההתחברות
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}