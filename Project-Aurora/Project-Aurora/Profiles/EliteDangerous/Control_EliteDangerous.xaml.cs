﻿using System;
using System.Windows;
using System.Windows.Controls;
using AuroraRgb.Settings;

namespace AuroraRgb.Profiles.EliteDangerous
{
    /// <summary>
    /// Interaction logic for Control_EliteDangerous.xaml
    /// </summary>
    public partial class Control_EliteDangerous : UserControl
    {
        private Application profile_manager;

        public Control_EliteDangerous(Application profile)
        {
            InitializeComponent();

            profile_manager = profile;

            SetSettings();

            if (!(profile_manager.Settings as FirstTimeApplicationSettings).IsFirstTimeInstalled)
            {
                (profile_manager.Settings as FirstTimeApplicationSettings).IsFirstTimeInstalled = true;
            }

            profile_manager.ProfileChanged += Control_EliteDangerous_ProfileChanged;

        }

        private void Control_EliteDangerous_ProfileChanged(object? sender, EventArgs e)
        {
            SetSettings();
        }

        private void SetSettings()
        {
            this.game_enabled.IsChecked = profile_manager.Settings.IsEnabled;

        }

        //Overview
        
        private void game_enabled_Checked(object? sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                profile_manager.Settings.IsEnabled = (this.game_enabled.IsChecked.HasValue) ? this.game_enabled.IsChecked.Value : false;
                profile_manager.SaveProfiles();
            }
        }
    }
}