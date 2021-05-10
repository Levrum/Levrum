using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Levrum.Licensing;
using Levrum.Licensing.Client.WPF;

namespace Levrum.UI.WPF
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        const string c_aboutTextFormat =
@"Copyright © {0} Levrum Data Technologies
All rights reserved.

License Status:     {1}     
License Type:       {2}
License Expires:    {3}     
Support Expires:    {4}
Licensed To:        {5} ({6})
Machine ID:         {7}

Please contact info@levrum.com for more information on our products and services.

Special thanks to Georgetown Fire Department (Georgetown, TX), San Antonio Fire Department (San Antonio, TX), and Virginia Fire Department (Virginia, MN) for their assistance developing this software.

Warning: This computer program is protected by copyright law and international treaties. Unauthorized reproduction or distribution of this program, of any portion of it, may result in severe civil and criminal penalties, and will be prosecuted to the maximum extent possible under the law.";

        public AboutWindow(string title, Assembly assembly)
        {
            InitializeComponent();

            LicenseClient client = new LicenseClient();
            FileInfo licenseFile = client.LicenseFile;
            License license = License.DecodeSignedLicense(licenseFile);

            VersionText.Text = string.Format("Version {0}", assembly.GetName().Version);

            string year = DateTime.Today.Year.ToString();
            string status = "Unknown";
            string licenseExpires = "Unknown";
            string supportExpires = "Unknown";
            string customerName = "Unknown";
            string customerId = "Unknown";
            string licenseType = "Unknown";
            string machineId = Licensing.Id.MachineId.GetMachineId();

            if (license != null)
            {
                status = license.VerifyLicense(assembly).ToString();
                licenseExpires = license.ExpirationDate.ToString();
                supportExpires = license.SupportExpirationDate.ToString();
                customerName = license.CustomerName;
                customerId = license.CustomerId;
                string typeString = license.Type.ToString();
                if (license.Type == LicenseType.Enterprise)
                {
                    licenseType = typeString;
                }
                else if (license.Type == LicenseType.Machine)
                {
                    licenseType = string.Format("{0} ({1})", typeString, license.LicenseeMachineId);
                }
                else if (license.Type == LicenseType.User)
                {
                    licenseType = string.Format("{0} ({1})", typeString, license.LicenseeEmail);
                }
            }

            Title = $"About {title}";
            titleBlock.Text = title;
            AboutDetailsText.Text = string.Format(c_aboutTextFormat, year, status, licenseType, licenseExpires, supportExpires, customerName, customerId, machineId);
        }

        private ImageSource _imageSource;
        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                BurgerImage.Source = _imageSource;
            }
        }

        bool IsBurger = false;
        DateTime FirstClick = DateTime.MinValue;

        private void BurgerImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DateTime secondClick = FirstClick;
            FirstClick = DateTime.Now;
            if (FirstClick - secondClick > new TimeSpan(0, 0, 0, 0, 200))
            {
                return;
            }
            try
            {
                if (!IsBurger)
                {
                    BurgerImage.Source = new BitmapImage(new Uri(@"Resources/hamburger.png", UriKind.Relative));
                }
                else
                {
                    BurgerImage.Source = new BitmapImage(new Uri(@"Resources/databridge.png", UriKind.Relative));
                }

                IsBurger = !IsBurger;
            }
            catch (Exception ex)
            {

            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
