using DnsClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Gdig
{

    public class ElementData
    {
        protected object element;
        protected string configName;

        protected void UpdateAppConfig(string name, object value)
        {
            Properties.Settings.Default[name] = value;
        }

    }

    public class ComBoBoxData : ElementData
    {
        protected new ComboBox element;
        public ComBoBoxData(ComboBox obj, string configName)
        {
            this.element = obj;
            this.configName = configName;
        }

        public class CbbObject
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public override string ToString()
            {
                return Name;
            }
        }

        public void FillElement()
        {
            List<CbbObject> cbbList = new List<CbbObject>();
            Trace.WriteLine((string)Properties.Settings.Default[configName]);
            List<string> strs = new List<string>(((string)Properties.Settings.Default[configName]).Split(new char[] { ';' }));
            for (var i = 0; i < strs.Count; i++)
            {
                cbbList.Add(new CbbObject { ID = i, Name = strs[i] });
            }
            this.element.ItemsSource = cbbList;
            string index = configName + "Index";
            this.element.SelectedIndex = (int)Properties.Settings.Default[index];
        }

        public void UpdateConfig()
        {
            List<string> cbbList = new List<string>();
            foreach (var item in this.element.Items)
            {
                cbbList.Add(item.ToString()); ;
            }

            UpdateAppConfig(configName, string.Join(";", cbbList));

            string configNameIndex = configName + "Index";
            UpdateAppConfig(configNameIndex, this.element.SelectedIndex);
        }
    }


    public class CheckBoxData : ElementData
    {
        protected new readonly CheckBox element;
        public CheckBoxData(CheckBox obj, string configName)
        {
            this.element = obj;
            this.configName = configName;
        }

        public void FillElement()
        {
            bool value = (bool)Properties.Settings.Default[configName];
            this.element.IsChecked = value;
        }

        public void UpdateConfig()
        {
            UpdateAppConfig(configName, this.element.IsChecked);
        }
    }


    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private LookupClient DnsClient;
        private String DnsServer;
        private Boolean IsWindowInitialized;

        private void CreateDnsclient()
        {
            if (!IsWindowInitialized)
            {
                return;
            }
            string[] dns_server_conf = Regex.Split(cbb_nameservers.Text, "->|:", RegexOptions.IgnoreCase);
            string dns_server_ip = dns_server_conf[1].Trim();
            int dns_server_port = Int32.Parse(dns_server_conf[2].Trim());
            DnsServer = dns_server_ip;
            if (dns_server_port != 53)
            {
                DnsServer += ":" + dns_server_port;
            }
            DnsClient = new LookupClient(IPAddress.Parse(dns_server_ip), dns_server_port)
            {
                EnableAuditTrail = true
            };
            bool use_tcp = (Boolean)cb_tcp.IsChecked;
            bool no_cache = (Boolean)cb_nocache.IsChecked;
            if (use_tcp)
            {
                DnsClient.UseTcpOnly = true;
                DnsServer += "(TCP)";
            }
            if (no_cache)
            {
                DnsClient.UseCache = false;
            }
            saveConfigtoAppData();
        }

        private void fill_data()
        {
            new ComBoBoxData(cb_type, "dnsTypes").FillElement();
            new ComBoBoxData(cbb_nameservers, "nameServers").FillElement();
            new CheckBoxData(cb_tcp, "tcp").FillElement();
            new CheckBoxData(cb_nocache, "noCache").FillElement();
            new CheckBoxData(cb_autoclear, "autoClear").FillElement();
        }

        private void saveConfigtoAppData()
        {
            if (!IsWindowInitialized)
            {
                return;
            }
            new ComBoBoxData(cb_type, "dnsTypes").UpdateConfig();
            new ComBoBoxData(cbb_nameservers, "nameServers").UpdateConfig();
            new CheckBoxData(cb_tcp, "tcp").UpdateConfig();
            new CheckBoxData(cb_nocache, "noCache").UpdateConfig();
            new CheckBoxData(cb_autoclear, "autoClear").UpdateConfig();
        }

        public MainWindow()
        {
            InitializeComponent();
            fill_data();
            IsWindowInitialized = true;
            CreateDnsclient();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        private void Rtb_Result_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ep_options.IsExpanded = false;
        }


        private async void Bt_dig_Click(object sender, RoutedEventArgs e)
        {
            string dns_type = cb_type.Text;
            string domain = tb_domain.Text;
            string version = "1.0.0";

            bool autoclear = (Boolean)cb_autoclear.IsChecked;
            if (autoclear)
            {
                rtb_result.Document = new FlowDocument();
            }

            string result;
            if (domain == "")
            {
                result = "Query Domain should not empty.";
            }
            else
            {
                string dnsResult = "";
                try
                {
                    var dnsQuery = await DnsClient.QueryAsync(domain, (QueryType)Enum.Parse(typeof(QueryType), dns_type));
                    dnsResult = dnsQuery.AuditTrail;
                }
                catch (DnsResponseException e1)
                {
                    dnsResult = e1.AuditTrail.Split(new[] { '\r', '\n' }).FirstOrDefault();
                }
                catch (Exception e2)
                {
                    dnsResult = e2.ToString();
                }
                result = String.Format("; <<>> Gdig {0} <<>> @{1} {2}\n", version, DnsServer, domain);
                result += dnsResult;
                result += "\n;;" + string.Concat(Enumerable.Repeat("= ", 30));
            }

            rtb_result.Document.Blocks.Add(new Paragraph(new Run(result)));
            rtb_result.ScrollToEnd();
            tb_domain.SelectAll();
        }


        private void tb_domain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                bt_dig.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void Bt_clear_Click(object sender, RoutedEventArgs e)
        {
            rtb_result.Document = new FlowDocument();
        }

        private void Bt_copy_Click(object sender, RoutedEventArgs e)
        {
            rtb_result.SelectAll();
            rtb_result.Copy();
        }

        private void Bt_save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
            if (sfd.ShowDialog() == true)
            {
                TextRange doc = new TextRange(rtb_result.Document.ContentStart, rtb_result.Document.ContentEnd);
                using (FileStream fs = File.Create(sfd.FileName))
                {
                    doc.Save(fs, DataFormats.Text);
                }
            }
        }

        private void Reconfig(object sender, EventArgs e)
        {
            CreateDnsclient();
        }

        private void Reconfig(object sender, RoutedEventArgs e)
        {
            CreateDnsclient();
        }

        private void About_Click(object sender, RoutedEventArgs e)

        {
            System.Diagnostics.Process.Start("https://github.com/XianwuLin/Gdig");
        }

        private void cb_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            saveConfigtoAppData();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }

}
