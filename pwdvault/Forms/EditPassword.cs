﻿using pwdvault.Modeles;
using pwdvault.Services;
using Serilog;

namespace pwdvault.Forms
{
    public partial class EditPassword : Form
    {
        private readonly UserPassword userPassword;
        public EditPassword(string AppName, string Username)
        {
            try
            {
                InitializeComponent();
                comBoxCat.DataSource = Enum.GetValues(typeof(Categories));
                lbTitle.Text = $"Edit {AppName} password";
                using var context = new PasswordVaultContext();
                userPassword = new UserPasswordService(context).GetUserPassword(AppName, Username);
                txtBoxApp.Text = userPassword.AppName;
                txtBoxApp.ReadOnly = true;
                comBoxCat.Text = userPassword.AppCategory;
                txtBoxUser.Text = userPassword.UserName;
                txtBoxUser.ReadOnly = true;
                txtBoxPwd.Text = EncryptionService.DecryptPassword(userPassword.Password, EncryptionService.GetKeyFromFile());
            }
            catch (Exception ex)
            {
                Log.Logger.Error("\nSource : " + ex.Source + "\nMessage : " + ex.Message);
                throw new Exception(ex.Message);
            }

        }

        private void BtnEye_MouseUp(object sender, MouseEventArgs e)
        {
            txtBoxPwd.PasswordChar = '*';
            txtBoxPwd.UseSystemPasswordChar = true;
        }

        private void BtnEye_MouseDown(object sender, MouseEventArgs e)
        {
            txtBoxPwd.PasswordChar = '\0';
            txtBoxPwd.UseSystemPasswordChar = false;
        }

        /// <summary>
        /// Encrypts the updated password and updates it on the database.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(txtBoxApp.Text) &&
                !String.IsNullOrWhiteSpace(txtBoxUser.Text) &&
                !String.IsNullOrWhiteSpace(txtBoxPwd.Text) &&
                !String.IsNullOrWhiteSpace(comBoxCat.Text) &&
                !errorProvider.HasErrors
                )
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    var encryptedPassword = EncryptionService.EncryptPassword(txtBoxPwd.Text, EncryptionService.GetKeyFromFile());
                    var userPasswordEdited = new UserPassword(comBoxCat.Text, userPassword.AppName, userPassword.UserName, encryptedPassword, userPassword.IconName)
                    {
                        Id = userPassword.Id,
                        CreationTime = userPassword.CreationTime,
                        UpdateTime = DateTime.Now
                    };
                    using var context = new PasswordVaultContext();
                    var userPasswordService = new UserPasswordService(context);
                    userPasswordService.UpdateUserPassword(userPasswordEdited);
                    
                    Cursor = Cursors.Default;
                    MessageBox.Show($"{userPasswordEdited.AppName}'s password successfully updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An unexpected error occured. Please try again later or contact the administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Cursor = Cursors.Default;
                    Log.Logger.Error("\nSource : " + ex.Source + "\nMessage : " + ex.Message);
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Please complete all fields.", "Incomplete form", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            txtBoxPwd.Text = PasswordService.GeneratePassword();
        }

        /// <summary>
        /// If the password is not strong enough, an error is shown to the user with the password's criteria.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBoxPwd_TextChanged(object sender, EventArgs e)
        {
            if (!PasswordService.IsPasswordStrong(txtBoxPwd.Text))
            {
                errorProvider.SetError(txtBoxPwd, "Password must be atleast 12 characters long and contain the following : " + Environment.NewLine +
                        "- Uppercase" + Environment.NewLine + "- Lowercase" + Environment.NewLine + "- Numbers" + Environment.NewLine + "- Symbols");
            }
            else
            {
                errorProvider.SetError(txtBoxPwd, String.Empty);
                errorProvider.Clear();
            }
        }
    }
}
