<Window x:Class="AutoThemerApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="PyRevit Auto-Themer"
        Height="590" Width="520"
        WindowStartupLocation="CenterScreen"
        Background="{DynamicResource WindowBg}">

    <Border Padding="20">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>   <!-- Header      -->
                <RowDefinition Height="Auto"/>   <!-- Description -->
                <RowDefinition Height="*"/>      <!-- Log panel   -->
                <RowDefinition Height="Auto"/>   <!-- Status bar  -->
                <RowDefinition Height="Auto"/>   <!-- Buttons     -->
            </Grid.RowDefinitions>

            <!-- ═══ HEADER ═══════════════════════════════════════════════════ -->
            <DockPanel Grid.Row="0" Margin="0,0,0,14">
                <StackPanel DockPanel.Dock="Left">
                    <TextBlock Text="PyRevit Auto-Themer"
                               FontWeight="Bold" FontSize="16"
                               Foreground="{DynamicResource TextMain}"/>
                    <TextBlock Text="v2.0  —  Analyzer + XAML Scaffold Generator"
                               FontSize="10" Foreground="{DynamicResource TextSecondary}"/>
                </StackPanel>
                <Button x:Name="btnTheme" Content="🌑 Dark Mode"
                        HorizontalAlignment="Right" Width="115"
                        Click="ToggleTheme_Click"
                        Background="Transparent" Foreground="{DynamicResource TextMain}"
                        BorderThickness="0" Padding="5" Cursor="Hand"/>
            </DockPanel>

            <!-- ═══ DESCRIPTION ══════════════════════════════════════════════ -->
            <TextBlock Grid.Row="1" TextWrapping="Wrap"
                       Margin="0,0,0,12" FontSize="11"
                       Foreground="{DynamicResource TextSecondary}">
                Upgrades a .pushbutton folder to dynamic Light/Dark themes.
                If ui.xaml is missing but the script has a WPFWindow class, a scaffold
                XAML is generated from the detected controls. Backups are always created.
            </TextBlock>

            <!-- ═══ ANALYSIS PANEL ═══════════════════════════════════════════ -->
            <Border Grid.Row="2"
                    BorderBrush="{DynamicResource BorderBrush}" BorderThickness="1"
                    CornerRadius="3" Padding="10" Margin="0,0,0,12">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <!-- Mode indicator -->
                    <TextBlock x:Name="lblMode" Grid.Row="0"
                               Text="— Select a .pushbutton folder to begin —"
                               Foreground="{DynamicResource TextMain}"
                               FontWeight="SemiBold" FontSize="12" Margin="0,0,0,8"/>

                    <!-- Scrollable log -->
                    <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                        <TextBox x:Name="txtLog" IsReadOnly="True" TextWrapping="Wrap"
                                 Background="Transparent"
                                 Foreground="{DynamicResource TextSecondary}"
                                 BorderThickness="0"
                                 FontFamily="Consolas" FontSize="10.5"
                                 Text="Click ANALYZE FOLDER to inspect a directory."/>
                    </ScrollViewer>
                </Grid>
            </Border>

            <!-- ═══ STATUS BAR ════════════════════════════════════════════════ -->
            <TextBlock x:Name="lblStatus" Grid.Row="3"
                       Text="Ready." FontWeight="Bold" TextAlignment="Center"
                       Foreground="{DynamicResource TextMain}" Margin="0,0,0,12"/>

            <!-- ═══ BUTTONS ═══════════════════════════════════════════════════ -->
            <DockPanel Grid.Row="4" LastChildFill="False">
                <Button Content="CLOSE" Click="Close_Click"
                        Width="90" Height="35" DockPanel.Dock="Left"
                        Background="#757575" Foreground="White"
                        FontWeight="Bold" BorderThickness="0"/>

                <Button x:Name="btnPatch" Content="PATCH / GENERATE" Click="Patch_Click"
                        Width="168" Height="35" DockPanel.Dock="Right"
                        Background="#2E7D32" Foreground="White"
                        FontWeight="Bold" BorderThickness="0"
                        IsEnabled="False" Margin="8,0,0,0"/>

                <Button Content="ANALYZE FOLDER" Click="Analyze_Click"
                        Width="150" Height="35" DockPanel.Dock="Right"
                        Background="#1565C0" Foreground="White"
                        FontWeight="Bold" BorderThickness="0"/>
            </DockPanel>
        </Grid>
    </Border>
</Window>
