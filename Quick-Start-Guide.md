# FamilyOS Quick Start Guide

## Welcome to FamilyOS! 🏠

FamilyOS is your family's digital safety platform, designed to create a secure and educational environment for children while giving parents complete control and oversight.

## Table of Contents
- [Getting Started](#getting-started)
- [First Time Setup](#first-time-setup)
- [Daily Usage](#daily-usage)
- [Parent Features](#parent-features)
- [Child Experience](#child-experience)
- [Troubleshooting](#troubleshooting)
- [Safety & Security](#safety--security)

---

## Getting Started

### System Requirements
- **Operating System**: Windows 10/11, macOS 10.15+, or Linux
- **Memory**: 2GB RAM minimum
- **Storage**: 1GB free space
- **.NET Runtime**: Automatically installed with FamilyOS

### Installation

#### Method 1: Simple Installation (Recommended)
1. **Download** the FamilyOS installer from our website
2. **Run** the installer and follow the prompts
3. **Launch** FamilyOS from your desktop or Start menu

#### Method 2: Developer Installation
If you're running from source code:
1. Open a terminal/command prompt
2. Navigate to the FamilyOS folder
3. Type: `dotnet run`

---

## First Time Setup

### Step 1: Create Parent Account
When you first run FamilyOS, you'll be prompted to create a parent account:

```
=== Welcome to FamilyOS! ===
No users found. Let's create your first parent account.

Enter username: mom_sarah
Enter password: [your secure password]
Full name: Sarah Johnson
```

**💡 Tips for Parent Account:**
- Choose a username you'll remember
- Use a strong password (8+ characters, mix of letters/numbers)
- Your full name helps children identify you

### Step 2: Add Family Members
After creating your parent account:

1. **Login** with your new parent credentials
2. **Select** "User Management" from the main menu
3. **Choose** "Add Child User" or "Add Parent User"
4. **Enter** the required information:
   - Username (what they'll type to login)
   - Password (consider age-appropriate complexity)
   - Full name
   - Age (determines available features)

### Step 3: Configure Family Settings
Navigate to **Settings** to customize:
- Screen time limits per child
- Allowed applications
- Educational goals
- Safety restrictions

---

## Daily Usage

### Starting FamilyOS
1. **Double-click** the FamilyOS icon on your desktop
2. **Wait** for the welcome screen to appear
3. **Select** a user or type a username

### Login Process
```
=== Family Login ===
Select user or type username:
1. Mom (Sarah)
2. Dad (Mike)  
3. Emma (Age 8)
4. Jake (Age 12)

Choice: 3
Password for Emma: [child enters password]
```

### Main Menu Navigation
After login, users see age-appropriate menus:

**Parent Menu:**
- 📊 Family Dashboard
- 👥 User Management  
- 📱 App Launcher
- 📈 Activity Reports
- ⚙️ Settings
- � Change Password
- 🔧 Password Management (Parent Only)
- 🚪 Logout

**Child Menu:**
- 🎮 Educational Games
- 📚 Learning Apps
- 🎨 Creative Tools
- 📖 Digital Library
- 🔐 Change Password
- 🚪 Logout

---

## Parent Features

### Family Dashboard
Your central control panel showing:
- **Active Users**: Who's currently logged in
- **Screen Time**: Today's usage per child
- **Recent Activity**: What apps were used
- **Alerts**: Any concerning activity

### User Management
- **Add/Remove** family members
- **Change** passwords or permissions
- **Set** age-based restrictions
- **Review** activity logs

### App Launcher
- **Approve** new applications
- **Monitor** app usage
- **Set** time limits per application
- **Block** inappropriate content

### Activity Reports
Daily, weekly, and monthly reports including:
- Screen time summaries
- Educational progress
- App usage patterns
- Achievement milestones

### Password Management
**Parent-Only Security Features:**
- **Reset child passwords** instantly
- **Unlock locked accounts** immediately
- **View password change history** for all family members
- **Check account status** and failed login attempts
- **Account lockout protection** - accounts auto-lock after 3 failed attempts

---

## Child Experience

### Safe Login
- Children can only access their own account
- **No switching** to parent accounts (security feature)
- Simple password recovery with parent approval

### Educational Focus
- **Curated apps** appropriate for their age
- **Learning games** that track progress
- **Creative tools** for digital art and storytelling
- **Digital books** with comprehension tracking

### Parental Oversight
Children see gentle reminders about:
- Time remaining for current session
- Educational goals for the day
- Family rules and expectations

---

## Troubleshooting

### Common Issues

#### "Cannot connect to FamilyOS"
**Solution:**
1. Check internet connection
2. Restart FamilyOS
3. Contact support if issue persists

#### "Wrong password" repeatedly
**Solution:**
1. Try typing password carefully (check caps lock)
2. After 3 failed attempts, account will be locked for 15 minutes
3. **If account is locked**: Ask a parent to unlock it using Password Management
4. **If you forgot your password**: Ask a parent to reset it
5. Use "Change Password" option from main menu when logged in

#### "App won't start"
**Solution:**
1. Check if app is approved by parents
2. Verify age restrictions
3. Ensure sufficient screen time remaining
4. Contact parent for app approval

#### Child can't logout properly
**Solution:**
1. Press Ctrl+C to force logout
2. Parent can reset sessions in Family Dashboard
3. Restart FamilyOS if needed

#### Account locked message
**Solution:**
1. **Wait 15 minutes** for automatic unlock, OR
2. **Ask a parent** to unlock immediately through Password Management
3. Parent can also reset your password if needed

### Getting Help
1. **Check this guide** first
2. **Ask a parent** for assistance
3. **Use the Help menu** in FamilyOS
4. **Contact support**: support@familyos.com

---

## Safety & Security

### Built-in Protections
- ✅ **Age verification** for all content
- ✅ **Parent approval** required for new apps
- ✅ **Activity monitoring** and reporting
- ✅ **Screen time controls** and limits
- ✅ **Secure authentication** system
- ✅ **Account lockout** after failed login attempts
- ✅ **Password change tracking** and history
- ✅ **Parent-controlled password reset** capabilities

### Child Account Security
- 🔒 **Cannot switch** to parent accounts
- 🔒 **Limited system access** based on age
- 🔒 **All activity tracked** and reported to parents
- 🔒 **Automatic logout** after inactivity
- 🔒 **Account lockout** after 3 failed password attempts
- 🔒 **Password change notifications** sent to parents

### Parent Controls
- 👨‍👩‍👧‍👦 **Full oversight** of all child activity
- 👨‍👩‍👧‍👦 **Instant notifications** for concerning behavior
- 👨‍👩‍👧‍👦 **Emergency shutdown** capabilities
- 👨‍👩‍👧‍👦 **Detailed reporting** and analytics
- 👨‍👩‍👧‍👦 **Complete password management** control
- 👨‍👩‍👧‍👦 **Account unlock** capabilities

### Privacy Commitment
- **No data sharing** with third parties
- **Local storage** of family information
- **Encrypted passwords** and sensitive data
- **Parent control** over all data retention

---

## Quick Reference

### Essential Commands
- **Login**: Select user and enter password
- **Logout**: Choose "Logout" from any menu
- **Emergency Exit**: Press Ctrl+C
- **Help**: Look for 📋 Help in any menu

### Parent Shortcuts
- **View all users**: Main Menu → User Management
- **Check activity**: Main Menu → Activity Reports
- **Change settings**: Main Menu → Settings
- **Add new child**: User Management → Add Child User

### Important Notes
- 🚨 **Children cannot access parent features**
- ⏰ **Screen time limits are enforced automatically**
- 📧 **Parents receive weekly usage reports**
- 🔧 **All settings require parent authentication**

---

## Need More Help?

### Resources
- 📖 **Full Documentation**: Available in the Help menu
- 💬 **Community Forum**: Connect with other FamilyOS families
- 📧 **Email Support**: support@familyos.com
- 📞 **Phone Support**: 1-800-FAMILY-OS

### Video Tutorials
- Setting up your first family account
- Configuring child restrictions
- Understanding activity reports
- Troubleshooting common issues

---

**Welcome to the FamilyOS family! We're here to help make your digital parenting journey safer, easier, and more educational for everyone.** 🎉

*Last updated: December 2025*