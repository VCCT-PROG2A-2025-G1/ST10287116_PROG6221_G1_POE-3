using System;
using System.Media;
using System.Windows;

namespace CyberSecurityBot
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            PlayVoiceGreeting();
        }

        private void PlayVoiceGreeting()
        {
            try
            {
                SoundPlayer player = new SoundPlayer("Program.wav");
                player.PlaySync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Voice greeting failed to play: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}