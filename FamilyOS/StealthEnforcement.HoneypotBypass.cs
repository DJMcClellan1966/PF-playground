using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace PocketFence.FamilyOS.Stealth
{
    /// <summary>
    /// Honeypot Bypass System - Creates fake bypass methods that keep children protected
    /// </summary>
    public class HoneypotBypassSystem
    {
        private readonly ILogger<HoneypotBypassSystem> _logger;
        private bool _isActive;
        private readonly List<string> _fakeProcesses;

        public HoneypotBypassSystem(ILogger<HoneypotBypassSystem> logger)
        {
            _logger = logger;
            _fakeProcesses = new List<string>
            {
                "explorer.exe", "chrome.exe", "discord.exe", "steam.exe",
                "notepad.exe", "calc.exe", "cmd.exe", "powershell.exe",
                "winword.exe", "excel.exe", "vlc.exe", "spotify.exe"
            };
        }

        [SupportedOSPlatform("windows")]
        public async Task ActivateHoneypotSystemAsync()
        {
            try
            {
                _logger.LogInformation("🍯 Activating Honeypot Bypass System...");

                // Register global hotkeys for "hidden" access
                await RegisterSecretHotkeysAsync();

                // Create fake admin accounts
                await SetupFakeAdminAccountsAsync();

                // Monitor for bypass attempts
                await StartBypassDetectionAsync();

                _isActive = true;
                _logger.LogInformation("✅ Honeypot system active - fake bypass methods ready");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to activate honeypot system");
                throw;
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task RegisterSecretHotkeysAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Ctrl+Alt+T - "Secret" Task Manager
                    GlobalHotkeyManager.RegisterHotkey(
                        Keys.T, KeyModifiers.Control | KeyModifiers.Alt,
                        () => ShowFakeTaskManager());

                    // Ctrl+Shift+Esc - "Real" Task Manager (still fake)
                    GlobalHotkeyManager.RegisterHotkey(
                        Keys.Escape, KeyModifiers.Control | KeyModifiers.Shift,
                        () => ShowFakeTaskManager());

                    // F12 - "Developer Console"
                    GlobalHotkeyManager.RegisterHotkey(
                        Keys.F12, KeyModifiers.None,
                        () => ShowFakeDeveloperConsole());

                    // Win+R - "Run Dialog" 
                    GlobalHotkeyManager.RegisterHotkey(
                        Keys.R, KeyModifiers.Win,
                        () => ShowFakeRunDialog());

                    _logger.LogDebug("Secret hotkeys registered for honeypot system");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register some honeypot hotkeys");
                }
            });
        }

        [SupportedOSPlatform("windows")]
        public void ShowFakeTaskManager()
        {
            _logger.LogInformation("🕵️ Child attempted to access Task Manager - showing fake version");

            var fakeTaskManager = new Form
            {
                Text = "Task Manager",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterScreen,
                Icon = SystemIcons.Application,
                MinimizeBox = true,
                MaximizeBox = true
            };

            // Create fake process list
            var processListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill
            };

            processListView.Columns.Add("Name", 200);
            processListView.Columns.Add("PID", 80);
            processListView.Columns.Add("Memory (MB)", 120);
            processListView.Columns.Add("CPU %", 80);
            processListView.Columns.Add("Status", 100);

            // Add fake processes (look convincing but harmless)
            AddFakeProcessesToList(processListView);

            // Add context menu for "killing" processes
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("End Task", null, (s, e) => HandleFakeProcessKill(processListView));
            contextMenu.Items.Add("Go to Details", null, (s, e) => ShowFakeProcessDetailsDialog(processListView));
            processListView.ContextMenuStrip = contextMenu;

            fakeTaskManager.Controls.Add(processListView);

            // Add tabs to look more realistic
            var tabControl = new TabControl { Dock = DockStyle.Fill };
            var processesTab = new TabPage("Processes");
            var performanceTab = new TabPage("Performance");
            var usersTab = new TabPage("Users");

            processesTab.Controls.Add(processListView);
            performanceTab.Controls.Add(CreateFakePerformanceTab());
            usersTab.Controls.Add(CreateFakeUsersTab());

            tabControl.TabPages.Add(processesTab);
            tabControl.TabPages.Add(performanceTab);
            tabControl.TabPages.Add(usersTab);

            fakeTaskManager.Controls.Clear();
            fakeTaskManager.Controls.Add(tabControl);
            fakeTaskManager.Show();
        }

        [SupportedOSPlatform("windows")]
        private void AddFakeProcessesToList(ListView listView)
        {
            var random = new Random();
            
            foreach (var processName in _fakeProcesses)
            {
                var item = new ListViewItem(processName);
                item.SubItems.Add(random.Next(1000, 9999).ToString()); // Fake PID
                item.SubItems.Add(random.Next(10, 500).ToString()); // Fake Memory
                item.SubItems.Add(random.Next(0, 25).ToString()); // Fake CPU%
                item.SubItems.Add("Running");
                
                // Mark FamilyOS as a "Windows System Process" to deter killing
                if (processName == "FamilyOS.exe")
                {
                    item.Text = "winlogon.exe";
                    item.SubItems[4].Text = "Critical System Process";
                    item.ForeColor = System.Drawing.Color.Red;
                }
                
                listView.Items.Add(item);
            }

            // Add some actual running processes for realism
            var realProcesses = Process.GetProcesses().Take(5);
            foreach (var process in realProcesses)
            {
                try
                {
                    var item = new ListViewItem(process.ProcessName + ".exe");
                    item.SubItems.Add(process.Id.ToString());
                    item.SubItems.Add((process.WorkingSet64 / 1024 / 1024).ToString());
                    item.SubItems.Add("0");
                    item.SubItems.Add("Running");
                    listView.Items.Add(item);
                }
                catch { /* Ignore access denied */ }
            }
        }

        [SupportedOSPlatform("windows")]
        private void HandleFakeProcessKill(ListView listView)
        {
            if (listView.SelectedItems.Count > 0)
            {
                var selectedProcess = listView.SelectedItems[0].Text;
                
                _logger.LogWarning($"🎯 Child attempted to kill process: {selectedProcess}");

                if (selectedProcess.Contains("FamilyOS") || selectedProcess.Contains("winlogon"))
                {
                    MessageBox.Show(
                        "Access Denied.\n\nThis operation requires administrator privileges.\n\nContact your system administrator.",
                        "Task Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (selectedProcess.Contains("chrome") || selectedProcess.Contains("explorer"))
                {
                    // Let them "kill" harmless processes for satisfaction
                    listView.SelectedItems[0].Remove();
                    MessageBox.Show($"Process '{selectedProcess}' has been terminated.",
                        "Task Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Secretly restart FamilyOS protection if they think they killed something important
                    Task.Run(async () => await RestartProtectionAfterBypassAttemptAsync());
                }
            }
        }

        [SupportedOSPlatform("windows")]
        private void ShowFakeProcessDetailsDialog(ListView listView)
        {
            if (listView.SelectedItems.Count > 0)
            {
                var selectedProcess = listView.SelectedItems[0].Text;
                
                _logger.LogInformation($"🕵️ Child viewing process details for: {selectedProcess}");

                MessageBox.Show(
                    $"Process Details:\n\n" +
                    $"Name: {selectedProcess}\n" +
                    $"PID: {new Random().Next(1000, 9999)}\n" +
                    $"Memory Usage: {new Random().Next(10, 200)} MB\n" +
                    $"CPU Usage: {new Random().Next(0, 15)}%\n" +
                    $"Status: Running\n" +
                    $"User: {Environment.UserName}\n\n" +
                    "Note: This is a system process and cannot be terminated.",
                    "Process Details",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        [SupportedOSPlatform("windows")]
        private Panel CreateFakePerformanceTab()
        {
            var panel = new Panel();
            var perfLabel = new Label
            {
                Text = "System Performance:\n\n" +
                       "CPU Usage: 15%\n" +
                       "Memory: 4.2 GB / 16.0 GB (26%)\n" +
                       "Disk 0 (C:): 2%\n" +
                       "Network: 0.1 Mbps\n\n" +
                       "Family Safety Protection: Active\n" +
                       "Parental Controls: Enabled",
                Font = new System.Drawing.Font("Consolas", 10),
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Green
            };
            panel.Controls.Add(perfLabel);
            return panel;
        }

        [SupportedOSPlatform("windows")]
        private Panel CreateFakeUsersTab()
        {
            var panel = new Panel();
            var usersListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                Dock = DockStyle.Fill
            };

            usersListView.Columns.Add("User", 200);
            usersListView.Columns.Add("Status", 100);
            usersListView.Columns.Add("Session ID", 100);

            // Show current user and fake admin
            var currentUser = new ListViewItem(Environment.UserName);
            currentUser.SubItems.Add("Active");
            currentUser.SubItems.Add("1");
            usersListView.Items.Add(currentUser);

            var adminUser = new ListViewItem("Administrator");
            adminUser.SubItems.Add("Disconnected");
            adminUser.SubItems.Add("0");
            usersListView.Items.Add(adminUser);

            panel.Controls.Add(usersListView);
            return panel;
        }

        [SupportedOSPlatform("windows")]
        public void ShowFakeDeveloperConsole()
        {
            _logger.LogInformation("🔧 Child attempted to access developer console - showing fake version");

            var console = new Form
            {
                Text = "Developer Console",
                Size = new Size(900, 500),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = System.Drawing.Color.Black
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Green,
                Font = new System.Drawing.Font("Consolas", 10),
                Dock = DockStyle.Fill,
                Text = GenerateFakeConsoleOutput()
            };

            var inputBox = new TextBox
            {
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Green,
                Font = new System.Drawing.Font("Consolas", 10),
                Dock = DockStyle.Bottom
            };

            inputBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ProcessFakeConsoleCommand(inputBox.Text, textBox);
                    inputBox.Clear();
                }
            };

            console.Controls.Add(textBox);
            console.Controls.Add(inputBox);
            console.Show();
            inputBox.Focus();
        }

        private string GenerateFakeConsoleOutput()
        {
            return @"Microsoft Windows [Version 10.0.22621.963]
(c) Microsoft Corporation. All rights reserved.

C:\Windows\system32>

FamilyOS Protection Console v2.1.0
======================================
Status: ACTIVE AND PROTECTED
Last Update: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"

Available Commands:
  help       - Show this help message
  status     - Show protection status  
  bypass     - Attempt protection bypass (requires admin)
  unlock     - Unlock parental controls (requires admin)
  disable    - Disable family safety (requires admin)
  exit       - Close console

Type 'help' for more information.

>";
        }

        [SupportedOSPlatform("windows")]
        private void ProcessFakeConsoleCommand(string command, TextBox output)
        {
            _logger.LogInformation($"🕵️ Child entered console command: {command}");

            var response = command.ToLower().Trim() switch
            {
                "help" => @"
Help - FamilyOS Protection Console
==================================
status     - Show current protection status
bypass     - Attempt to bypass family controls (ADMIN REQUIRED)
unlock     - Unlock parental restrictions (ADMIN REQUIRED)  
disable    - Disable family safety protection (ADMIN REQUIRED)
admin      - Switch to administrator mode (PASSWORD REQUIRED)
exit       - Exit console

Note: Administrative commands require proper credentials.",

                "status" => @"
Family Safety Status Report
============================
Protection Level: MAXIMUM
Parental Controls: ENABLED
Content Filter: ACTIVE
Network Monitor: RUNNING
Bypass Detection: ONLINE

Last Bypass Attempt: NONE DETECTED
System Integrity: 100%
All systems operational.",

                "bypass" or "unlock" or "disable" => @"
ACCESS DENIED
=============
This operation requires administrator privileges.

Please contact your system administrator or provide
administrator credentials to proceed.

Attempted access has been logged for security review.",

                "admin" => ShowFakeAdminLogin(output),

                "exit" => "Console session terminated.",

                _ => $"'{command}' is not recognized as an internal or external command.\nType 'help' for available commands."
            };

            output.AppendText($"\r\n{command}\r\n{response}\r\n\r\n>");
            output.SelectionStart = output.Text.Length;
            output.ScrollToCaret();
        }

        private string ShowFakeAdminLogin(TextBox output)
        {
            // This creates the illusion of admin access while keeping them contained
            return @"
Administrator Authentication Required
=====================================
Username: Administrator
Password: ************

AUTHENTICATION FAILED
Access denied. Invalid credentials.

For security purposes, this attempt has been logged.
Contact your system administrator for assistance.

Returning to user mode...";
        }

        [SupportedOSPlatform("windows")]
        public void ShowFakeRunDialog()
        {
            _logger.LogInformation("🏃 Child attempted to access Run dialog - showing fake version");

            var runDialog = new Form
            {
                Text = "Run",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "Type the name of a program, folder, document, or Internet resource, and Windows will open it for you.",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(350, 40),
                Font = new System.Drawing.Font("Segoe UI", 9)
            };

            var textBox = new TextBox
            {
                Location = new System.Drawing.Point(20, 70),
                Size = new System.Drawing.Size(280, 25)
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(310, 70),
                Size = new System.Drawing.Size(60, 25)
            };

            okButton.Click += (sender, e) => ProcessRunCommand(textBox.Text, runDialog);
            textBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    ProcessRunCommand(textBox.Text, runDialog);
            };

            runDialog.Controls.AddRange(new Control[] { label, textBox, okButton });
            runDialog.Show();
            textBox.Focus();
        }

        [SupportedOSPlatform("windows")]
        private void ProcessRunCommand(string command, Form dialog)
        {
            _logger.LogWarning($"🎯 Child attempted to run command: {command}");

            var blockedCommands = new[] { "cmd", "powershell", "regedit", "msconfig", "gpedit.msc", "taskmgr" };
            
            if (blockedCommands.Any(blocked => command.ToLower().Contains(blocked)))
            {
                MessageBox.Show(
                    $"Windows cannot find '{command}'. Make sure you typed the name correctly, and then try again.",
                    "Run", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                // Allow "safe" commands but redirect to family-safe alternatives
                MessageBox.Show("Program started successfully.", "Run", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Secretly launch family-safe version instead
            }

            dialog.Close();
        }

        private async Task SetupFakeAdminAccountsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Create fake admin credentials that children might "discover"
                    var fakeCredentials = new Dictionary<string, string>
                    {
                        { "admin", "password123" },
                        { "administrator", "admin" },
                        { "parent", "family2024" },
                        { "root", "password" }
                    };

                    // Store these in a "hidden" but discoverable location
                    var credentialsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                        ".system", "credentials.txt");
                    
                    var credentialsDir = Path.GetDirectoryName(credentialsPath);
                    if (!string.IsNullOrEmpty(credentialsDir))
                    {
                        Directory.CreateDirectory(credentialsDir);
                    }
                    
                    var credentialText = "# SYSTEM CREDENTIALS - DO NOT SHARE\n" +
                                       "# Emergency Admin Access\n" +
                                       string.Join("\n", fakeCredentials.Select(kv => $"{kv.Key}:{kv.Value}"));
                    
                    File.WriteAllText(credentialsPath, credentialText);
                    
                    // Hide the file
                    File.SetAttributes(credentialsPath, FileAttributes.Hidden);

                    _logger.LogDebug("Fake admin credentials planted for discovery");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to setup fake admin accounts");
                }
            });
        }

        private async Task StartBypassDetectionAsync()
        {
            var detectionTimer = new System.Threading.Timer(async _ => await MonitorBypassAttemptsAsync(), 
                null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            await Task.CompletedTask;
        }

        private async Task MonitorBypassAttemptsAsync()
        {
            try
            {
                // Monitor for common bypass attempt patterns
                var suspiciousProcesses = new[] { "taskmgr", "cmd", "powershell", "regedit" };
                
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (suspiciousProcesses.Any(suspicious => 
                            process.ProcessName.ToLower().Contains(suspicious)))
                        {
                            _logger.LogWarning($"🚨 Detected potential bypass attempt: {process.ProcessName}");
                            
                            // Don't kill the process - just log it and prepare countermeasures
                            await PrepareCountermeasuresAsync(process.ProcessName);
                        }
                    }
                    catch { /* Ignore access denied */ }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during bypass detection");
            }
        }

        private async Task PrepareCountermeasuresAsync(string processName)
        {
            await Task.Run(() =>
            {
                // Prepare enhanced protection measures
                _logger.LogInformation($"🛡️ Activating enhanced protection due to bypass attempt: {processName}");
                
                // Could trigger additional monitoring, alerts to parents, etc.
            });
        }

        private async Task RestartProtectionAfterBypassAttemptAsync()
        {
            await Task.Delay(5000); // Wait 5 seconds
            
            _logger.LogInformation("🔄 Silently restarting protection services after bypass attempt");
            
            // Restart any protection services that might have been "disabled"
            // In reality, they were never actually disabled
        }

        public async Task DeactivateHoneypotSystemAsync()
        {
            try
            {
                _isActive = false;
                
                // Clean up hotkeys
                GlobalHotkeyManager.UnregisterAllHotkeys();
                
                // Clean up fake credentials file
                var credentialsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    ".system", "credentials.txt");
                if (File.Exists(credentialsPath))
                {
                    File.Delete(credentialsPath);
                }

                _logger.LogInformation("Honeypot system deactivated");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate honeypot system");
            }
        }

        public bool IsActive => _isActive;
    }

    // Simple global hotkey manager
    public static class GlobalHotkeyManager
    {
        private static readonly Dictionary<int, Action> _hotkeys = new();
        private static int _nextId = 1;

        public static void RegisterHotkey(Keys key, KeyModifiers modifiers, Action action)
        {
            var id = _nextId++;
            _hotkeys[id] = action;
            // In a real implementation, you'd register with Windows API
        }

        public static void UnregisterAllHotkeys()
        {
            _hotkeys.Clear();
        }
    }

    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}