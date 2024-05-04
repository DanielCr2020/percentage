using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;


namespace percentage
{
    class TrayIcon
    {
        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        private const int fontSize = 18;
        private const string font = "Segoe UI";

        private NotifyIcon notifyIcon;

        public TrayIcon()
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();

            notifyIcon = new NotifyIcon();

            contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem });

            menuItem.Click += new System.EventHandler(MenuItemClick);
            menuItem.Index = 0;
            menuItem.Text = "E&xit";

            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.Visible = true;

            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += new EventHandler(TimerTick);
            timer.Start();
        }

        private Bitmap GetTextBitmap(String text, Font font, Color fontColor)
        {
            SizeF imageSize = GetStringImageSize(text, font);
            Bitmap bitmap = new Bitmap((int)imageSize.Width, (int)imageSize.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(255, 0, 0, 0));
                using (Brush brush = new SolidBrush(fontColor))
                {
                    graphics.DrawString(text, font, brush, 0, 0);
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    graphics.Save();
                }
            }
            return bitmap;
        }

        private static SizeF GetStringImageSize(string text, Font font)
        {
            using (Image image = new Bitmap(2, 1))
            using (Graphics graphics = Graphics.FromImage(image))
                return graphics.MeasureString(text, font);
        }

        private void MenuItemClick(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            double percentage = Math.Round(powerStatus.BatteryLifePercent * 100.0, 3);

            if(percentage > 99)
            {
                percentage = 99;    //To keep system tray icon font size normal (only allow 2 digits)
            }
            bool isCharging = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
            String bitmapText = Math.Round(percentage, 0).ToString();
            Color color;
            if (isCharging)
            {
                color = Color.LightGreen;
            }
            else
            {
                color = Color.White;
            }





            using (Bitmap bitmap = new Bitmap(GetTextBitmap(bitmapText, new Font(font, fontSize), color)))
            {
                System.IntPtr intPtr = bitmap.GetHicon();

                ManagementScope scope = new ManagementScope("root\\WMI");
                ObjectQuery query = new ObjectQuery("SELECT * FROM BatteryStatus");

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        decimal fullCapacity = Convert.ToDecimal(obj["DischargeRate"]);
                        Debug.WriteLine(fullCapacity);
                    }
                }

                try
                {
                    using (Icon icon = Icon.FromHandle(intPtr))
                    {
                        PowerStatus pwr = SystemInformation.PowerStatus;
                        TimeSpan ts;
                        String batteryChargeInfo = "";
                        if(pwr.BatteryLifeRemaining < 0 && pwr.BatteryFullLifetime > 0)        //Is charging
                        {
                            ts = TimeSpan.FromSeconds(pwr.BatteryFullLifetime);
                            batteryChargeInfo = "\nTime To Charge: "+ts.Hours + "h " + ts.Minutes + "m " + ts.Seconds + "s";
                        }
                        else if(pwr.BatteryLifeRemaining > 0 && pwr.BatteryFullLifetime < 0)    //Is not charging
                        {
                            ts = TimeSpan.FromSeconds(pwr.BatteryLifeRemaining);
                            batteryChargeInfo = "\nTime Left: "+ts.Hours + "h " + ts.Minutes + "m " + ts.Seconds + "s";
                        }
                        notifyIcon.Icon = icon;
                        String toolTipText = percentage + "%" + (isCharging ? " Charging" : "")
                            + batteryChargeInfo;
                        notifyIcon.Text = toolTipText;
                    }
                }
                finally
                {
                    DestroyIcon(intPtr);
                }
            }
        }
    }
}
