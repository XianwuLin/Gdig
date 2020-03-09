﻿using DnsClient;
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

namespace Gdig_dev
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private LookupClient DnsClient;
        private String DnsServer;
        private Boolean IsWindowInitialized;


        ///// <summary>
        ///// 判断.Net Framework的Version是否符合需要
        ///// (.Net Framework 版本在2.0及以上)
        ///// </summary>
        ///// <param name="version">需要的版本 version = 4.5</param>
        ///// <returns></returns>
        //private static bool GetDotNetVersion(string version)
        //{
        //    string oldname = "0";
        //    using (RegistryKey ndpKey =
        //        RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").
        //        OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
        //    {
        //        foreach (string versionKeyName in ndpKey.GetSubKeyNames())
        //        {
        //            if (versionKeyName.StartsWith("v"))
        //            {
        //                RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
        //                string newname = (string)versionKey.GetValue("Version", "");
        //                if (string.Compare(newname, oldname) > 0)
        //                {
        //                    oldname = newname;
        //                }
        //                if (newname != "")
        //                {
        //                    continue;
        //                }
        //                foreach (string subKeyName in versionKey.GetSubKeyNames())
        //                {
        //                    RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
        //                    newname = (string)subKey.GetValue("Version", "");
        //                    if (string.Compare(newname, oldname) > 0)
        //                    {
        //                        oldname = newname;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return string.Compare(oldname, version) > 0 ? true : false;
        //}


        private void CreateDnsclient()
        {
            if (!IsWindowInitialized) {
                Trace.WriteLine("not finished.");
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
            Trace.WriteLine("DnsServer:" + DnsServer);
            Trace.WriteLine("recreated DnsServer.");
        }

        private void fill_data()
        {
            List<string> dnsTypes = new List<string> { "A", "AAAA", "CNAME", "MX", "NS", "PTR", "CERT", "SRV", "TXT", "SOA" };
            foreach(string dnsType in dnsTypes)
            {
                cb_type.Items.Add(dnsType);
            }
            cb_type.SelectedIndex = 0;
        }

        public MainWindow()
        {
            InitializeComponent();
            fill_data();
            IsWindowInitialized = true;
            CreateDnsclient();
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
                catch(Exception e2)
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

        
    }

}
